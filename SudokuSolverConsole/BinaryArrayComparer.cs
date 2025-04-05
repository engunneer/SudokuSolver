using System.Diagnostics.CodeAnalysis;

namespace SudokuSolverConsole;

public class ByteArrayComparer : IEqualityComparer<byte[]>
{
    public bool Equals(byte[] x, byte[] y) => x.SequenceEqual(y);

    public int GetHashCode([DisallowNull] byte[] data)
    {
        unchecked
        {
            const int p = 16777619;
            int hash = (int)2166136261;

            foreach (var b in data)
            {
                hash = (hash ^ b) * p;
            }

            hash += hash << 13;
            hash ^= hash >> 7;
            hash += hash << 3;
            hash ^= hash >> 17;
            hash += hash << 5;
            return hash;
        }
    }
}

