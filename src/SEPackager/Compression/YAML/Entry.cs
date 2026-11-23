namespace SEPackager.Compression.YAML
{
    internal class Entry
    {
        public string Path { get; set; }
        public string Compression { get; set; }
        public int? BC { get; set; }
        public int? AQuality { get; set; }
    }
}