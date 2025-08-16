using System.Runtime.Intrinsics;

namespace SEPpackager
{
    internal class Program
    {
        static void Main()
        {
            Console.Title = "SEP Packager";
            Console.ForegroundColor = ConsoleColor.White;

            TUI();
        }

        private static void AppendLogo()
        {
            Console.Clear();
            Console.Write("""
                
                  ███████╗███████╗██████╗
                  ██╔════╝██╔════╝██╔══██╗
                  ███████╗█████╗  ██████╔╝
                  ╚════██║██╔══╝  ██╔═══╝
                  ███████║███████╗██║
                  ╚══════╝╚══════╝╚═╝  Packager
                
                  ------------------------------------------


                """);
        }

        public static void TUI()
        {
            AppendLogo();

            Console.Write("""
                  What would you like to do today?
                
                  1. Compress
                  2. Decompress
                  3. Add to exisitng SEP                    (Unavaible, will crash)
                  4. Delete from existing SEP               (Unavaible, will crash)

                  0. Exit


                """);

            string? input = ReadInput();
            switch (input)
            {
                case "1": CompressionTUI();
                    break;

                case "2": DecompressionTUI();
                    break;

                case "3": Environment.Exit(0);
                    break;

                case "4": Environment.Exit(0);
                    break;

                case "0": Environment.Exit(0);
                    break;

                default: TUI();
                    break;
            }
        }

        private static void CompressionTUI()
        {
            AppendLogo();

            Console.Write("""
                  Compression

                  Select the type of the files the archive should store.

                  1. Misc
                  2. Texture
                  3. Sound

                
                """);

            string mode = ReadInput();
            switch (mode)
            {
                case "1": Compression.mode = 0;
                    break;

                case "2": Compression.mode = 1;
                    break;

                case "3": Compression.mode = 2;
                    break;

                default: CompressionTUI();       // Clear the console and call the TUI again
                    break;
            }

            AppendLogo();
            Console.Write($"""
                  Compression

                  Choose the max size of the parts in MegaBytes. (100MB default)

                
                """);

            string input = ReadInput("[INT] ");
            if (!string.IsNullOrEmpty(input)) Compression.maxSizePerPart = Convert.ToUInt32(input) * 1048576;

            AppendLogo();
            Console.Write($"""
                  Compression

                  Enter the name of the archive.

                
                """);

            while (true)
            {
                string name = ReadInput();
                if (string.IsNullOrWhiteSpace(name))
                {
                    int currentLine = Console.CursorTop;

                    Console.Write(new string('\r', Console.WindowWidth));
                    Console.SetCursorPosition(0, Console.CursorTop - 1);

                    Console.Write("  Name cannot be empty.");

                    Thread.Sleep(1000);

                    Console.SetCursorPosition(0, currentLine - 1);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, currentLine - 1);

                    continue;
                }

                Compression.dirsName = name + $"_{Enum.GetName(typeof(Mode), Compression.mode)}_dirs.sep";
                Compression.partName = name + $"_{Enum.GetName(typeof(Mode), Compression.mode)}_";

                break;
            }

            AppendLogo();
            Console.Write($"""
                  Compression

                  Put all files to be packaged in a input folder, then press "Enter" when ready.
                  {Directory.GetCurrentDirectory()}\input

                  [ Ready? ]
                """);

            string inputDirPath = $@"{Directory.GetCurrentDirectory()}\input";
            if (!Directory.Exists(inputDirPath)) Directory.CreateDirectory(inputDirPath);

            _ = Console.ReadLine();

            Console.SetCursorPosition(0, Console.CursorTop - 1);
            Console.Write(new string('\r', Console.WindowWidth));

            Compression.Compress();
            Console.Write("\n  Press [ANY] to exit.");
            _ = Console.ReadLine();
            TUI();
        }

        private static void DecompressionTUI()
        {
            AppendLogo();

            Console.Write("""
                  Decompression

                  Enter the path to a SEP directory file.

                
                """);

            string inPath = ReadInput("[PATH] ");
            if (!File.Exists(inPath))
            {
                Console.Write($"File at path \"{inPath}\" does not exist.");

                Thread.Sleep(2500);
                DecompressionTUI();
            }
            
            Console.Write("""
                  
                  Specify the output path (defaults to the packagers path)

                
                """);

            string outPath = ReadInput("[PATH] ");

            Decompression.Decompress(inPath, outPath);
        }

        public static string ReadInput(string type = "")
        {
            Console.Write($"  > {type}");
            string input = Console.ReadLine()!.ToString().Trim();
            Console.Title = "SEP Packager";

            return input;
        }
    }

}
