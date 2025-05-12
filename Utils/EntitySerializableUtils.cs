#if BEPINEX_CS2
using BepInEx.Logging;
#endif
using Colossal.Serialization.Entities;
using System;
using Unity.Collections;

namespace Belzont.Utils
{
    public static class EntitySerializableUtils
    {
        public static uint CheckVersionK45(this IReader reader, uint currentVersion, Type currentType)
        {
            reader.Read(out uint version);
            if (version > currentVersion)
            {
                throw new Exception($"Invalid version of {currentType.FullName}! ({version} > {currentVersion})");
            }
            return version;
        }

        public static void Read<IEnum>(this IReader reader, out IEnum result, IEnum defaultValue = default) where IEnum : struct, Enum
        {
            reader.Read(out string value);
            if (!Enum.TryParse(value, out result))
            {
                result = defaultValue;
            }
        }
        public static void ReadAsInt<IEnum>(this IReader reader, out IEnum result) where IEnum : struct, Enum
        {
            reader.Read(out int value);
            if (!Enum.TryParse(value.ToString(), out result))
            {
                result = default;
            }
        }
        public static void Write<IEnum>(this IWriter writer, IEnum result) where IEnum : struct, Enum
        {
            writer.Write(result.ToString());
        }
        public static void Write<T>(this IWriter writer, NativeList<T> input) where T : unmanaged, ISerializable
        {
            var length = input.Length;
            writer.Write(length);
            for (int i = 0; i < length; i++)
            {
                writer.Write(input[i]);
            }
        }
        public static void Read<T>(this IReader reader, ref NativeList<T> input) where T : unmanaged, ISerializable
        {
            reader.Read(out int length);
            if (input.IsCreated)
            {
                input.Clear();
            }
            else
            {
                input = new NativeList<T>(Allocator.Persistent);
            }
            for (int i = 0; i < length; i++)
            {
                reader.Read(out T item);
                input.Add(item);
            }
        }

        public static void Write(this IWriter writer, byte[] bytes)
        {
            if (bytes == null)
            {
                writer.Write(-1);
                return;
            }
            using var mainTexBytes = new NativeArray<byte>(bytes, Allocator.Temp);
            writer.Write(mainTexBytes.Length);
            writer.Write(mainTexBytes);
        }
        public static void Read(this IReader reader, out byte[] bytes)
        {
            reader.Read(out int length);
            if (length < 0)
            {
                bytes = null;
                return;
            }
            if (length == 0)
            {
                bytes = new byte[0];
                return;
            }
            using var texBytes = new NativeArray<byte>(length, Allocator.Temp);
            reader.Read(texBytes);
            bytes = texBytes.ToArray();
            texBytes.Dispose();
        }
    }
}
