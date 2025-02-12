using System.Collections.Generic;
using System.Linq;

namespace Belzont.Utils
{
    public static class ByteArrayUtils
    {
        public static IEnumerable<byte[]> Chunk(this byte[] value, int bufferLength)
        {
            int countOfArray = value.Length / bufferLength;
            if (value.Length % bufferLength > 0)
                countOfArray++;
            for (int i = 0; i < countOfArray; i++)
            {
                yield return value.Skip(i * bufferLength).Take(bufferLength).ToArray();

            }
        }
    }
}
