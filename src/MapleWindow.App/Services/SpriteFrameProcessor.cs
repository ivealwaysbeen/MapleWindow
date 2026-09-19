using System.Drawing.Imaging;
using System.IO;

namespace MapleWindow.App.Services;

/// <summary>
/// MapleStory character renders sit on a mostly-transparent fixed-size canvas (measured: a 300x300 image
/// with only ~79px of actual character content, ~97px of transparent padding below the feet) — trimming
/// that padding is what makes the sprite fill its window and lets the feet land on the window's bottom
/// edge. The crop rectangle's left/top/right are the union of the real (no-margin) content bounds across
/// every known action frame (see Prime) — since that union already covers every pose the character can be
/// in, no extra side/top margin is needed to absorb per-pose variation. Bottom keeps a small margin since
/// it's the feet-to-floor reference and must stay tight.
/// </summary>
public sealed class SpriteFrameProcessor
{
    private const int AlphaThreshold = 10;
    private const int BottomMargin = 2; // kept tight: this edge is the feet-to-floor reference

    private Rectangle? _cropRect;

    /// <summary>Computes the crop rectangle once, up front, from every known action frame's raw pixel
    /// bounds, so the crop never has to grow (and visually shrink the on-screen character) mid-session as
    /// a not-yet-seen pose shows up. Safe to call once before animation starts; Process falls back to
    /// single-frame sizing if it's ever called without a prior Prime.</summary>
    public void Prime(IEnumerable<byte[]> pngFrames)
    {
        int minX = int.MaxValue, maxX = -1, minY = int.MaxValue, maxY = -1;
        int width = 0, height = 0;

        foreach (var pngBytes in pngFrames)
        {
            using var stream = new MemoryStream(pngBytes);
            using var bitmap = new Bitmap(stream);
            width = bitmap.Width;
            height = bitmap.Height;

            var bounds = ComputeContentBounds(bitmap);
            if (bounds.MaxX < 0) continue; // fully transparent frame — contributes nothing

            minX = Math.Min(minX, bounds.MinX);
            maxX = Math.Max(maxX, bounds.MaxX);
            minY = Math.Min(minY, bounds.MinY);
            maxY = Math.Max(maxY, bounds.MaxY);
        }

        if (maxX < 0) return; // no frame had any content — leave _cropRect unset, fall back per-frame

        var bottom = Math.Min(height, maxY + BottomMargin + 1);
        _cropRect = new Rectangle(minX, minY, maxX + 1 - minX, bottom - minY);
    }

    public byte[] Process(byte[] pngBytes)
    {
        using var stream = new MemoryStream(pngBytes);
        using var bitmap = new Bitmap(stream);

        _cropRect ??= ComputeCropRect(bitmap);

        using var cropped = bitmap.Clone(_cropRect.Value, bitmap.PixelFormat);
        using var outStream = new MemoryStream();
        cropped.Save(outStream, ImageFormat.Png);
        return outStream.ToArray();
    }

    private static Rectangle ComputeCropRect(Bitmap bitmap)
    {
        var bounds = ComputeContentBounds(bitmap);
        if (bounds.MaxX < 0) return new Rectangle(0, 0, bitmap.Width, bitmap.Height); // fully transparent frame — nothing to crop to

        var bottom = Math.Min(bitmap.Height, bounds.MaxY + BottomMargin + 1);
        return new Rectangle(bounds.MinX, bounds.MinY, bounds.MaxX + 1 - bounds.MinX, bottom - bounds.MinY);
    }

    private static (int MinX, int MaxX, int MinY, int MaxY) ComputeContentBounds(Bitmap bitmap)
    {
        int minX = bitmap.Width, maxX = -1, minY = bitmap.Height, maxY = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (bitmap.GetPixel(x, y).A <= AlphaThreshold) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        return (minX, maxX, minY, maxY);
    }
}
