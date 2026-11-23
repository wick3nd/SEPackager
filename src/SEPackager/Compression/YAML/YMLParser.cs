using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SEPackager.Compression.YAML
{
    internal static class YMLParser
    {
        public static void Parse()
        {
            var file = Path.Combine(Directory.GetCurrentDirectory(), @"\overerides.yaml");

            // YAML Decoding
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();

            var root = deserializer.Deserialize<Root>(file);

            for (int i = 0; i < root.Files.Count; i++)
            {
                Console.WriteLine(root.Files[i].Path);
                if (root.Files[i].Compression != null) Console.WriteLine(root.Files[i].Compression);
                if (root.Files[i].AQuality != null) Console.WriteLine(root.Files[i].AQuality);
                if (root.Files[i].BC != null) Console.WriteLine(root.Files[i].BC);
            }
        }
    }
}