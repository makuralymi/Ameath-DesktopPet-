using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Ameath.DesktopPet.Core;

namespace Ameath.DesktopPet.Managers;

public sealed class AnimationManager
{
    private readonly AssetManager _assetManager;
    private readonly Dictionary<string, AnimatedImage> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _cacheLock = new();
    private readonly Random _random = new();
    private readonly Dictionary<PetState, IReadOnlyList<string>> _paths = new();
    private Size? _referenceSize;

    public AnimationManager(AssetManager assetManager)
    {
        _assetManager = assetManager;
    }

    public void LoadAssets()
    {
        _paths[PetState.Idle] = _assetManager.GetAssetPaths("hu.gif", "nothing.gif", "cool.gif", "cute.gif");
        _paths[PetState.Wander] = _assetManager.GetAssetPaths("fly.gif");
        _paths[PetState.Interact] = _assetManager.GetAssetPaths("jump.gif", "jump2.gif");
        _paths[PetState.Drag] = _assetManager.GetAssetPaths("happy.gif", "happy2.gif");
        _paths[PetState.Sleep] = _assetManager.GetAssetPaths("cool.gif", "cute.gif");

        InitializeReferenceSize();
    }

    public AnimatedImage? GetRandomAnimation(PetState state)
    {
        if (!_paths.TryGetValue(state, out var list) || list.Count == 0)
        {
            return null;
        }

        var selectedPath = list[_random.Next(list.Count)];
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(selectedPath, out var cached))
            {
                return cached;
            }

            var animation = LoadAnimation(selectedPath);
            _cache[selectedPath] = animation;
            return animation;
        }
    }

    public void PreloadAssets(params string[] fileNames)
    {
        var paths = _assetManager.GetAssetPaths(fileNames);
        foreach (var path in paths)
        {
            lock (_cacheLock)
            {
                if (_cache.ContainsKey(path))
                {
                    continue;
                }

                var animation = LoadAnimation(path);
                _cache[path] = animation;
            }
        }
    }

    private AnimatedImage LoadAnimation(string path)
    {
        using var image = Image.FromFile(path);
        var scaleToReference = ShouldScaleToReference(path);
        var dimension = new FrameDimension(image.FrameDimensionsList[0]);
        var frameCount = image.GetFrameCount(dimension);

        var frames = new List<Image>(frameCount);
        var durations = new List<int>(frameCount);
        var delays = GetFrameDelays(image, frameCount);

        for (var i = 0; i < frameCount; i++)
        {
            image.SelectActiveFrame(dimension, i);
            var frame = new Bitmap(image);
            frames.Add(frame);
            durations.Add(delays[i]);
        }

        if (scaleToReference && _referenceSize.HasValue)
        {
            ResizeFrames(frames, _referenceSize.Value);
        }

        return new AnimatedImage(frames, durations);
    }

    private void InitializeReferenceSize()
    {
        _referenceSize = null;
        var referenceAssets = _assetManager.GetAssetPaths("hu.gif", "nothing.gif");
        if (referenceAssets.Count == 0)
        {
            return;
        }

        var referencePath = referenceAssets[0];
        var referenceAnimation = LoadAnimation(referencePath);
        _referenceSize = referenceAnimation.Frames[0].Size;
        _cache[referencePath] = referenceAnimation;
    }

    private static bool ShouldScaleToReference(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Equals("cool.gif", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("cute.gif", StringComparison.OrdinalIgnoreCase);
    }

    private static void ResizeFrames(List<Image> frames, Size targetSize)
    {
        for (var i = 0; i < frames.Count; i++)
        {
            var original = frames[i];
            var resized = ResizeFrame(original, targetSize);
            original.Dispose();
            frames[i] = resized;
        }
    }

    private static Image ResizeFrame(Image source, Size targetSize)
    {
        var bitmap = new Bitmap(targetSize.Width, targetSize.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.DrawImage(source, new Rectangle(Point.Empty, targetSize));
        return bitmap;
    }

    private static List<int> GetFrameDelays(Image image, int frameCount)
    {
        const int defaultDelay = 100;
        var delays = new List<int>(frameCount);
        var item = image.PropertyItems.Length == 0
            ? null
            : Array.Find(image.PropertyItems, property => property.Id == 0x5100);

        if (item == null || item.Value.Length < 4)
        {
            for (var i = 0; i < frameCount; i++)
            {
                delays.Add(defaultDelay);
            }

            return delays;
        }

        for (var i = 0; i < frameCount; i++)
        {
            var delay = BitConverter.ToInt32(item.Value, i * 4) * 10;
            delays.Add(delay > 0 ? delay : defaultDelay);
        }

        return delays;
    }
}
