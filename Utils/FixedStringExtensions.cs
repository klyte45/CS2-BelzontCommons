using Colossal.Serialization.Entities;
using Unity.Collections;

namespace Belzont.Utils
{
    public static class FixedStringExtensions
    {
        public static void Write(this IWriter writer, FixedString4096Bytes text)
        {
            writer.Write(text.ToString());
        }

        public static void Read(this IReader reader, out FixedString512Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString128Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString64Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString32Bytes text) => text = ReadString(reader);
        public static void Read(this IReader reader, out FixedString4096Bytes text) => text = ReadString(reader);

        private static string ReadString(IReader reader)
        {
            reader.Read(out string value);
            return value ?? "";
        }
    }
}