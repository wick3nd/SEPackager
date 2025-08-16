using SEPpackager.CRC;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace System.Runtime.Intrinsics;

enum Mode
{
    misc,
    tex,
    audio
}

internal static class Compression
{
    public const byte versionMajor = 0x00;
    public const byte versionMinor = 0x06;

    public static uint mode;
    public static string? partName;
    public static string? dirsName;

    private static uint _fileCount;
    private static uint _totalOffset = 11;
    private static uint _fileSize;       // In Bytes
    private static uint _originalFileSize;       // Size before compression
    private static string? _files;

    public static uint maxSizePerPart = 104857600;      // In Bytes

    private static readonly string _inPath = Path.Combine(Directory.GetCurrentDirectory(), @"input\");
    private static readonly string _outPath = Path.Combine(Directory.GetCurrentDirectory(), @"output\");

    public static void Compress()
        {
            if (!Directory.Exists(_outPath)) Directory.CreateDirectory(_outPath);     // Creates a output directory if needed

            WriteArchive();
        }

    private static void WriteDirsHeader(BinaryWriter bw)
        {
            byte byte4 = (byte)_fileCount;
            byte byte3 = (byte)(_fileCount >> 8);
            byte byte2 = (byte)(_fileCount >> 16);
            byte byte1 = (byte)(_fileCount >> 24);

            //                    S     E     P     .     D     I     R     S
            byte[] dataBuffer = [ 0x53, 0x45, 0x50, 0x2E, 0x44, 0x49, 0x52, 0x53, versionMajor, versionMinor, (byte)mode, byte4, byte3, byte2, byte1 ];

            bw.Write(dataBuffer);
            bw.Write(CRC8.ComputeChecksum(dataBuffer));
        }

    private static void WriteArchiveData(BinaryWriter bw)
    { 
        byte partPointer = 0x00;
        uint temp = 0;
            
        FileStream partFS = new($"{_outPath}{partName}{partPointer:D3}.sep", FileMode.OpenOrCreate, FileAccess.Write);
        BinaryWriter partBW = new(partFS);
        {
            string[]? fullFileName = Directory.GetFiles(_inPath, "*", SearchOption.AllDirectories);
            _fileCount = (uint)fullFileName.AsSpan().Length;
            
            WriteDirsHeader(bw);

            Console.Write($"  Creating part {partPointer:D3}\n");
            WritePartsHeader(partBW);

            for (int i = 0; i != _fileCount; i++)
            {
                _files = Path.GetRelativePath(_inPath, fullFileName[i]);

                using FileStream input = new(fullFileName[i], FileMode.Open, FileAccess.Read);

                switch (mode)
                {
                    case 0:     // misc
                        uint lB = (uint)partFS.Position;
                        using (var zstd = new CompressionStream(partFS, level: 15, leaveOpen: true))
                        {
                            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
                            zstd.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);

                            input.CopyTo(zstd);
                        }

                        _fileSize = (uint)(partFS.Length - lB);
                        _originalFileSize = (uint)new FileInfo(fullFileName[i]).Length;

                        break;

                    case 1:     // tex
                        bool success;
                        MemoryStream? imgData;

                        _ = new ImageGrabber(fullFileName[i], out success, out imgData);

                        if (!success)
                        {
                            input.CopyTo(partFS);
                            _fileSize = (uint)new FileInfo(fullFileName[i]).Length;
                            input.Close();
                        }
                        else
                        {
                            _fileSize = (uint)imgData!.Length;
                            imgData.CopyTo(partFS);
                            imgData.Close();
                        }

                        break;

                    case 2:     // sound
                        input.CopyTo(partFS);
                        _fileSize = (uint)new FileInfo(fullFileName[i]).Length;

                        break;
                }

                input.Close();

                using MemoryStream ms = new();
                using BinaryWriter MSbw = new(ms);

                // Writing to memory stream
                MSbw.Write(_files);              // Relative path
                MSbw.Write(partPointer);        // Part pointer
                MSbw.Write(_totalOffset);        // File offset
                MSbw.Write(_fileSize);           // File length
                if (mode == 0) MSbw.Write(_originalFileSize);
                
                byte[] dataBuffer = ms.ToArray();
                
                // Writing the actual data
                bw.Write(dataBuffer);
                bw.Write(CRC16.ComputeChecksum(dataBuffer));

                if (temp >= maxSizePerPart)
                {
                    partPointer++;

                    _totalOffset = 11;
                    _fileSize = 0;
                    if (mode == 0) _originalFileSize = 0;

                    partFS.Close();
                    partBW.Close();

                    partFS = new FileStream($"{_outPath}{partName}{partPointer:D3}.sep", FileMode.Create, FileAccess.Write);
                    partBW = new BinaryWriter(partFS);

                    Console.Write($"\n  Creating part {partPointer:D3}\n");
                    WritePartsHeader(partBW);
                }
                
                // Increase the offset for the next file
                _totalOffset += _fileSize;
                temp = _totalOffset;
                
                Console.Write($"  ( {i + 1}/{_fileCount} )  {_files}\n");
            }

            Console.Write($"\n  Dirs: {_outPath}{dirsName}\n");
        }
    
        partFS.Close();
        partBW.Close();
    }

    private static void WritePartsHeader(BinaryWriter bw)
    {
        //                     S     E     P     .     D     A     T     A
        byte[] partsHeader = [ 0x53, 0x45, 0x50, 0x2E, 0x44, 0x41, 0x54, 0x41, versionMajor, versionMinor ];
        
        bw.Write(partsHeader);
        bw.Write(CRC8.ComputeChecksum(partsHeader));
    }

    private static void WriteArchive()
    {
        using (File.Create(Path.Combine(_outPath, dirsName!))) { }       // Make the path for directory part
        using FileStream fs = new(_outPath + dirsName, FileMode.Open, FileAccess.Write);
        using BinaryWriter bw = new(fs);
        {
            WriteArchiveData(bw);
        }
    }
}