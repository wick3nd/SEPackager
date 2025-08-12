using K4os.Compression.LZ4.Streams;
using SEPpackager.CRC;

namespace SEPpackager
{
    enum Mode
    {
        misc,
        tex,
        audio
    }

    internal static class Compression
    {
        public const byte versionMajor = 0x00;
        public const byte versionMinor = 0x05;

        public static uint mode;
        public static string? partName;
        public static string? dirsName;

        private static uint fileCount;
        private static uint totalOffset = 11;
        private static uint fileSize;       // In Bytes
        private static uint originalFileSize;       // Size before compression
        private static string? files;

        public static uint maxSizePerPart = 104857600;      // In Bytes

        private static readonly string inPath = Path.Combine(Directory.GetCurrentDirectory(), @"input\");
        private static readonly string outPath = Path.Combine(Directory.GetCurrentDirectory(), @"output\");

        public static void Compress()
        {
            if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);     // Creates a output directory if needed

            WriteArchive();
        }

        private static void WriteDirsHeader(BinaryWriter bw)
        {
            byte byte4 = (byte)fileCount;
            byte byte3 = (byte)(fileCount >> 8);
            byte byte2 = (byte)(fileCount >> 16);
            byte byte1 = (byte)(fileCount >> 24);

            //                    S     E     P     .     D     I     R     S
            byte[] dataBuffer = [ 0x53, 0x45, 0x50, 0x2E, 0x44, 0x49, 0x52, 0x53, versionMajor, versionMinor, (byte)mode, byte4, byte3, byte2, byte1 ];

            bw.Write(dataBuffer);
            bw.Write(CRC8.ComputeChecksum(dataBuffer));
        }

        private static void WriteArchiveData(BinaryWriter bw)
        {
            byte partPointer = 0x00;
            uint temp = 0;
            
            FileStream partFS = new($"{outPath}{partName}{partPointer:D3}.sep", FileMode.OpenOrCreate, FileAccess.Write);
            BinaryWriter partBW = new(partFS);
            {
                fileCount = (uint)Directory.GetFiles(inPath, "*", SearchOption.AllDirectories).ToArray().Length;

                string[]? fullFileName = Directory.GetFiles(inPath, "*", SearchOption.AllDirectories);
                
                WriteDirsHeader(bw);

                Console.Write($"  Creating part {partPointer:D3}\n");
                WritePartsHeader(partBW);

                for (int i = 0; i != fileCount; i++)
                {
                    files = Path.GetRelativePath(inPath, fullFileName[i]);

                    using FileStream input = new(fullFileName[i], FileMode.Open, FileAccess.Read);

                    switch (mode)
                    {
                        case 0:     // misc
                            uint lB = (uint)partFS.Position;
                            var setting = new LZ4EncoderSettings { ContentChecksum = true, CompressionLevel = K4os.Compression.LZ4.LZ4Level.L12_MAX };

                            using (var lz4 = LZ4Stream.Encode(partFS, leaveOpen: true, settings: setting)) input.CopyTo(lz4);

                            fileSize = (uint)(partFS.Length - lB);
                            originalFileSize = (uint)new FileInfo(fullFileName[i]).Length;

                            break;

                        case 1:     // tex
                            input.CopyTo(partFS);
                            fileSize = (uint)new FileInfo(fullFileName[i]).Length;

                            break;

                        case 2:     // sound
                            input.CopyTo(partFS);
                            fileSize = (uint)new FileInfo(fullFileName[i]).Length;

                            break;
                    }
                    
                    using MemoryStream ms = new();
                    using BinaryWriter MSbw = new(ms);
                    
                    // Writing to memory stream
                    MSbw.Write(files);              // Relative path
                    MSbw.Write(partPointer);        // Part pointer
                    MSbw.Write(totalOffset);        // File offset
                    MSbw.Write(fileSize);           // File length
                    if (mode == 0) MSbw.Write(originalFileSize);

                    byte[] dataBuffer = ms.ToArray();

                    // Writing the actual data
                    bw.Write(dataBuffer);
                    bw.Write(CRC16.ComputeChecksum(dataBuffer));

                    if (temp >= maxSizePerPart)
                    {
                        partPointer++;

                        totalOffset = 11;
                        fileSize = 0;
                        if (mode == 0) originalFileSize = 0;

                        partFS.Close();
                        partBW.Close();

                        partFS = new FileStream($"{outPath}{partName}{partPointer:D3}.sep", FileMode.Create, FileAccess.Write);
                        partBW = new BinaryWriter(partFS);

                        Console.Write($"\n  Creating part {partPointer:D3}\n");
                        WritePartsHeader(partBW);
                    }

                    // Increase the offset for the next file
                    totalOffset += fileSize;
                    temp = totalOffset;

                    Console.Write($"  ( {i+1}/{fileCount} )  {files}\n");
                }

                Console.Write($"\n  Dirs: {outPath}{dirsName}\n");
            }

            partFS.Close();
            partBW.Close();
        }

        private static void WritePartsHeader(BinaryWriter bw)
        {
            //                     S     E     P     .     D     A     T     A
            byte[] partsHeader = [ 0x53, 0x45, 0x50, 0x2E, 0x44, 0x41, 0x54, 0x41, versionMajor, versionMajor ];

            bw.Write(partsHeader);
            bw.Write(CRC8.ComputeChecksum(partsHeader));
        }

        private static void WriteArchive()
        {
            using (File.Create(Path.Combine(outPath, dirsName!))) { }       // Make the path for directory part
            using FileStream fs = new(outPath + dirsName, FileMode.Open, FileAccess.Write);
            using BinaryWriter bw = new(fs);
            {
                WriteArchiveData(bw);
            }
        }
    }
}
