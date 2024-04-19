using Colossal.Serialization.Entities;
using Unity.Collections;

namespace Belzont.Utils
{
    public static class FixedStringExtensions
    {
        public static void Write(this IWriter writer, FixedString512Bytes text)
        {
            var arraySave = new NativeArray<byte>(ZipUtils.Zip(text.ToString()), Allocator.Temp);
            writer.Write(arraySave.Length);
            writer.Write(arraySave);
            arraySave.Dispose();
        }

        public static void Read(this IReader reader, out FixedString512Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString128Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString64Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString32Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString4096Bytes text) => text = ReadString(reader);

        private static string ReadString(IReader reader)
        {
            string text;
            reader.Read(out int size);
            NativeArray<byte> byteNativeArray = new(new byte[size], Allocator.Temp);
            reader.Read(byteNativeArray);
            try
            {
                text = ZipUtils.Unzip(byteNativeArray.ToArray());
            }
            catch
            {
                text = "<FAILED LOADING>";
            }
            byteNativeArray.Dispose();
            return text;
        }
    }
}