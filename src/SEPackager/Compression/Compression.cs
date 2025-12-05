using BCnEncoder.Encoder;
using BCnEncoder.ImageSharp;
using BCnEncoder.Shared;

using FFMpegCore;
using FFMpegCore.Pipes;

using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;

using SevenZip.Compression.LZMA;
using ZstdSharp;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using w3.CRC;

namespace SEPackager.Compression
{
    internal class Compression
    {
        public static void Copy(string path, FileStream outStream)  // Add CRC
        {
            using var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            var crc = new CRC32Stream(inStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)inStream.Length);
            
            inStream.CopyTo(outStream);
            outStream.Write(BitConverter.GetBytes(CRC));
        }

        public static void CompressLZMA(string path, FileStream outStream, out uint fileSize)
        {
            using var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;

           // using var crcStream = new MemoryStream();
            var LZMAEncoder = new Encoder();
            
            LZMAEncoder.WriteCoderProperties(outStream);
            LZMAEncoder.Code(inStream, outStream, inStream.Length, -1, null);

            var crc = new CRC32Stream(inStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)inStream.Length);
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void CompressZSTD(string path, FileStream outStream, out uint fileSize, int compresLevel = 5)
        {
            using var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;

            using (var ZSTDEncoder = new CompressionStream(outStream, compresLevel)) inStream.CopyTo(ZSTDEncoder);

            var crc = new CRC32Stream(inStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)inStream.Length);
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void CompressLZ4(string path, FileStream outStream, out uint fileSize, int compresLevel = 5)
        {
            using var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;

            var settings = new LZ4EncoderSettings() { CompressionLevel = (LZ4Level)compresLevel };

            using var LZ4Encoder = LZ4Stream.Encode(outStream, settings);
            inStream.CopyTo(LZ4Encoder);
            LZ4Encoder.Flush();

            var crc = new CRC32Stream(inStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)inStream.Length);
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void SoundToOGG(string path, FileStream outStream, out uint fileSize)
        {
            string tempPath = $"{Program.outPath}tempSoundBuffer_{Guid.NewGuid()}.tmp";

            using var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var tempStream = new FileStream(tempPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            File.SetAttributes(tempPath, FileAttributes.Hidden);

            long streamStart = outStream.Position;

            FFMpegArguments.FromPipeInput(new StreamPipeSource(inStream))
                .OutputToPipe(new StreamPipeSink(tempStream), options => options
                    .WithAudioCodec("libopus")
                   // .WithAudioBitrate(AudioQuality.Good)

                    .ForceFormat("ogg"))
                .ProcessSynchronously();

            tempStream.Position = 0;
            byte[] buffer = new byte[tempStream.Length];
            tempStream.Read(buffer, 0, buffer.Length);

            var crc = new CRC32Stream(tempStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)tempStream.Length);
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);

            tempStream.Flush();
            tempStream.Dispose();

            File.Delete(tempPath);
        }

        public static void VideoToVG9(string path, FileStream outStream, out uint fileSize)
        {
            using var inStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;
            using var crcStream = new MemoryStream();

            var videoInfo = FFProbe.Analyse(path);
            var videoBitrate = videoInfo.PrimaryVideoStream!.BitRate;
            var videoFremarate = videoInfo.PrimaryVideoStream!.FrameRate;

            FFMpegArguments.FromPipeInput(new StreamPipeSource(inStream))
                .OutputToPipe(new StreamPipeSink(crcStream), options => options
                    .WithAudioCodec("libopus")

                    .WithVideoCodec("libvpx-vp9")
                    .WithFramerate(videoFremarate)
                    .WithVideoBitrate((int)videoBitrate)

                    .ForceFormat("webm"))
                .ProcessSynchronously();

            var crc = new CRC32Stream(inStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)inStream.Length);
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void ImageToBC(string path, FileStream outStream, out uint fileSize, CompressionFormat format)
        {
            using var tempStream = new MemoryStream();
            using Image<Rgba32> image = Image.Load<Rgba32>(path);

            long streamStart = outStream.Position;

            var BCEncoder = new BcEncoder();
            var outOptions = BCEncoder.OutputOptions;

            outOptions.GenerateMipMaps = false;
            outOptions.Quality = CompressionQuality.Balanced;
            outOptions.FileFormat = OutputFileFormat.Dds;
            outOptions.Format = format;

            BCEncoder.EncodeToStream(image, tempStream);

            var crc = new CRC32Stream(tempStream);
            uint CRC = crc.ComputeChecksum(8192, 0, (int)tempStream.Length);
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }
    }
}