using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImproService
{
    class Utility
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(byte[] b1, byte[] b2, long count);

        private static byte[] EmptyHash { get; } = new byte[]
        {
            0x31, 0xd6, 0xcf, 0xe0, 0xd1, 0x6a, 0xe9, 0x31,
            0xb7, 0x3c, 0x59, 0xd7, 0xe0, 0xc0, 0x89, 0xc0
        };

        public static bool CompareEmpty(byte[] b)
        {
            return CompareBytes(b, EmptyHash);
        }

        public static bool CompareBytes(byte[] b1, byte[] b2)
        {
            return b1.Length == b2.Length && memcmp(b1, b2, b1.Length) == 0;
        }

        public class ByteArrayComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] a, byte[] b)
            {
                if (a.Length != b.Length)
                    return false;

                for (int i = 0; i < a.Length; i++)
                {
                    if (a[i] != b[i])
                        return false;
                }

                return true;
            }

            public int GetHashCode(byte[] a)
            {
                uint b = 0;

                for (int i = 0; i < a.Length; i++)
                    b = ((b << 23) | (b >> 9)) ^ a[i];

                return unchecked((int)b);
            }
        }
    }
}
