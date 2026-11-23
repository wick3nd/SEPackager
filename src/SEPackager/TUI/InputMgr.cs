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

           // Input handling
            string input = ReadInput().Replace(".", ",").Split(",")[0];
            string regexProcess = MyRegex().Replace(input, "");  // Keep only digits - no letters, special characters, signs, etc.
            uint value = regexProcess != "" ? (regexProcess.Length > 4 ? 4000 : Convert.ToUInt32(regexProcess)) : 100;  // If the string is not empty(has numbers) it converts to uint, otherwise it defaults to 100
            uint finalValue = Math.Clamp(value, 20, 4000);  // If less than 20MB, goes back to 20MB(to prevent filling your folder by accident), if more then 4000, it goes back to 4000(uint limits)

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

        [GeneratedRegex(@"[^0-9,]")]
        private static partial Regex MyRegex();
    }
}