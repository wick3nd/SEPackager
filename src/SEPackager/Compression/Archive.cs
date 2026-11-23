using SEPackager.Utils;
using w3.CRC;
using System.Text;

namespace SEPackager.Compression
{
    enum Mode  // 3 bit number - [0, 4]
    {
        none,
        miscLZMA,
        miscLZ4,
        miscZSTD,
        image,
        sound,
        video
    }

    internal class Archive
    {
       // Archive creation
        public static string? archName;
        public static uint bytesPerArchive;
        private static readonly int _fileCount = FileCheck.GetFileCount();  // UInt24 not 32 - less files to store but that shouldn't be a big problem

       // Entries
        private static byte _archPart;
        private static int _offset = 8;
        public static int entryOffset = 14;
        private static int _originalFileLen = 0;

       // Streams
        private static FileStream? _dataStream;
        private static BinaryWriter? _dataWriter;

        public static void Write()
        {
           // Create the output stream
            using var stream = new FileStream($"{Program.outPath}{archName}_dirs.sep", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            using var binWriter = new BinaryWriter(stream);

           // Begin writing the entries first
            stream.Seek(14, SeekOrigin.Begin);
            UpdateDataStream();

           // Write both archive simultaneously
            for (int i = 0; i < _fileCount; i++)
            {
                Mode compresMode = (Mode)FileCheck.files![i];
                string path = FileCheck.filePaths[i];

                WriteDatArch(path, compresMode);
                WriteEntry(binWriter, compresMode, path);
                ManifestWriter.Write(_offset, Path.GetRelativePath(Program.inPath, path));
                _originalFileLen = 0;
                
               // Create new data archive when its too big
                if (_offset >= bytesPerArchive)
                {
                    _offset = 8;
                    _archPart++;

                    UpdateDataStream();
                }

                Console.WriteLine($"  ({i + 1}/{_fileCount}) P:{_archPart:X3} | {path}");
            }
           // Write the hash table
            Console.Write("  Done.\n  Writing hash... ");
            Hash.WriteHash(binWriter);

            Console.WriteLine("Done.");

           // Write the header last
            Console.Write("  Finishing writing the archive... ");

            stream.Seek(0, SeekOrigin.Begin);
            WriteDirHeader(binWriter);
            Dispose();

            Console.WriteLine("Done.");
        }

        private static void WriteDatArch(string path, Mode compression)
        {
            switch (compression)
            {
                case Mode.none:     Compressions.Copy(path, _dataStream!);
                    break;

                case Mode.miscLZMA: Compressions.CompressLZMA(path, _dataStream!, out _originalFileLen);
                    break;

                case Mode.miscZSTD: Compressions.CompressZSTD(path, _dataStream!, out _originalFileLen);
                    break;

                case Mode.miscLZ4:  Compressions.CompressLZ4(path, _dataStream!, out _originalFileLen);
                    break;

                case Mode.image:    Compressions.ImageToBC(path, _dataStream!, out _originalFileLen, BCnEncoder.Shared.CompressionFormat.Bc7);
                    break;

                case Mode.sound:    Compressions.SoundToOGG(path, _dataStream!, out _originalFileLen);  // use something else than originalfilelen to prevent the creation of bigger entries
                    break;

                case Mode.video:    Compressions.VideoToVG9(path, _dataStream!, out _originalFileLen);
                    break;
            }
        }

        private static void UpdateDataStream()
        {
           // Dispose the streams if created
            _dataStream?.Dispose();
            _dataWriter?.Dispose();

           // Create new streams
            _dataStream = new($"{Program.outPath}{archName}_{_archPart:D3}.sep", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            _dataWriter = new(_dataStream);

            WriteDatHeader();
        }

        private static void WriteEntry(BinaryWriter writer, Mode compression, string path)
        {
           // Streams for CRC calculation and writing to stream
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);
            
            string relativePath = Path.GetRelativePath(Program.inPath, path);
            byte[] relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
            int fileLength = (int)new FileInfo(path).Length;
            byte[] packedfileLen = [(byte)fileLength, (byte)(fileLength >> 8), (byte)(fileLength >> 16), (byte)(fileLength >> 24 | _originalFileLen << 4), (byte)(_originalFileLen >> 4), (byte)(_originalFileLen >> 12), (byte)(_originalFileLen >> 20)];

            bool isCompressed = (compression == Mode.miscLZMA) || (compression == Mode.miscZSTD) || (compression == Mode.miscLZ4);

           // Entry structure
            bw.Write(_archPart);
            bw.Write(_offset);
            bw.Write((byte)compression);
            if (!isCompressed) bw.Write(fileLength);
            else bw.Write(packedfileLen);

            bw.Write(CRC8.ComputeChecksum(ms.ToArray().AsSpan()));
            writer.Write(ms.ToArray().AsSpan());
            Hash.PrepareHash(path);

            _offset += _originalFileLen == 0 ? fileLength : _originalFileLen;
            entryOffset += isCompressed ? 14: 11;
        }

        //  \/  Archive Header Section  \/
        private static void WriteDirHeader(BinaryWriter writer)
        {
            //                First 2 bits     | Next 5 bits           | Last bits
            int packedData = Program.VerMajor | Program.VerMinor << 3 | _fileCount << 9;

            byte[] metadata = [
                0x53,  // S
                0x45,  // E
                0x50,  // P
                0x44,  // D
                0x49,  // I
                0x52,  // R
                (byte)packedData,
                (byte)(packedData >> 8),
                (byte)(packedData >> 16),
                (byte)entryOffset,
                (byte)(entryOffset >> 8),
                (byte)(entryOffset >> 16),
                (byte)(entryOffset >> 24),
            ];
            byte CRC = CRC8.ComputeChecksum(metadata);
            
            writer.Write(metadata);
            writer.Write(CRC);
        }

        private static void WriteDatHeader()
        {
            //               First 2 bits            | Last 5 bits           
            byte packedVer = Program.VerMajor << 6 | Program.VerMinor;
            byte[] metadata = [
                0x53,  // S
                0x45,  // E
                0x50,  // P
                0x44,  // D
                0x41,  // A
                0x54,  // T
                packedVer
            ];
            byte CRC = CRC8.ComputeChecksum(metadata);

            _dataWriter?.Write(metadata);
            _dataWriter?.Write(CRC);
        }

       // Annihilate the data
        private static void Dispose()
        {
            Hash.Dispose();
            ManifestWriter.Dispose();
            FileCheck.Dispose();

            _dataStream?.Dispose();
            _dataWriter?.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}