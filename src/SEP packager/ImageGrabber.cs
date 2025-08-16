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
        MemoryStream ms = new();

        using var image = new MagickImage(inPath);
        var pixels = image.GetPixels().GetValues().AsSpan();

        // Get essential info
        var originalLength = (uint)pixels.Length;

        var channelCount = (byte)image.ChannelCount;
        var width = (ushort)image.Width;
        var height = (ushort)image.Height;

        var packedBits = (((((uint)(width & 0x3FFF) << 14) | (uint)(height & 0x3FFF)) << 3) | channelCount) << 1;

        using var zstd = new Compressor(level: 15);      // Compress the data

        zstd.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);        // Enable multithreading
        zstd.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);      // Enable CRC

        ms.Write(ImageHeader(packedBits, originalLength));
        ms.Write(zstd.Wrap(pixels));
        ms.Position = 0;

        return ms;
    }

    private static byte[] ImageHeader(
        uint packedBits,
        uint originalLength) =>
    [
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
