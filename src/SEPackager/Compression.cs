using SEPackager.CRC;
using K4os.Compression.LZ4.Streams;

namespace SEPackager
{
    internal class Compression
    {
        public static void Copy(FileStream inStream, FileStream outStream) => inStream.CopyTo(outStream);

       // Fix the compression
        public static void CompressLZMA(FileStream inStream, FileStream outStream, out uint fileSize)  // Slower, good ratio
        {
            long streamStart = outStream.Position;

            using var tempStream = new MemoryStream();
            var LZMAEncoder = new SevenZip.Compression.LZMA.Encoder();
            
            LZMAEncoder.WriteCoderProperties(tempStream);
            LZMAEncoder.Code(inStream, tempStream, inStream.Length, -1, null);

            uint CRC = CRC32.ComputeChecksum(tempStream.ToArray().AsSpan());

            tempStream.Position = 0;
            tempStream.CopyTo(outStream);

            outStream.Write(BitConverter.GetBytes(CRC));          

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void CompressZSTD(FileStream inStream, FileStream outStream, out uint fileSize, int compresLevel = 5)  // faster, worse ratio
        {
            long streamStart = outStream.Position;

            using var tempStream = new MemoryStream();
            using (var ZSTDEncoder = new ZstdSharp.CompressionStream(tempStream, compresLevel)) inStream.CopyTo(ZSTDEncoder);

            uint CRC = CRC32.ComputeChecksum(tempStream.ToArray().AsSpan());

            tempStream.Position = 0;
            tempStream.CopyTo(outStream);
            
            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

        public static void CompressLZ4(FileStream inStream, FileStream outStream, out uint fileSize, int compresLevel = 5)
        {
            long streamStart = outStream.Position;

            using var tempStream = new MemoryStream();
            var settings = new LZ4EncoderSettings() { CompressionLevel = (K4os.Compression.LZ4.LZ4Level)compresLevel };

            using var LZ4Encoder = LZ4Stream.Encode(tempStream, settings);
            inStream.CopyTo(LZ4Encoder);
            LZ4Encoder.Flush();

            uint CRC = CRC32.ComputeChecksum(tempStream.ToArray().AsSpan());

            tempStream.Position = 0;
            tempStream.CopyTo(outStream);

            outStream.Write(BitConverter.GetBytes(CRC));

            fileSize = (uint)(outStream.Position - streamStart);
        }

       // Add other compressions
    }
}