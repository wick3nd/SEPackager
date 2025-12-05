using SEPackager.Compression;
using SEPackager.TUI;
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
           // Name of the archive/s
            SetCursorPos(0, 12);

            string name = "";
            while (name == "")
            {
                SetCursorPos(0, 12);
                name = ReadInput();
            }

           // Max size per archive parts
            SetCursorPos(0, 16);

            string input = MyRegex().Replace(ReadInput(), "");  // Keep only digits - no floats, letters, special characters, signs, etc.
            uint value = input != "" ? Convert.ToUInt32(input) : 100;  // If the string is not empty(has numbers) it converts the int, otherwise it defaults to 100
            uint finalValue = value > 20 ? (value > 4000 ? 4000 : value) : 20;  // If less than 20MB, goes back to 20MB(to prevent filling your folder by accident), if more then 4000, it goes back to 4000(uint limits)

           // Go back and clear the line
            Console.SetCursorPosition(0, 16);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, 16);

            Console.Write($"  > {finalValue}MB");  // Write the final value

           // Pass it all to compression
            Archive.archName = name;
            Archive.bytesPerArchive = finalValue * 1048576;  // MiB -> B
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