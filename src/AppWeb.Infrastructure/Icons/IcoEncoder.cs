using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace AppWeb.Infrastructure.Icons;

internal static class IcoEncoder
{
    private static readonly int[] Sizes = [16, 24, 32, 48, 64, 128, 256];

    public static byte[] Encode(Image source)
    {
        var images = new List<(int Size, byte[] Data)>(Sizes.Length);

        foreach (var size in Sizes)
        {
            using var bitmap = RenderSquare(source, size);
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            images.Add((size, stream.ToArray()));
        }

        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)images.Count);

        var imageOffset = 6 + (16 * images.Count);
        foreach (var (size, data) in images)
        {
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)(size >= 256 ? 0 : size));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)data.Length);
            writer.Write((uint)imageOffset);

            imageOffset += data.Length;
        }

        foreach (var (_, data) in images)
        {
            writer.Write(data);
        }

        writer.Flush();
        return output.ToArray();
    }

    private static Bitmap RenderSquare(Image source, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.Clear(Color.Transparent);
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var scale = Math.Max((float)size / source.Width, (float)size / source.Height);
        var width = source.Width * scale;
        var height = source.Height * scale;
        var x = (size - width) / 2f;
        var y = (size - height) / 2f;

        graphics.DrawImage(source, x, y, width, height);
        return bitmap;
    }
}
