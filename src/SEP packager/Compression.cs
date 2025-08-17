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

    public static Mode mode;
    public static string? partName;
    public static string? dirsName;

    private static uint _fileCount;
    private static uint _totalOffset = 11;
    private static uint _fileSize;       // In Bytes
    private static uint _originalFileSize;       // Size before compression
    private static string? _files;

    public static uint maxSizePerPart = 104857600;

    private static readonly string _inPath = Path.Combine(Directory.GetCurrentDirectory(), @"input\");
    private static readonly string _outPath = Path.Combine(Directory.GetCurrentDirectory(), @"output\");

    public static void Compress()
    {
            if (!Directory.Exists(_outPath)) Directory.CreateDirectory(_outPath);     // Creates a output directory if needed

            WriteArchive();
    }

    private static void WriteDirsHeader(BinaryWriter bw)
    {
        ReadOnlySpan<byte> dataBuffer = [
            0x53,       // S
            0x45,       // E
            0x50,       // P
            0x2E,       // .
            0x44,       // D
            0x49,       // I
            0x52,       // R
            0x53,       // S
            versionMajor,
            versionMinor,
            (byte)mode,
            (byte)_fileCount,
            (byte)(_fileCount >> 8),
            (byte)(_fileCount >> 16),
            (byte)(_fileCount >> 24)
        ];

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
            string[] fullFileName = Directory.GetFiles(_inPath, "*", SearchOption.AllDirectories);
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
                    case Mode.misc:
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

                    case Mode.tex:
                    {
                        if (ImageGrabber.TryGrabImage(fullFileName[i], out var imgData))
                        {
                            using (imgData)
                            {
                                _fileSize = (uint)imgData.Length;
                                imgData.CopyTo(partFS);
                            }
                        }
                        else
                        {
                            input.CopyTo(partFS);
                            _fileSize = (uint)new FileInfo(fullFileName[i]).Length;
                        }

                        break;
                    }

                    case Mode.audio:
                        input.CopyTo(partFS);
                        _fileSize = (uint)new FileInfo(fullFileName[i]).Length;

                        break;
                }

                input.Close();

                using MemoryStream ms = new();
                using BinaryWriter MSbw = new(ms);

                // Writing to memory stream
                MSbw.Write(_files);              // Relative path
                MSbw.Write(partPointer);         // Part pointer
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
        byte[] partsHeader = [
            0x53,       // S
            0x45,       // E
            0x50,       // P
            0x2E,       // .
            0x44,       // D
            0x41,       // A
            0x54,       // T
            0x41,       // A
            versionMajor,
            versionMinor
        ];
        
        bw.Write(partsHeader);
        bw.Write(CRC8.ComputeChecksum(partsHeader));
    }

    private static void WriteArchive()
    {
        // Make the path for directory part
        using FileStream fs = new(Path.Combine(_outPath, dirsName!), FileMode.Create, FileAccess.Write);
        using BinaryWriter bw = new(fs);

        WriteArchiveData(bw);
    }
}