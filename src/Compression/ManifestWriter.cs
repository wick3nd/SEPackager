namespace SEPackager.Compression
{
    internal class ManifestWriter
    {
        private static readonly StreamWriter writer = new($"{Program.outPath}{Archive.archName}_manifest.csv", true);
        
        public static async void Write(uint offset, string path) => await writer.WriteLineAsync($"{offset}|{path}");

        public static void Dispose()
        {
            writer.Flush();
            writer.Dispose();
        }
    }
}
