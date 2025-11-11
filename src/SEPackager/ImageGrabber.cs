using System.Diagnostics.CodeAnalysis;
using MimeDetective;
using ZstdSharp;
using ZstdSharp.Unsafe;
using StbImageSharp;

namespace SEPpackager;

public class ImageGrabber
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
        StbImage.stbi_set_flip_vertically_on_load(1);
        var image = ImageResult.FromStream(File.OpenRead(inPath));

        var pixels = image.Data.AsSpan();
        var ms = new MemoryStream();

        using (var zstd = new CompressionStream(ms, level: 15))
        {
            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);

            var packedBits = (uint)(((image.Width & 0x3FFF) << 14 | image.Height & 0x3FFF) << 3 | (byte)image.SourceComp) << 1;
            ms.Write(ImageHeader(packedBits, (uint)ms!.Length));

            zstd.Write(pixels);
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
        var inspector = FileSorter.FileInspector().Inspect(inPath).ByMimeType().FirstOrDefault()?.MimeType.Split("/");

        if (inspector![0] == "IMAGE") return true;

        return false;
    }
}
