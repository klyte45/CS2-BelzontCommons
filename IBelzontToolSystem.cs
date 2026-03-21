using Game;
using Game.Tools;

namespace Belzont.Interfaces
{
    public abstract partial class IBelzontToolSystem : ToolBaseSystem, IBelzontBasicSystem
    {
        protected ToolOutputBarrier Barrier { get; private set; }

        protected abstract void OnCreateWithBarrier();
        protected sealed override void OnCreate()
        {
            base.OnCreate();
            RegisterSystem();
            OnCreateWithBarrier();
        }

        private void RegisterSystem()
        {
            var updateSystem = World.GetOrCreateSystemManaged<UpdateSystem>();
            var UpdateAt = typeof(UpdateSystem).GetMethod("UpdateAt").MakeGenericMethod(GetType());
            UpdateAt.Invoke(updateSystem, [SystemUpdatePhase.ToolUpdate]);
            Barrier = World.GetOrCreateSystemManaged<ToolOutputBarrier>();
        }
    }
}

