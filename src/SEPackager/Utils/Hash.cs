using w3.CRC;
using System.Runtime.CompilerServices;
using System.Text;

class Hash
{
    public readonly uint bucketCount;

    // List of integers to store values
    public List<uint>[] table;

    public Hash(uint buckets)
    {
        bucketCount = buckets;

        table = new List<uint>[bucketCount];
        for (uint i = 0; i < bucketCount; i++) table[i] = [];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint HashFunction(uint x) => (uint)Math.Floor(bucketCount * (x * 0.6180339887 % 1.0f));  // Change the hash function if shit breaks

    // Inserts a key into the hash table
    public void InsertItem(string key)
    {
        uint CRC = CRC32.ComputeChecksum(Encoding.UTF8.GetBytes(key));
        uint index = HashFunction(CRC);

        table[index].Add(CRC);
    }

    // Deletes a key from the hash table
    /*
    public void DeleteItem(string key)
    {
        uint CRC = CRC32.ComputeChecksum(Encoding.UTF8.GetBytes(key));

        uint index = HashFunction(CRC);
        table[index].Remove(CRC);
    }
    */

    public void GetHash(out List<uint>[] hashTable) => hashTable = table;
    public void Dispose() => table = [];
}