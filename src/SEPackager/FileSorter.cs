using MimeDetective;
using MimeDetective.Definitions;
using System.Collections.Generic;

namespace SEPpackager
{
    internal class FileSorter
    {
        private static readonly List<string> _texPaths = [];
        private static readonly List<string> _audioPaths = [];
        private static readonly List<string> _miscPaths = [];

        public FileSorter(out string[] texPaths, out string[] audioPaths, out string[] miscPaths)
        {
            TrySortFiles();

            texPaths = [.. _texPaths];
            audioPaths = [.. _audioPaths];
            miscPaths = [.. _miscPaths];
        }

        private static void TrySortFiles()
        {
            string[] allFiles = Directory.GetFiles(Compression.inPath, "*", SearchOption.AllDirectories);
            var inspector = FileInspector();

            Console.Write("  Sorting the files for compression.\n");

            for (int i = 0; i < allFiles.Length; i++)
            {
                var results = inspector.Inspect(allFiles[i]);
                var mimeType = results.ByMimeType().FirstOrDefault()?.MimeType.Split("/")[0] ?? "MISC";

                switch (mimeType)
                {
                    case "IMAGE":
                        _texPaths.Add(allFiles[i]);
                        break;

                    case "VIDEO":
                        _texPaths.Add(allFiles[i]);
                        break;

                    case "AUDIO":
                        _audioPaths.Add(allFiles[i]);
                        break;

                    default:
                        _miscPaths.Add(allFiles[i]);
                        break;
                }

                Console.Write($"  {i} / {allFiles.Length}");
                Console.SetCursorPosition(0, 16);
            }
        }

        public static IContentInspector FileInspector()
        {
            var Inspector = new ContentInspectorBuilder()
            {
                Definitions = new CondensedBuilder()
                {
                    UsageType = MimeDetective.Definitions.Licensing.UsageType.CommercialPaid
                }.Build()
            }.Build();

            return Inspector;
        }
    }
}