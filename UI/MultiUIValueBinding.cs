using Game.SceneFlow;
using System;

namespace Belzont.Utils
{
    public class MultiUIValueBinding<B, F>
    {
        public B Value
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
        private B m_value;
        private readonly Action<string, object[]> eventCaller;
        private readonly string m_propertyPrefix;
        private readonly Func<F, MultiUIValueBinding<B, F>, B> m_dataNormalizeFn;
        private readonly Func<B, MultiUIValueBinding<B, F>, F> m_frontendTransformFn;
        private readonly bool m_isReadOnlyForUI;
        public event Action<B> OnScreenValueChanged;

        public MultiUIValueBinding(B initialValue, string propertyPrefix, Action<string, object[]> euisEventCaller, Action<string, Delegate> callBinder, Func<B, MultiUIValueBinding<B, F>, F> frontendTransformFn, Func<F, MultiUIValueBinding<B, F>, B> dataNormalizeFn, bool isReadOnlyForUI = false)
        {
            m_value = initialValue;
            m_propertyPrefix = propertyPrefix;
            if (!isReadOnlyForUI) callBinder($"{propertyPrefix}!", OnUiValueChanged);
            callBinder($"{propertyPrefix}?", GetValueUI);
            eventCaller = euisEventCaller;
            m_dataNormalizeFn = dataNormalizeFn;
            m_frontendTransformFn = frontendTransformFn;
            m_isReadOnlyForUI = isReadOnlyForUI;
        }

        private object GetValueUI() => m_frontendTransformFn(Value, this);

        private void OnUiValueChanged(F newValue)
        {
            if (m_isReadOnlyForUI) return;
            m_value = m_dataNormalizeFn(newValue, this);
            OnScreenValueChanged?.Invoke(m_value);
            UpdateUIs();
        }

        public void ChangeValueWithEffects(F newValue)
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


    public class MultiUIValueBinding<T>(T initialValue, string propertyPrefix, Action<string, object[]> euisEventCaller, Action<string, Delegate> callBinder, Func<T, MultiUIValueBinding<T, T>, T> dataNormalizeFn = null, bool isReadOnlyForUI = false)
        : MultiUIValueBinding<T, T>(initialValue, propertyPrefix, euisEventCaller, callBinder, (x, _) => x, dataNormalizeFn ?? ((x, _) => x), isReadOnlyForUI)
    {
    }

}