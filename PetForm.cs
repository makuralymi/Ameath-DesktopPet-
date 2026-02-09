using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;
using Ameath.DesktopPet.Controllers;
using Ameath.DesktopPet.Core;
using Ameath.DesktopPet.Managers;

namespace Ameath.DesktopPet;

public sealed class PetForm : Form
{
    private readonly PictureBox _pictureBox;
    private readonly PetStateMachine _stateMachine;
    private readonly BehaviorController _behaviorController;
    private readonly MovementController _movementController;
    private readonly AnimationManager _animationManager;
    private readonly System.Windows.Forms.Timer _animationTimer;
    private bool _dragging;
    private Point _dragOffset;
    private bool _mouseThrough;
    private bool _pinned;
    private ToolStripMenuItem? _mouseThroughItem;
    private ToolStripMenuItem? _pinItem;
    private ToolStripMenuItem? _resizeItem;
    private NotifyIcon? _notifyIcon;
    private AnimatedImage? _currentAnimation;
    private int _currentFrameIndex;
    private bool _faceLeft;
    private readonly Dictionary<Image, Image> _flippedCache = new();
    private bool _resizeMode;
    private float _scale = 1f;

    public PetForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        StartPosition = FormStartPosition.Manual;

        _pictureBox = new PictureBox
        {
            BackColor = Color.Transparent,
            SizeMode = PictureBoxSizeMode.AutoSize
        };

        Controls.Add(_pictureBox);

        var assetRoot = Path.Combine(AppContext.BaseDirectory, "gif");
        if (!Directory.Exists(assetRoot))
        {
            var fallbackRoot = Path.Combine(Directory.GetCurrentDirectory(), "gif");
            if (Directory.Exists(fallbackRoot))
            {
                assetRoot = fallbackRoot;
            }
            else
            {
                assetRoot = FindGifRootFromParent(AppContext.BaseDirectory) ?? assetRoot;
            }
        }
        var assetManager = new AssetManager(assetRoot);
        _animationManager = new AnimationManager(assetManager);
        _animationManager.LoadAssets();
        _ = Task.Run(() => _animationManager.PreloadAssets("cool.webp", "cute.webp"));

        _animationTimer = new System.Windows.Forms.Timer();
        _animationTimer.Tick += (_, _) => AdvanceFrame();

        _movementController = new MovementController(this);
        _movementController.FacingChanged += OnFacingChanged;

        _stateMachine = new PetStateMachine();
        _stateMachine.StateEntered += OnStateEntered;
        _stateMachine.StateExited += OnStateExited;

        _behaviorController = new BehaviorController(_stateMachine);

        _stateMachine.Initialize(PetState.Idle);
        _behaviorController.Start();

        _pictureBox.MouseDown += HandleMouseDown;
        _pictureBox.MouseMove += HandleMouseMove;
        _pictureBox.MouseUp += HandleMouseUp;
        _pictureBox.MouseClick += HandleMouseClick;
        _pictureBox.MouseWheel += HandleMouseWheel;
        MouseWheel += HandleMouseWheel;

        ContextMenuStrip = BuildContextMenu();
        InitializeTrayIcon();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (_notifyIcon != null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }

        base.OnFormClosed(e);
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            if (_mouseThrough)
            {
                cp.ExStyle |= 0x20;
            }

            return cp;
        }
    }

    private void OnStateEntered(PetState state)
    {
        var image = _animationManager.GetRandomAnimation(state);
        if (image != null)
        {
            StartAnimation(image);
        }

        if (state == PetState.Wander)
        {
            _movementController.StartWander();
        }
        else
        {
            _movementController.Stop();
        }
    }

    private void OnStateExited(PetState state)
    {
        if (state == PetState.Wander)
        {
            _movementController.Stop();
        }
    }

    private void HandleMouseClick(object? sender, MouseEventArgs e)
    {
        if (_pinned)
        {
            return;
        }

        if (e.Button == MouseButtons.Left)
        {
            _behaviorController.TriggerInteract();
        }
    }

    private void HandleMouseDown(object? sender, MouseEventArgs e)
    {
        if (_pinned)
        {
            return;
        }

        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _dragging = true;
        _dragOffset = new Point(e.X, e.Y);
        _behaviorController.BeginDrag();
    }

    private void HandleMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var screenPos = PointToScreen(e.Location);
        Location = new Point(screenPos.X - _dragOffset.X, screenPos.Y - _dragOffset.Y);
    }

    private void HandleMouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = false;
            _behaviorController.EndDrag();
        }
    }

    private void StartAnimation(AnimatedImage animation)
    {
        _currentAnimation = animation;
        _currentFrameIndex = 0;
        var frame = ResolveFrameImage(animation.Frames[0]);
        SetFrame(frame);

        if (animation.FrameCount > 1)
        {
            _animationTimer.Interval = animation.Durations[0];
            _animationTimer.Start();
        }
        else
        {
            _animationTimer.Stop();
        }
    }

    private void AdvanceFrame()
    {
        if (_currentAnimation == null || _currentAnimation.FrameCount <= 1)
        {
            _animationTimer.Stop();
            return;
        }

        _currentFrameIndex = (_currentFrameIndex + 1) % _currentAnimation.FrameCount;
        SetFrame(ResolveFrameImage(_currentAnimation.Frames[_currentFrameIndex]));
        _animationTimer.Interval = _currentAnimation.Durations[_currentFrameIndex];
    }

    private void OnFacingChanged(bool faceLeft)
    {
        _faceLeft = faceLeft;
        if (_currentAnimation == null)
        {
            return;
        }

        var frame = ResolveFrameImage(_currentAnimation.Frames[_currentFrameIndex]);
        SetFrame(frame);
    }

    private void SetFrame(Image frame)
    {
        _pictureBox.Image = frame;
        ApplyScale(frame.Size);
    }

    private void ApplyScale(Size baseSize)
    {
        var width = Math.Max(1, (int)Math.Round(baseSize.Width * _scale));
        var height = Math.Max(1, (int)Math.Round(baseSize.Height * _scale));
        _pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
        _pictureBox.Size = new Size(width, height);
        Size = _pictureBox.Size;
    }

    private Image ResolveFrameImage(Image source)
    {
        if (!_faceLeft)
        {
            return source;
        }

        if (_flippedCache.TryGetValue(source, out var cached))
        {
            return cached;
        }

        var flipped = new Bitmap(source);
        flipped.RotateFlip(RotateFlipType.RotateNoneFlipX);
        _flippedCache[source] = flipped;
        return flipped;
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        _mouseThroughItem = new ToolStripMenuItem("鼠标穿透: 关");
        _mouseThroughItem.Click += (_, _) => ToggleMouseThrough();
        menu.Items.Add(_mouseThroughItem);

        _pinItem = new ToolStripMenuItem("固定位置: 关");
        _pinItem.Click += (_, _) => TogglePinned();
        menu.Items.Add(_pinItem);

        _resizeItem = new ToolStripMenuItem("调整大小: 关");
        _resizeItem.Click += (_, _) => ToggleResizeMode();
        menu.Items.Add(_resizeItem);

        var exitItem = new ToolStripMenuItem("退出");
        exitItem.Click += (_, _) => Close();
        menu.Items.Add(exitItem);
        return menu;
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Desktop Pet",
            Visible = true,
            ContextMenuStrip = ContextMenuStrip
        };

        _notifyIcon.MouseUp += (_, e) =>
        {
            if (e.Button == MouseButtons.Right)
            {
                ContextMenuStrip?.Show(Cursor.Position);
            }
        };
    }

    private void ToggleMouseThrough()
    {
        _mouseThrough = !_mouseThrough;
        _mouseThroughItem!.Text = _mouseThrough ? "鼠标穿透: 开" : "鼠标穿透: 关";
        RecreateHandle();
    }

    private void TogglePinned()
    {
        _pinned = !_pinned;
        _pinItem!.Text = _pinned ? "固定位置: 开" : "固定位置: 关";

        if (_pinned)
        {
            _behaviorController.Stop();
            _movementController.Stop();
            _stateMachine.ChangeState(PetState.Sleep);
        }
        else
        {
            _stateMachine.ChangeState(PetState.Idle);
            _behaviorController.Start();
        }
    }

    private void ToggleResizeMode()
    {
        _resizeMode = !_resizeMode;
        _resizeItem!.Text = _resizeMode ? "调整大小: 开" : "调整大小: 关";
    }

    private void HandleMouseWheel(object? sender, MouseEventArgs e)
    {
        if (!_resizeMode)
        {
            return;
        }

        var delta = e.Delta > 0 ? 0.1f : -0.1f;
        _scale = Math.Clamp(_scale + delta, 0.5f, 3f);
        if (_pictureBox.Image != null)
        {
            ApplyScale(_pictureBox.Image.Size);
        }
    }

    private static string? FindGifRootFromParent(string startPath)
    {
        var current = new DirectoryInfo(startPath);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, "gif");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        return null;
    }
}
