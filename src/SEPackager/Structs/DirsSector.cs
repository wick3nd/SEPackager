using System.Text;

namespace SEPpackager.Structs
{
    internal class DirsSector
    {
        public string Path { get; set; } = "error/possibly?";
        public byte ArchivePointer { get; set; } = 0x00;
        public uint Offset { get; set; } = 11;
        public uint Length { get; set; } = 0;

        public ReadOnlyMemory<byte> GetArray()
        {
            byte[] buffer = new byte[Path.Length + 9];

            buffer[0] = (byte)Path.Length;

            Encoding.UTF8.GetBytes(Path, buffer.AsSpan(1));

            buffer[Path.Length + 1] = ArchivePointer;

            buffer[Path.Length + 2] = (byte)(Offset >> 24);
            buffer[Path.Length + 3] = (byte)(Offset >> 16);
            buffer[Path.Length + 4] = (byte)(Offset >> 8);
            buffer[Path.Length + 5] = (byte)Offset;

            buffer[Path.Length + 6] = (byte)(Length >> 24);
            buffer[Path.Length + 7] = (byte)(Length >> 16);
            buffer[Path.Length + 8] = (byte)(Length >> 8);
            buffer[Path.Length + 9] = (byte)Length;

            return buffer;
        }
    }
}
