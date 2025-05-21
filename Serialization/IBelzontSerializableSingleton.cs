using Colossal.Serialization.Entities;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace Belzont.Serialization
{
    public interface IBelzontSerializableSingleton<B> : IJobSerializable where B : ComponentSystemBase, IBelzontSerializableSingleton<B>, new()
    {
        World World { get; }
        public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter;
        public void Deserialize<TReader>(TReader reader) where TReader : IReader;

        internal sealed void CheckVersion<TReader>(TReader reader, uint currentVersion) where TReader : IReader
        {
            reader.Read(out uint version);
            if (version > currentVersion)
            {
                throw new Exception($"Invalid version of {GetType()}!");
            }
        }

        JobHandle IJobSerializable.Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps)
        {
            BelzontSerializeJob<TWriter, B> jobData = default;
            jobData.m_WriterData = writerData;
            jobData.m_WorldIndex = -1;
            for (int i = 0; i < World.All.Count; i++)
            {
                if (World.All[i] == World)
                {
                    jobData.m_WorldIndex = i;
                    break;
                }
            }
            return jobData.Schedule(inputDeps);
        }

        JobHandle IJobSerializable.Deserialize<TReader>(EntityReaderData readerData, JobHandle inputDeps)
        {
            BelzontDeserializeJob<TReader, B> jobData = default;
            jobData.m_ReaderData = readerData;
            jobData.m_WorldIndex = -1;
            for (int i = 0; i < World.All.Count; i++)
            {
                if (World.All[i] == World)
                {
                    jobData.m_WorldIndex = i;
                    break;
                }
            }
            inputDeps = jobData.Schedule(inputDeps);
            return inputDeps;
        }

    }

}

