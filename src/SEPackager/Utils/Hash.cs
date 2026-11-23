using SEPackager.Compression;
using System.Runtime.CompilerServices;
using System.Text;
using w3.CRC;

namespace SEPackager.Utils
{
    public static class Hash
    {
        private static readonly int bucketCount = (int)(FileCheck.filePaths.Length * 1.8f);
        private static short[] _collisionDetection1 = new short[bucketCount];
        private static short[] _collisionDetection2 = new short[bucketCount];
        private static int[] _hashEntryOffset = new int[bucketCount];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashFunction(uint x) => (int)Math.Floor(bucketCount * (x * 0.6180339887 % 1.0f));  // Change the hash function if shit breaks

        public static void PrepareHash(string path)
        {
            int hashIndex = HashFunction(CRC32.ComputeChecksum(Encoding.UTF8.GetBytes(path)));

            while (_collisionDetection1[hashIndex] != 0) hashIndex = (hashIndex + 1) % _collisionDetection1.Length;  // Checks if the space is occupied
            _collisionDetection1[hashIndex] = (short)CRC16.ComputeChecksum(Encoding.UTF8.GetBytes(path));  // First CRC check for full file name
            _collisionDetection2[hashIndex] = (short)CRC16.ComputeChecksum(Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(path)));  // Second CRC check for file name without extension
            _hashEntryOffset[hashIndex] = Archive.entryOffset;
        }
        
        public static void WriteHash(BinaryWriter writer)
        {
            for (int i = 0; i < bucketCount; i++)
            {
                writer.Write(_collisionDetection1[i]);
                writer.Write(_collisionDetection2[i]);
                writer.Write(_hashEntryOffset[i]);

                writer.Write(CRC8.ComputeChecksum([
                    (byte)_collisionDetection1[i],
                    (byte)(_collisionDetection1[i] >> 8),
                    (byte)_collisionDetection2[i],
                    (byte)(_collisionDetection2[i] >> 8),
                    (byte)_hashEntryOffset[i],
                    (byte)(_hashEntryOffset[i] >> 8),
                    (byte)(_hashEntryOffset[i] >> 16),
                    (byte)(_hashEntryOffset[i] >> 24)
                ]));
            }
        }

        public static void Dispose()
        {
            _collisionDetection1 = [];
            _collisionDetection2 = [];
            _hashEntryOffset = [];
        }
    }
}