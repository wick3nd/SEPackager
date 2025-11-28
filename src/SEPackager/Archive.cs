using SEPackager.CRC;
using System.Text;

namespace SEPackager
{
    enum Mode  // 3 bit number - [0, 4]
    {
        none,
        misc,
        image,
        sound,
        video
    }

    internal class Archive
    {
       // Archive creation
        public static string? archName;
        public static uint bytesPerArchive;
        private static readonly uint _fileCount = FileCheck.GetFileCount();  // UInt24 not 32 - less files to store but that shouldn't be a big problem

       // Entries
        private static byte _archPart;
        private static uint _offset = 8;
        private static uint _entryOffset = 14;
        private static uint _originalFileLen = 0;

       // Streams
        private static FileStream? _dataStream;
        private static BinaryWriter? _dataWriter;

       // Hash
        private static readonly Hash _hash = new((uint)(_fileCount * 1.8f));
        private static ushort[] _collisionDetection1 = new ushort[_hash.bucketCount];
        private static ushort[] _collisionDetection2 = new ushort[_hash.bucketCount];
        private static uint[] _hashEntryOffset = new uint[_hash.bucketCount];

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
            WriteHash(binWriter);

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
                case Mode.none:  Compression.Copy(path, _dataStream!);
                    break;

                case Mode.misc:  Compression.CompressZSTD(path, _dataStream!, out _originalFileLen, 15);
                    break;

                case Mode.image: Compression.ImageToBC(path, _dataStream!, out _originalFileLen, BCnEncoder.Shared.CompressionFormat.Bc7);
                    break;

                case Mode.sound: Compression.SoundToOGG(path, _dataStream!, out _originalFileLen);  // use something else than originalfilelen to prevent the creation of bigger entries
                    break;

                case Mode.video: Compression.VideoToVG9(path, _dataStream!, out _originalFileLen);
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
            byte stringLen = (byte)relativePathBytes.Length;
            uint fileLength = (uint)new FileInfo(path).Length;
            byte[] packedfileLen = [(byte)fileLength, (byte)(fileLength >> 8), (byte)(fileLength >> 16), (byte)((fileLength >> 24) | (_originalFileLen << 4)), (byte)(_originalFileLen >> 4), (byte)(_originalFileLen >> 12), (byte)(_originalFileLen >> 20)];

            bool isCompressed = compression == Mode.misc;

           // Entry structure
            bw.Write(stringLen);
            bw.Write(relativePathBytes);
            bw.Write(_archPart);
            bw.Write(_offset);
            bw.Write((byte)compression);
            if (!isCompressed) bw.Write(fileLength);
            else bw.Write(packedfileLen);

            bw.Write(CRC8.ComputeChecksum(ms.ToArray()));

            writer.Write(ms.ToArray());

            PrepareHash(relativePath);

            _offset += _originalFileLen == 0 ? fileLength : _originalFileLen;
            _entryOffset += isCompressed ? (uint)(14 + stringLen) : (uint)(12 + stringLen);
        }
        
        //  \/  Hash Table  \/
        private static void PrepareHash(string path)
        {
            uint hashIndex = _hash.HashFunction( CRC32.ComputeChecksum( Encoding.UTF8.GetBytes(path) ) );

            while (_collisionDetection1[hashIndex] != 0) hashIndex = (hashIndex + 1) % (uint)_collisionDetection1.Length;  // Checks if the space is occupied
            _collisionDetection1[hashIndex] = CRC16.ComputeChecksum( Encoding.UTF8.GetBytes( path ) );  // First CRC check for full file name
            _collisionDetection2[hashIndex] = CRC16.ComputeChecksum( Encoding.UTF8.GetBytes( Path.GetFileNameWithoutExtension(path) ) );  // Second CRC check for file name without extension
            _hashEntryOffset[hashIndex] = _entryOffset;
        }

        private static void WriteHash(BinaryWriter writer)
        {
            for (int i = 0; i < _hash.bucketCount; i++)
            {
                writer.Write(_collisionDetection1[i]);
                writer.Write(_collisionDetection2[i]);
                writer.Write(_hashEntryOffset[i]);
            }
        }

        //  \/  Archive Header Section  \/
        private static void WriteDirHeader(BinaryWriter writer)
        {
            //                First 2 bits     | Next 5 bits           | Last bits
            uint packedData = Program.VerMajor | Program.VerMinor << 3 | (_fileCount << 9);

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
                (byte)_entryOffset,
                (byte)(_entryOffset >> 8),
                (byte)(_entryOffset >> 16),
                (byte)(_entryOffset >> 24),
            ];
            byte CRC = CRC8.ComputeChecksum(metadata);
            
            writer.Write(metadata);
            writer.Write(CRC);
        }

        private static void WriteDatHeader()
        {
            //               First 2 bits            | Last 5 bits           
            byte packedVer = (Program.VerMajor << 6) | Program.VerMinor;
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
            _collisionDetection1 = [];
            _collisionDetection2 = [];
            _hashEntryOffset = [];

            FileCheck.Dispose();
            _hash.Dispose();
            _dataStream?.Dispose();
            _dataWriter?.Dispose();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}