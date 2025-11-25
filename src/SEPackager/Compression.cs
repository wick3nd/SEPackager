using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Pipes;
using K4os.Compression.LZ4.Streams;
using SEPackager.CRC;
using System.IO;

namespace SEPackager
{
    internal class Compression
    {
        public static void Copy(string path, FileStream outStream)
        {
            using var inStream = new FileStream($"{path}", FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            inStream.CopyTo(outStream);
        }

       // Fix the compression
        public static void CompressLZMA(string path, FileStream outStream, out uint fileSize)  // Slower, good ratio
        {
            using var inStream = new FileStream($"{path}", FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;

            using var crcStream = new MemoryStream();
            var LZMAEncoder = new SevenZip.Compression.LZMA.Encoder();
            
            LZMAEncoder.WriteCoderProperties(crcStream);
            LZMAEncoder.Code(inStream, crcStream, inStream.Length, -1, null);

            uint CRC = CRC32.ComputeChecksum(crcStream.ToArray().AsSpan());

            crcStream.Position = 0;
            crcStream.CopyTo(outStream);

            outStream.Write(BitConverter.GetBytes(CRC));          

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void CompressZSTD(string path, FileStream outStream, out uint fileSize, int compresLevel = 5)  // faster, worse ratio
        {
            using var inStream = new FileStream($"{path}", FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;

            using var crcStream = new MemoryStream();
            using (var ZSTDEncoder = new ZstdSharp.CompressionStream(crcStream, compresLevel)) inStream.CopyTo(ZSTDEncoder);

            uint CRC = CRC32.ComputeChecksum(crcStream.ToArray().AsSpan());

            crcStream.Position = 0;
            crcStream.CopyTo(outStream);
            
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void CompressLZ4(string path, FileStream outStream, out uint fileSize, int compresLevel = 5)
        {
            using var inStream = new FileStream($"{path}", FileMode.Open, FileAccess.Read, FileShare.Read);
            inStream.Position = 0;

            long streamStart = outStream.Position;

            using var crcStream = new MemoryStream();
            var settings = new LZ4EncoderSettings() { CompressionLevel = (K4os.Compression.LZ4.LZ4Level)compresLevel };

            using var LZ4Encoder = LZ4Stream.Encode(crcStream, settings);
            inStream.CopyTo(LZ4Encoder);
            LZ4Encoder.Flush();

            uint CRC = CRC32.ComputeChecksum(crcStream.ToArray().AsSpan());

            crcStream.Position = 0;
            crcStream.CopyTo(outStream);

            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void SoundToOGG(string path, FileStream outStream, out uint fileSize)  // Add CRC for fucks sake
        {
           // Creates a temporary ogg buffer
           // string tempFilePath = $"{Program.soundBufferPath}";
           // using var tempStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
           // File.SetAttributes(tempFilePath, FileAttributes.Hidden);

            using var inStream = new FileStream($"{path}", FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);

            long streamStart = outStream.Position;

                FFMpegArguments.FromPipeInput(new StreamPipeSource(inStream))
                    .OutputToPipe(new StreamPipeSink(outStream), options => options
                        .WithAudioCodec("libopus")
                       // .WithAudioBitrate(AudioQuality.Good)

                        .ForceFormat("ogg"))
                    .ProcessSynchronously();

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void VideoToVG9(string path, FileStream outStream, out uint fileSize)
        {
            using var inStream = new FileStream($"{path}", FileMode.Open, FileAccess.Read, FileShare.Read);
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

            uint CRC = CRC32.ComputeChecksum(crcStream.ToArray().AsSpan());

            crcStream.Position = 0;
            crcStream.CopyTo(outStream);

            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void ImageToBC()
        {

        }

       // Add other compressions  - maybe
    }
}