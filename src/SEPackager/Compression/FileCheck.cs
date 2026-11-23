using SEPackager.Utils;

namespace SEPackager.Compression
{
    internal class FileCheck
    {
        public static readonly Dictionary<string, byte> FileCompression = new(StringComparer.OrdinalIgnoreCase)
        {
            // 0 - No compression
            // 1 - misceleanous files - LZMA
            // 2 - misceleanous files - ZSTD
            // 3 - misceleanous files - LZ4
            // 4 - Images - Block Compression
            // 5 - sound files - Any to OGG(opus)
            // 6 - video files - Any to webm(VP9)

            ["gif"]    = 0,
            ["yml"]    = 0,
            ["yaml"]   = 0,
            ["glsl"]   = 0,
            ["shader"] = 0,
            ["vert"]   = 0,
            ["frag"]   = 0,

            ["txt"]    = 1,
            ["json"]   = 1,
            ["cfg"]    = 1,
            ["bsp"]    = 1,
            ["gltf"]   = 1,
            ["fbx"]    = 1,
            ["ttf"]    = 1,
            ["otf"]    = 1,
            ["bin"]    = 1,

            ["png"]    = 4,
            ["jpg"]    = 4,
            ["jpeg"]   = 4,
            ["webp"]   = 4,
            ["tiff"]   = 4,
            ["bmp"]    = 4,

            ["ogg"]    = 5,
            ["mp3"]    = 5,
            ["wav"]    = 5,
            ["aac"]    = 5,
            ["m4a"]    = 5,
            ["opus"]   = 5,
            ["flac"]   = 5,
            ["aiff"]   = 5,

            ["webm"]   = 6,
            ["mp4"]    = 6,
            ["mkv"]    = 6,
            ["mov"]    = 6,
            ["avi"]    = 6,
            ["flv"]    = 6,
        };

        public static byte[]? files;  // Stores file path index alongside its compression type
        public static string[] filePaths = Directory.GetFiles(Program.inPath, "*", SearchOption.AllDirectories);

        public static void GetFileExtensions()
        {
            files = new byte[filePaths.Length];

            for (int i = 0; i < filePaths.Length; i++)
            {
                string extension = Path.GetExtension(filePaths[i]).TrimStart('.');

                if (FileCompression.TryGetValue(extension, out byte value))
                {
                    files[i] = value;
                    continue;
                }

                SEDebug.Log(SEDebugState.Warning, $"Extension not supported for use in SteelEngine: {extension}. Open an issue on github to request adding this extension; file has not been compressed.");
                files[i] = 0;
            }
        }

        public static int GetFileCount() => filePaths.Length;
        public static void Dispose()
        {
            files = [];
            filePaths = [];
        }
    }
}