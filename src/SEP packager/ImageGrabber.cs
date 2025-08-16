using ImageMagick;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace System.Runtime.Intrinsics
{
    public class ImageGrabber
    {
        private static string? _inPath;
        private static uint _originalLength;
        private static byte _channelCount;
        private static ushort _width;
        private static ushort _height;
        private static uint _packedBits;

        public ImageGrabber(string path, out bool success, out MemoryStream? image)
        {   
            _inPath = path;

            bool isImage = IsImage();
            success = isImage;

            image = null;
            if (!isImage) return;     // Go back if there are no supported image formats

            image = ImageDataStream();
        }

        private static MemoryStream ImageDataStream()
        {
            MemoryStream ms = new();

            using var image = new MagickImage(_inPath!);
            var pixels = image.GetPixels().GetValues().AsSpan();

            // Get essential info
            _originalLength = (uint)pixels.Length;
            _channelCount = (byte)image.ChannelCount;
            _width = (ushort)image.Width;
            _height = (ushort)image.Height;

            _packedBits = (((((uint)(_width & 0x3FFF) << 14) | (uint)(_height & 0x3FFF)) << 3) | _channelCount) << 1;

            using var zstd = new Compressor(level: 15);      // Compress the data

            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);        // Enable multithreading
            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);      // Enable CRC

            ms.Write(ImageHeader());
            ms.Write(zstd.Wrap(pixels));
            ms.Position = 0;

            return ms;
        }

        private static byte[] ImageHeader() => [ 0x53, 0x45, 0x52, 0x49, (byte)(_packedBits >> 24), (byte)(_packedBits >> 16), (byte)(_packedBits >> 8), (byte)_packedBits, (byte)_originalLength, (byte)(_originalLength >> 8), (byte)(_originalLength >> 16), (byte)(_originalLength >> 24) ];

        private static bool IsImage()
        {
            try { return new MagickImageInfo(_inPath!) != null; }
            catch { return false; }
        }
    }
}