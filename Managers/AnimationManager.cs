using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using Ameath.DesktopPet.Core;
using SkiaSharp;

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
        _paths[PetState.Idle] = _assetManager.GetAssetPaths("hu.webp", "nothing.webp", "cool.webp", "cute.webp");
        _paths[PetState.Wander] = _assetManager.GetAssetPaths("fly.webp");
        _paths[PetState.Interact] = _assetManager.GetAssetPaths("jump.webp", "jump2.webp");
        _paths[PetState.Drag] = _assetManager.GetAssetPaths("happy.webp", "happy2.webp");
        _paths[PetState.Sleep] = _assetManager.GetAssetPaths("cool.webp", "cute.webp");

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
        using var stream = File.OpenRead(path);
        using var codec = SKCodec.Create(stream);
        var scaleToReference = ShouldScaleToReference(path);
        if (codec == null)
        {
            return LoadSingleFrame(path, scaleToReference);
        }

        var info = codec.Info;
        var frameCount = codec.FrameCount;
        if (frameCount <= 1)
        {
            return LoadSingleFrame(path, scaleToReference);
        }

        var frameInfos = codec.FrameInfo;
        var frames = new List<Image>(frameCount);
        var durations = new List<int>(frameCount);

        for (var i = 0; i < frameCount; i++)
        {
            using var bitmap = new SKBitmap(info);
            var options = new SKCodecOptions(i);
            codec.GetPixels(info, bitmap.GetPixels(), options);
            frames.Add(ToImage(bitmap));

            var duration = frameInfos[i].Duration;
            durations.Add(duration > 0 ? duration : 100);
        }

        if (scaleToReference && _referenceSize.HasValue)
        {
            ResizeFrames(frames, _referenceSize.Value);
        }

        return new AnimatedImage(frames, durations);
    }

    private AnimatedImage LoadSingleFrame(string path, bool scaleToReference)
    {
        using var bitmap = SKBitmap.Decode(path);
        var frame = ToImage(bitmap);
        if (scaleToReference && _referenceSize.HasValue)
        {
            var resized = ResizeFrame(frame, _referenceSize.Value);
            frame.Dispose();
            frame = resized;
        }

        return new AnimatedImage(new List<Image> { frame }, new List<int> { 1000 });
    }

    private static Image ToImage(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        using var temp = Image.FromStream(stream);
        return new Bitmap(temp);
    }

    private void InitializeReferenceSize()
    {
        _referenceSize = null;
        var referenceAssets = _assetManager.GetAssetPaths("hu.webp", "nothing.webp");
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
        return fileName.Equals("cool.webp", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("cute.webp", StringComparison.OrdinalIgnoreCase);
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
}
