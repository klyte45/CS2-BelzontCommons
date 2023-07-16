using Belzont.Utils;
using Colossal.Serialization.Entities;
using System;
using Unity.Entities;
using Unity.Jobs;

namespace Belzont.Serialization
{
    internal struct BelzontDeserializeJob<TReader, B> : IJob where TReader : struct, IReader where B : ComponentSystemBase, IBelzontSerializableSingleton<B>, new()
    {
        public void Execute()
        {
            try
            {
                var reader = m_ReaderData.GetReader<TReader>();                
                World.All[m_WorldIndex].GetExistingSystemManaged<B>().Deserialize(reader);

                LogUtils.DoLog($"Deserialized {typeof(B)}");
            }
            catch (Exception e)
            {
                LogUtils.DoWarnLog($"Error loading deserialization for {typeof(B)}!\n{e}");
            }
        }
        public int m_WorldIndex;
        public EntityReaderData m_ReaderData;
    }

}

