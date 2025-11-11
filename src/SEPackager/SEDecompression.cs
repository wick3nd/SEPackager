using SEPpackager.CRC;
using System.ComponentModel;

namespace SEPpackager
{
    internal class SEDecompression
    {
        internal const byte supportedMajorVer = 0x00;
        internal const byte supportedMinorVer = 0x06;

        internal static byte type;
        internal static uint fileCount;

        internal static string[]? paths;
        internal static byte[]? archivePointer;
        internal static uint[]? offset;
        internal static uint[]? length;
        internal static uint[]? originalLength;
        internal static byte[]? CRC;

        public static void InitializePackage(string path)
        {
            byte[] buffer;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // Read the header
            buffer = br.ReadBytes(16);

            // Validate the file
            if (CRC8.CheckChecksum(buffer) && !buffer[..^8].SequenceEqual("SEP.DIRS"u8.ToArray())) SEDebug.Log(SEDebugState.Error, $"[{path}] is not a valid SEP file");
            
            // Version check
            if (buffer[^8] != supportedMajorVer && buffer[^7] != supportedMinorVer) SEDebug.Log(SEDebugState.Warning, $"[{path}] version is not up to date may not work properly. Current supported version: {supportedMajorVer:D1}.{supportedMinorVer:D3}");

            type = br.ReadByte();
            fileCount = br.ReadUInt32();

            // Initialize the arrays
            paths = new string[fileCount];
            archivePointer = new byte[fileCount];
            offset = new uint[fileCount];
            length = new uint[fileCount];
            if (type == 0) originalLength = new uint[fileCount];
            CRC = new byte[fileCount];

            // Cache the data
            for (int i = 0; i < fileCount; i++)
            {
                paths[i] = br.ReadString();
                archivePointer[i] = br.ReadByte();
                offset[i] = br.ReadUInt32();
                length[i] = br.ReadUInt32();
                if (type == 0) originalLength![i] = br.ReadUInt32();
                CRC[i] = br.ReadByte();
            }
        }
    }
}