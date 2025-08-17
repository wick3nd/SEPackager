using System.Diagnostics.CodeAnalysis;
using ImageMagick;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace System.Runtime.Intrinsics;

public static class ImageGrabber
{
    public static bool TryGrabImage(string path, [MaybeNullWhen(false)] out MemoryStream image)
    {
        if (!IsImage(path)) {
            image = null;
            return false;
        }

        image = ImageDataStream(path);
        return true;
    }

    private static MemoryStream ImageDataStream(string inPath)
    {
        using var image = new MagickImage(inPath);

        var pixels = image.GetPixels().GetValues();
        var ms = new MemoryStream();

        using (var zstd = new CompressionStream(ms, level: 15))
        {
            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);

            // Write header
            var packedBits = (((((image.Width & 0x3FFF) << 14) | (image.Height & 0x3FFF)) << 3) | (byte)image.ChannelCount) << 1;
            ms.Write(ImageHeader(packedBits, (uint)pixels!.Length));

            // Compress pixels directly
            zstd.Write(pixels, 0, pixels.Length);
        }

        ms.Position = 0;
        return ms;
    }


    private static byte[] ImageHeader(uint packedBits, uint originalLength) => [
        0x53,
        0x45,
        0x52,
        0x49,
        (byte)(packedBits >> 24),
        (byte)(packedBits >> 16),
        (byte)(packedBits >> 8),
        (byte)packedBits,
        (byte)originalLength,
        (byte)(originalLength >> 8),
        (byte)(originalLength >> 16),
        (byte)(originalLength >> 24)
    ];

    private static bool IsImage(string inPath)
    {
        try { return new MagickImageInfo(inPath!) != null; }
        catch { return false; }
    }
}
