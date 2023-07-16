using Belzont.Utils;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Jobs;

namespace Belzont.Serialization
{
    internal struct BelzontSerializeJob<TWriter, B> : IJob where TWriter : struct, IWriter where B : ComponentSystemBase, IBelzontSerializableSingleton<B>, new()
    {
        public void Execute()
        {
            TWriter writer = this.m_WriterData.GetWriter<TWriter>();
            World.All[m_WorldIndex].GetExistingSystemManaged<B>().Serialize(writer);
            LogUtils.DoLog($"Serialized {typeof(B)}");
        }
        public int m_WorldIndex;
        public EntityWriterData m_WriterData;
    }

}

