using System.Drawing.Imaging;
using System.IO;

namespace MapleWindow.App.Services;

/// <summary>
/// MapleStory character renders sit on a mostly-transparent fixed-size canvas (measured: a 300x300 image
/// with only ~79px of actual character content, ~97px of transparent padding below the feet) — trimming
/// that padding is what makes the sprite fill its window and lets the feet land on the window's bottom
/// edge. The crop rectangle is measured once, from the first frame seen, and reused for every later frame
/// so that different poses (whose exact content bounds differ slightly) don't jitter against inconsistent
/// per-frame crops — margins absorb that variation instead.
/// </summary>
public sealed class SpriteFrameProcessor
{
    private const int AlphaThreshold = 10;
    private const int SideMargin = 20;
    private const int TopMargin = 15;
    private const int BottomMargin = 2; // kept tight: this edge is the feet-to-floor reference

    private Rectangle? _cropRect;

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

        if (maxX < 0) return new Rectangle(0, 0, bitmap.Width, bitmap.Height); // fully transparent frame — nothing to crop to

        var left = Math.Max(0, minX - SideMargin);
        var top = Math.Max(0, minY - TopMargin);
        var right = Math.Min(bitmap.Width, maxX + SideMargin + 1);
        var bottom = Math.Min(bitmap.Height, maxY + BottomMargin + 1);
        return new Rectangle(left, top, right - left, bottom - top);
    }
}
