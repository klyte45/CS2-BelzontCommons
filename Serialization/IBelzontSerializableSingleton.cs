using Colossal.Serialization.Entities;
using System;
using System.Linq;
using Unity.Entities;
using Unity.Jobs;

namespace Belzont.Serialization
{
    internal interface IBelzontSerializableSingleton<B> : IJobSerializable where B : ComponentSystemBase, IBelzontSerializableSingleton<B>, new()
    {
        World World { get; }
        internal void Serialize<TWriter>(TWriter writer) where TWriter : IWriter;
        internal void Deserialize<TReader>(TReader reader) where TReader : IReader;

        JobHandle IJobSerializable.Serialize<TWriter>(EntityWriterData writerData, JobHandle inputDeps)
        {
            BelzontSerializeJob<TWriter, B> jobData = default;
            jobData.m_WriterData = writerData;
            jobData.m_WorldIndex = -1;
            for(int i = 0; i< World.All.Count; i++)
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

