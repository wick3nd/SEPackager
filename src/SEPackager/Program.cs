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
                
                  -----------------------------


                """);
        }

        public static void TUI()
        {
            AppendLogo();

            Console.Write("""
                  What would you like to do today?
                
                  1. Compress
                  2. Decompress
                  3. Add to SEP                    (Unavaible, will crash)
                  4. Delete from SEP               (Unavaible, will crash)

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

                Compression.name = name;

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

            _ = new FileSorter(out string[] texPaths, out string[] soundPaths, out string[] miscPaths);

            if (miscPaths.Length > 0) _ = new Compression(miscPaths, Mode.misc);
            if (texPaths.Length > 0) _ = new Compression(texPaths, Mode.tex);
            if (soundPaths.Length > 0) _ = new Compression(soundPaths, Mode.audio);

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
            SEDecompression.InitializePackage(inPath);
            //Decompression.Decompress(inPath, outPath);
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