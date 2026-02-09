using System;
using System.Drawing;
using System.Windows.Forms;

namespace Ameath.DesktopPet.Controllers;

public sealed class MovementController
{
    private readonly Form _form;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Random _random = new();
    private Point _targetLocation;
    private bool _moving;
    private bool _facingLeft;

    public event Action<bool>? FacingChanged;

    public MovementController(Form form)
    {
        _form = form;
        _timer = new System.Windows.Forms.Timer { Interval = 16 };
        _timer.Tick += (_, _) => Step();
    }

    public void StartWander()
    {
        _moving = true;
        _targetLocation = GetRandomTarget();
        _timer.Start();
    }

    public void Stop()
    {
        _moving = false;
        _timer.Stop();
    }

    private void Step()
    {
        if (!_moving)
        {
            return;
        }

        var current = _form.Location;
        var dx = _targetLocation.X - current.X;
        var dy = _targetLocation.Y - current.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        if (distance < 2)
        {
            _targetLocation = GetRandomTarget();
            return;
        }

        var newFacingLeft = dx < 0;
        if (newFacingLeft != _facingLeft)
        {
            _facingLeft = newFacingLeft;
            FacingChanged?.Invoke(_facingLeft);
        }

        var step = 2.5;
        var nextX = current.X + (int)(dx / distance * step);
        var nextY = current.Y + (int)(dy / distance * step);
        _form.Location = new Point(nextX, nextY);
    }

    private Point GetRandomTarget()
    {
        var screen = Screen.FromControl(_form).WorkingArea;
        var maxX = Math.Max(screen.Left, screen.Right - _form.Width);
        var maxY = Math.Max(screen.Top, screen.Bottom - _form.Height);
        var x = _random.Next(screen.Left, maxX + 1);
        var y = _random.Next(screen.Top, maxY + 1);
        return new Point(x, y);
    }
}
