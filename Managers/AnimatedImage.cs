using System.Collections.Generic;
using System.Drawing;

namespace Ameath.DesktopPet.Managers;

public sealed class AnimatedImage
{
    public AnimatedImage(IReadOnlyList<Image> frames, IReadOnlyList<int> durations)
    {
        Frames = frames;
        Durations = durations;
    }

    public IReadOnlyList<Image> Frames { get; }

    public IReadOnlyList<int> Durations { get; }

    public int FrameCount => Frames.Count;
}
