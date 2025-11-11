using SEPpackager.CRC;
using System.Diagnostics;
using System.Text;
using ZstdSharp;

namespace SEPpackager;
internal static class Decompression
{
    static byte[]? byteBuffer;
    static readonly byte[] identifier = new byte[16];
    static readonly byte[] correctIdentifier = [ 0x53, 0x45, 0x50, 0x2E, 0x44, 0x49, 0x52, 0x53];       // "SEP.DIRS"
    
    static byte partNumber;
    
    static int mode;
    static uint fileCount;
    static string[]? path;
    static string[]? partPointer;
    static uint[]? offset;
    static uint[]? fileSize;
    static uint[]? originalFileSize;
    static ushort[]? CRC;

    static string? inPath;
    static string? outPath;

    static int pathIndex;
    static byte[]? byteArray;

    static int filesToDecode;
    static readonly string[] filesToDecodeArray = new string[ushort.MaxValue];

    public static void Decompress(string inputPath, string outputPath)
    {
        inPath = inputPath;
        outPath = outputPath;

        ValidateFile();

        Stopwatch watch = new();
        watch.Start();

        SearchHeaders();

        watch.Stop();
        Console.Write($"\n  Operation done in {watch.ElapsedMilliseconds}ms.\n\n");
        
        Input();

        Console.Write("\n  Done");
        Thread.Sleep(2500);
        Program.TUI();
    }

    private static void SearchHeaders()
    {
        using FileStream fs = new(inPath!, FileMode.Open, FileAccess.Read);
        using BinaryReader br = new(fs, Encoding.UTF8, false);

        fs.Seek(10, SeekOrigin.Begin);    // Skip the idendtifier and version

        mode = br.ReadByte();
        fileCount = br.ReadUInt32();

        fs.Seek(16, SeekOrigin.Begin);    // Skip the CRC8 chunk

        path = new string[fileCount];
        partPointer = new string[fileCount];
        offset = new uint[fileCount];
        fileSize = new uint[fileCount];
        if (mode == 0) originalFileSize = new uint[fileCount];
        CRC = new ushort[fileCount];

        string fileName = Path.GetFileName(inPath)![..^8];    // Get the filename and crop the last 8 chararcters

        if (fileCount > 0)
        {
            for (int i = 0; i != fileCount; i++)
            {
                using MemoryStream ms = new();
                using BinaryWriter bw = new(ms);

                path[i] = br.ReadString();    // Get the path of the file inside the archive

                partNumber = br.ReadByte();
                partPointer[i] = $"{fileName}{partNumber:D3}.sep";    // Pointer to the part of the archive

                offset[i] = br.ReadUInt32();    // Offset from start of the file
                fileSize[i] = br.ReadUInt32();    // Length of the data
                if (mode == 0) originalFileSize![i] = br.ReadUInt32();
                CRC[i] = br.ReadUInt16();    // CRC16 of the entry

                // Writing to memory stream for CRC check
                bw.Write(path[i]);
                bw.Write(partNumber);
                bw.Write(offset[i]);
                bw.Write(fileSize[i]);
                if (mode == 0) bw.Write(originalFileSize![i]);
                bw.Write(CRC[i]);

                if (!CRC16.CheckChecksum(ms.ToArray())) throw new FileLoadException($"Corrupted entry {i} detected, please recompress or redownload the _dirs file.");
            }

            // It needs to have a separate loop or it will break - to fix?
            for (int i = 0; i != fileCount; i++) PrintContents(i); 
        }
    }

    private static void PrintContents(int i)
    {
        if (i == 0 || partPointer![i - 1] != partPointer[i]) Console.Write($"\n  {partPointer![i]}\n");

        bool charSelector = i == fileCount - 1 || partPointer[i] != partPointer[i + 1];    // Checks if the file is the last one in the part
        Console.Write(charSelector ? $"  └{path![i]}\n" : $"  ├{path![i]}\n");
    }

    private static void ValidateFile()
    {
        Console.Clear();
        byteBuffer = File.ReadAllBytes(inPath!);
        Buffer.BlockCopy(byteBuffer!, 0, identifier, 0, 16);

        if (!CRC8.CheckChecksum(identifier)) throw new FileLoadException("Corrupt header detected");
        if (!identifier[..8].SequenceEqual(correctIdentifier[..8])) throw new FileLoadException("Wrong format given");    // Checks the first 8 bytes if it has a correct header; sidenote -  dont delete any slicing or it will fuck itself up

        Console.Write("File loaded\n---------------------------------\n");

        CheckVersion();
    }

    private static void CheckVersion()
    {
        Console.Write($"SteelEngine Package version {identifier[8]}.{identifier[9]:D3}\n");

        if (Compression.versionMajor != identifier[8] || Compression.versionMinor != identifier[9])
        {
            SEDebug.Log(SEDebugState.Warning, "This version of the packager may not work on this file.\n");
        }
    }

    private static void Input()
    {
        if (fileCount == 0)
        {
            Console.Write("""
                  No files avaible for decompression. Exit? [any]
                
                  > 
                """);

            _ = Console.ReadLine()!;

            Program.TUI();
        }

        while (true)
        {
            Console.Write("  > [PATH|MULTI|ALL] ");
            string input = Console.ReadLine()!.ToString().Trim();
            Console.Title = "SEP Packager";

            switch (input.ToLower())
            {
                case "all": GetAllFiles();
                    break;

                case "multi": GetMultipleFiles();
                    break;

                case "exit": Program.TUI();
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        GetBytesFromPart(input);
                        WriteFile();

                        break;
                    }
                    ClearCurrentLine();
                    
                    continue;
            }
            break;
        }
    }

    private static void GetAllFiles()
    {
        for (int i = 0; i != fileCount; i++)
        {
            GetBytesFromPart(path![i]);
            WriteFile();
        }
    }

    private static void GetMultipleFiles()
    {
        ClearCurrentLine();

        Console.Write("\n  Enter the paths of the files to decode.\n");

        while (true)    // Waits for input
        {
            Console.Write("  > [MULTI] ");
            string multiInput = Console.ReadLine()!.ToString();
            Console.Title = "SEP Packager";

            if (multiInput.Equals("done", StringComparison.CurrentCultureIgnoreCase)) break;        // Exits the loop if keyword "done" is entered

            if (!string.IsNullOrWhiteSpace(multiInput))
            {
                filesToDecodeArray![filesToDecode] = multiInput;
                filesToDecode++;
            }
        }

        for (int i = 0; i != filesToDecode; i++)    // Loops and saves all selected files
        {
            string currentPath = filesToDecodeArray![i];

            if (path!.Contains(filesToDecodeArray[i]))
            {
                GetBytesFromPart(currentPath);
                WriteFile();
            }
            else
            {
                SEDebug.Log(SEDebugState.Error, $"No file with the name \"{filesToDecodeArray[i]}\" exists.");
            }
        }
    }

    private static void ClearCurrentLine()
    {
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        Console.Write(new string('\r', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop - 1);
    }

    private static void GetBytesFromPart(string input)
    {
        if (!path!.Contains(input)) throw new FileNotFoundException($"No file with the name \"{input}\" exists.");    // Checks if the path array doesnt contain the input path 
        pathIndex = Array.IndexOf(path!, input);

        // byteArray initialization dependent on the mode
        if (mode == 0) byteArray = new byte[originalFileSize![pathIndex]];
        else byteArray = new byte[fileSize![pathIndex]];

        string sepPath = Path.Combine(Path.GetDirectoryName(inPath)!, partPointer![pathIndex]);

        // Get the bytes from the part file
        using FileStream fs = new(sepPath, FileMode.Open, FileAccess.Read);
        using BinaryReader br = new(fs);
        {
            fs.Seek(offset![pathIndex], SeekOrigin.Begin);

            if (mode == 0)
            {
                using MemoryStream bufferMS = new(br.ReadBytes((int)fileSize![pathIndex]));
                using var zstd = new DecompressionStream(bufferMS, leaveOpen: true);
                using MemoryStream ms = new();
                zstd.CopyTo(ms);

                byteArray = ms.ToArray();

                return;
            }
            if (mode == 1)
            {
                
            }

            byteArray = br.ReadBytes((int)fileSize![pathIndex]);
        }
    }

    private static void WriteFile()
    {
        string fullOutPath;

        if (outPath == "") fullOutPath = Path.Combine(@"output\", path![pathIndex]);
        else fullOutPath = Path.Combine(outPath!, path![pathIndex]);

        Directory.CreateDirectory(Path.GetDirectoryName(fullOutPath)!);    // Make the path at the selected location

        // Create the file at the path
        using FileStream fs = new(fullOutPath, FileMode.Create);
        using BinaryWriter bw = new(fs);
        bw.Write(byteArray!);
    }
}