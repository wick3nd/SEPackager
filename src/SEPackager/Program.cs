namespace SEPackager
{
    internal class Program
    {
        // Only change the version if the reading/writing of the files is changed
        public const byte VerMajor = 0;  // 2 bit number - [0, 3]
        public const byte VerMinor = 5;  // 6 bit number - [0, 63]

        public static readonly string logPath = Path.Combine(Directory.GetCurrentDirectory(), @"Logs\");
        public static readonly string outPath = Path.Combine(Directory.GetCurrentDirectory(), @"Output\");
        public static readonly string inPath = Path.Combine(Directory.GetCurrentDirectory(), @"Input\");

        private static void Main()
        {
            Console.Title = "SEPackager";
            CreateFolders();
            ViewMgr.ShowMain();

            Console.ReadKey();
        }

        public static void CreateFolders()
        {
            if (!Directory.Exists(outPath)) Directory.CreateDirectory(outPath);
            if (!Directory.Exists(inPath)) Directory.CreateDirectory(inPath);
        }
    }
}