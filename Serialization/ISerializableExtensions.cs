using Colossal.Serialization.Entities;

namespace Belzont.Serialization
{
    public static class ISerializableExtensions
    {
        public static void WriteNullCheck<TWriter, TSerializable>(this TWriter writer, TSerializable item) where TWriter : IWriter where TSerializable : class, ISerializable
        {
            if (item is null) writer.Write(false);
            else
            {
                writer.Write(true);
                writer.Write(item);
            }
        }
        public static void ReadNullCheck<TReader, TSerializable>(this TReader reader, out TSerializable item) where TReader : IReader where TSerializable : class, ISerializable, new()
        {
            reader.Read(out bool hasSelf);
            if (hasSelf)
            {
                item = new();
                reader.Read(item);
            }
            else
            {
                item = null;
            }
        }
    }

}

