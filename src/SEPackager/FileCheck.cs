namespace SEPackager
{
    enum FileCompression
    {
        gif  = 0,

        txt  = 1,
        json = 1,
        cfg  = 1,
        bsp  = 1,

        sem  = 1,

        gltf = 1,
        fbx  = 1,
        obj  = 1,

        ttf  = 1,
        otf  = 1,

        png  = 2,
        jpg  = 2,
        jpeg = 2,

        ogg  = 3,
        mp3  = 3,
        wav  = 3,
        
        webm = 4,
        mp4  = 4,
    }

    internal class FileCheck
    {
        public static Dictionary<int, byte>? files;
        public static string[] filePaths = Directory.GetFiles(Program.inPath, "*", SearchOption.AllDirectories);

        public static void GetFileExtensions()
        {
            files = new(filePaths.Length);

            for (int i = 0; i < filePaths.Length; i++)
            {
                string extension = Path.GetExtension(filePaths[i]).TrimStart('.');

                if (Enum.TryParse(extension, true, out FileCompression fc)) files.Add(i, (byte)fc);
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