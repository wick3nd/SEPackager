using ZstdSharp;
using SevenZip.Compression.LZMA;
using SEPackager.CRC;

namespace SEPackager
{
    internal class Compression
    {
        public static void Copy(FileStream inStream, FileStream outStream) => inStream.CopyTo(outStream);

       // Fix the compression
        public static void CompressLZMA(FileStream inStream, FileStream outStream, out uint fileSize)  // Slower, good ratio
        {
            using var tempStream = new MemoryStream();
            using var tempWriter = new BinaryWriter(tempStream);

            var LZMAEncoder = new Encoder();
            long streamStart = outStream.Position;  // File start

            LZMAEncoder.WriteCoderProperties(tempStream);
            LZMAEncoder.Code(inStream, tempStream, -1, -1, null);
            tempWriter.Write(CRC32.ComputeChecksum(tempStream.ToArray()));

            tempStream.CopyTo(outStream);

            long streamEnd = outStream.Position;  // File end
            fileSize = (uint)(streamEnd - streamStart);  // Compressed file size
        }

        public static void CompressZSTD(FileStream inStream, FileStream outStream, out uint fileSize, int compresLevel = 5)  // faster, worse ratio
        {
            using var tempStream = new MemoryStream();
            using var tempWriter = new BinaryWriter(tempStream);

            using var ZSTDEncoder = new CompressionStream(inStream, compresLevel);
            long streamStart = outStream.Position;  // File start

            ZSTDEncoder.CopyTo(tempStream);
            tempWriter.Write(CRC32.ComputeChecksum(tempStream.ToArray()));
            tempStream.CopyTo(outStream);

            long streamEnd = outStream.Position;  // File end
            fileSize = (uint)(streamEnd - streamStart);  // Compressed file size
        }

       // Add other compressions
    }
}