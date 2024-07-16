using Game.SceneFlow;
using System;

namespace Belzont.Utils
{
    public class MultiUIValueBinding<T, U>
    {
        public T Value
        {
            get => m_value; set
            {
                if (!Equals(value, m_value))
                {
                    m_value = value;
                    UpdateUIs();
                }
            }
        }
        private T m_value;
        private readonly Action<string, object[]> eventCaller;
        private readonly string m_propertyPrefix;
        private readonly Func<U, MultiUIValueBinding<T, U>, T> m_dataNormalizeFn;
        private readonly Func<T, MultiUIValueBinding<T, U>, U> m_frontendTransformFn;
        public event Action<T> OnScreenValueChanged;

        public MultiUIValueBinding(T initialValue, string propertyPrefix, Action<string, object[]> euisEventCaller, Action<string, Delegate> callBinder, Func<T, MultiUIValueBinding<T, U>, U> frontendTransformFn, Func<U, MultiUIValueBinding<T, U>, T> dataNormalizeFn)
        {
            m_value = initialValue;
            m_propertyPrefix = propertyPrefix;
            callBinder($"{propertyPrefix}!", OnUiValueChanged);
            callBinder($"{propertyPrefix}?", GetValueUI);
            eventCaller = euisEventCaller;
            m_dataNormalizeFn = dataNormalizeFn;
            m_frontendTransformFn = frontendTransformFn;
        }

        private object GetValueUI() => m_frontendTransformFn(Value, this);

        private void OnUiValueChanged(U newValue)
        {
            m_value = m_dataNormalizeFn(newValue, this);
            OnScreenValueChanged?.Invoke(m_value);
            UpdateUIs();
        }

        public void ChangeValueWithEffects(U newValue)
        {
            m_value = m_dataNormalizeFn(newValue, this);
            OnScreenValueChanged?.Invoke(m_value);
            UpdateUIs();
        }

        public void UpdateUIs()
        {
            var valueToSend = GetValueUI();
            eventCaller($"{m_propertyPrefix}->", new object[] { valueToSend });
            if (GameManager.instance.userInterface.view.View.IsReadyForBindings()) GameManager.instance.userInterface.view.View.TriggerEvent($"{m_propertyPrefix}->", valueToSend);
        }

    }


    public class MultiUIValueBinding<T> : MultiUIValueBinding<T, T>
    {
        public MultiUIValueBinding(T initialValue, string propertyPrefix, Action<string, object[]> euisEventCaller, Action<string, Delegate> callBinder, Func<T, MultiUIValueBinding<T, T>, T> dataNormalizeFn = null)
            : base(initialValue, propertyPrefix, euisEventCaller, callBinder, (x, _) => x, dataNormalizeFn ?? ((x, _) => x))
        {
        }
    }

}