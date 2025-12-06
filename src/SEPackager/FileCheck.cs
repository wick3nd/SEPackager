using SEPackager.Utils;

namespace SEPackager
{
    internal class FileCheck
    {
        public static readonly Dictionary<string, byte> FileCompression = new(StringComparer.OrdinalIgnoreCase)
        {
            ["gif"] = 0,
            ["yml"] = 0,
            ["yaml"] = 0,
            ["glsl"] = 0,
            ["shader"] = 0,
            ["vertex"] = 0,
            ["fragment"] = 0,

            ["txt"] = 1,
            ["json"] = 1,
            ["cfg"] = 1,
            ["bsp"] = 1,
            ["gltf"] = 1,
            ["fbx"] = 1,
            ["ttf"] = 1,
            ["otf"] = 1,

            ["png"] = 2,
            ["jpg"] = 2,
            ["jpeg"] = 2,
            ["webp"] = 2,
            ["tiff"] = 2,
            ["bmp"] = 2,

            ["ogg"] = 3,
            ["mp3"] = 3,
            ["wav"] = 3,
            ["aac"] = 3,
            ["m4a"] = 3,
            ["opus"] = 3,
            ["flac"] = 3,
            ["aiff"] = 3,

            ["webm"] = 4,
            ["mp4"] = 4,
            ["mkv"] = 4,
            ["mov"] = 4,
            ["avi"] = 4,
            ["flv"] = 4,
        };

        public static Dictionary<int, byte>? files;
        public static string[] filePaths = Directory.GetFiles(Program.inPath, "*", SearchOption.AllDirectories);

        public static void GetFileExtensions()
        {
            files = new(filePaths.Length);

            for (int i = 0; i < filePaths.Length; i++)
            {
                string extension = Path.GetExtension(filePaths[i]).TrimStart('.');

                if (FileCompression.TryGetValue(extension, out byte value)) files.Add(i, value);
                else
                {
                    SEDebug.Log(SEDebugState.Warning, $"Extension not fully supported: {extension}. Open an issue on github to request adding this extension; file has not been compressed.");
                    files.Add(i, 0);
                }
            }
        }

        public static uint GetFileCount() => (uint)filePaths.Length;
        public static void Dispose()
        {
            files = [];
            filePaths = [];
        }
    }
}