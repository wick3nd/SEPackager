using System.Text.RegularExpressions;

namespace SEPackager
{
    internal partial class InputMgr
    {
        public static void ListenMain()
        {
            string input = ReadInput();
            switch (input)
            {
                case "1": ViewMgr.ShowCompres();
                    break;

                case "2": Environment.Exit(0);
                    break;

                case "3": Environment.Exit(0);
                    break;

                case "4": Environment.Exit(0);
                    break;

                case "0": Environment.Exit(0);
                    break;

                default: ViewMgr.ShowMain();
                    break;
            }
        }

        public static void ListenCompres()
        {
           // Name of the archives
            SetCursorPos(0, 12);

            string name = "";
            while (name == "")
            {
                SetCursorPos(0, 12);
                name = ReadInput();
            }


           // Max size per archive part

           //  ADD A
           //  LIMIT
           //  OF 4GB
           //  TO PREVENT
           //  CORRUPTION

            SetCursorPos(0, 16);
            string defaultMaxMB = "100";

            string input = ReadInput();
            if (input == "")
            {
                input = defaultMaxMB;
                SetCursorPos(4, 16);
                Console.Write(defaultMaxMB);
            }
            input = MyRegex().Replace(input, "");  // Keep only digits - no floats, letters, special characters, signs, etc.

           // Pass it all to compression
            Archive.archName = name;
            Archive.bytesPerArchive = Convert.ToUInt32(input) * 1048576;  // MiB -> B
        }

        private static string ReadInput(string type = "")
        {
            Console.Write($"  > {type}");
            string input = Console.ReadLine()!.ToString().Trim();

            return input;
        }

        private static void SetCursorPos(int x, int y)
        {
            Console.CursorLeft = x;
            Console.CursorTop = y;
        }


        [GeneratedRegex(@"[^\d]")]
        private static partial Regex MyRegex();
    }
}