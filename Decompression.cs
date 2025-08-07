using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using SEPpackager;
using SEPpackager.CRC;
using System.Diagnostics;
using System.Text;

internal static class Decompression
{
    static int mode;
    static ushort fileCount;
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

    static byte partNumber;
    //static string[]? validParts;

    public static void Decompress(string inputPath, string outputPath)
    {
        Stopwatch watch = new();
        watch.Start();

        inPath = inputPath;
        outPath = outputPath;

        SearchHeaders();

        watch.Stop();
        Console.Write($"\n\n  Operation done in {watch.ElapsedMilliseconds}ms.\n\n");
        
        Input();

        Console.Write("\n  Done");
        Thread.Sleep(2500);
        Program.TUI();
    }

    private static void SearchHeaders()
    {
        using FileStream fs = new(inPath!, FileMode.Open, FileAccess.Read);
        using BinaryReader br = new(fs, Encoding.UTF8, false);

        fs.Seek(10, SeekOrigin.Begin);      // Skip the idendtifier and version

        mode = br.ReadByte();
        fileCount = br.ReadUInt16();

        fs.Seek(14, SeekOrigin.Begin);      // Skip the CRC8 chunk

        path = new string[fileCount];
        partPointer = new string[fileCount];
        offset = new uint[fileCount];
        fileSize = new uint[fileCount];
        if (mode == 0) originalFileSize = new uint[fileCount];
        CRC = new ushort[fileCount];

        string fileName = Path.GetFileName(inPath)![..^8];        // Get the filename and crop the last 8 chararcters

        if (fileCount > 0)
        {
            for (int i = 0; i != fileCount; i++)
            {
                using MemoryStream ms = new();
                using BinaryWriter bw = new(ms);

                path[i] = br.ReadString();          // Get the path of the file inside the archive

                partNumber = br.ReadByte();
                partPointer[i] = $"{fileName}{partNumber:D3}.sep";      // Pointer to the part of the archive

                offset[i] = br.ReadUInt32();        // Offset from the beginning
                fileSize[i] = br.ReadUInt32();      // Length of the data
                if (mode == 0) originalFileSize![i] = br.ReadUInt32();
                CRC[i] = br.ReadUInt16();           // CRC16 of the entry

                // Writing to memory stream for CRC check
                bw.Write(path[i]);
                bw.Write(partNumber);
                bw.Write(offset[i]);
                bw.Write(fileSize[i]);
                if (mode == 0) bw.Write(originalFileSize![i]);
                bw.Write(CRC[i]);

                if (!CRC16.CheckChecksum(ms.ToArray())) throw new FileLoadException($"Corrupted entry {i} detected, please recompress or redownload the _dirs file.");
            }

            for (int i = 0; i != fileCount; i++)
            {
                if (i == 0 || partPointer[i - 1] != partPointer[i]) Console.Write($"\n  {partPointer[i]}\n");

                bool charSelector = (i == fileCount - 1) || partPointer[i] != partPointer[i + 1];       // Checks if the file is the last one in the part
                Console.Write(charSelector ? $"  └{path[i]}\n" : $"  ├{path[i]}\n");
            }
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

        while (true)        // Waits for input
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

        for (int i = 0; i != filesToDecode; i++)        // Loops and saves all selected files
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
        Console.SetCursorPosition(0, Console.CursorTop - 1);        // Get back to the previous line
        Console.Write(new string('\r', Console.WindowWidth));       // Clear the line
        Console.SetCursorPosition(0, Console.CursorTop - 1);        // i dont know why its needed here but because of it, it works fine
    }

    private static void GetBytesFromPart(string input)
    {
        if (!path!.Contains(input)) throw new FileNotFoundException($"No file with the name \"{input}\" exists.");      // Checks if the path array doesnt contain the input path 
        pathIndex = Array.IndexOf(path!, input);

        if (mode == 0) byteArray = new byte[originalFileSize![pathIndex]];
        else byteArray = new byte[fileSize![pathIndex]];

        string sepPath = Path.Combine(Path.GetDirectoryName(inPath)!, partPointer![pathIndex]);

        using FileStream fs = new(sepPath, FileMode.Open, FileAccess.Read);     // Get the bytes from the part file
        using BinaryReader br = new(fs);
        {
            fs.Seek(offset![pathIndex], SeekOrigin.Begin);

            if (mode == 0)
            {
                using MemoryStream bufferMS = new(br.ReadBytes((int)fileSize![pathIndex]));
                using var lz4 = LZ4Stream.Decode(bufferMS, leaveOpen: true);
                using MemoryStream ms = new();
                lz4.CopyTo(ms);

                byteArray = ms.ToArray();
            }
            else byteArray = br.ReadBytes((int)fileSize![pathIndex]);
        }
    }

    private static void WriteFile()
    {
        string fullOutPath;

        if (outPath == "") fullOutPath = Path.Combine(@"output\", path![pathIndex]);
        else fullOutPath = Path.Combine(outPath!, path![pathIndex]);

        Directory.CreateDirectory(Path.GetDirectoryName(fullOutPath)!);     // Make the path at the selected location

        using FileStream fs = new(fullOutPath, FileMode.Create);       // Create the file at the path
        using BinaryWriter bw = new(fs);
        bw.Write(byteArray!);
    }
}