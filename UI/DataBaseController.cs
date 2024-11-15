using Belzont.Interfaces;
using Game.SceneFlow;
using System;
using Unity.Entities;

namespace Belzont.Utils
{
    public abstract class DataBaseController : ComponentSystemBase, IBelzontBindable
    {
        protected Action<string, object[]> EventCaller { get; private set; }
        protected Action<string, Delegate> CallBinder { get; private set; }

        protected bool m_initialized = false;

        public void SetupCallBinder(Action<string, Delegate> callBinder)
        {
            CallBinder = callBinder;
            if (EventCaller != null) InitValueBindings();
        }
        public void SetupEventBinder(Action<string, Delegate> eventBinder)
        {
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            EventCaller = eventCaller;
            if (CallBinder != null) InitValueBindings();
        }

        private void InitValueBindings()
        {
            if (m_initialized) return;
            DoInitValueBindings();
            m_initialized = true;
        }

        protected abstract void DoInitValueBindings();
        public override void Update() { }
        protected override void OnCreate()
        {
            base.OnCreate();
            GameManager.instance.userInterface.view.Listener.BindingsReleased += () => m_initialized = false;
        }
        public abstract void OnCurrentItemChanged(Entity newSelection);
    }
}