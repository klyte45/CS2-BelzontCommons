using System;

namespace Belzont.Interfaces
{
    public interface IBelzontBindable
    {
        public void SetupCaller(Action<string, object[]> eventEmitter);
        public void SetupEventBinder(Action<string, Delegate> eventBinderFn);
        public void SetupCallBinder(Action<string, Delegate> callBinderFn);
    }
}
