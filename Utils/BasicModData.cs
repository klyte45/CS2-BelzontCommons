using Belzont.Utils;
using Colossal.Logging;
using Game.Modding;
using Game.Settings;
using Game.UI.Widgets;
using System;


namespace Belzont.Interfaces
{

    [SettingsUIShowGroupName(kLogSection)]
    public abstract partial class BasicModData : ModSetting

    {
        public const string kAboutTab = "About";
        public const string kLogSection = "Logging";
        private LogLevel loggingLevel = LogLevel.Normal;
        private bool logStacktraces = true;
        private bool showErrorsPopups = false;

        public event Action<LogLevel> OnLoggingEnabledChanged;

        public static BasicModData Instance { get; private set; }

        public BasicIMod ModInstance { get; private set; }
        protected BasicModData(IMod mod) : base(mod)
        {
            ModInstance = mod as BasicIMod;
        }


        [SettingsUISection(kAboutTab, kLogSection)]
        [SettingsUIDropdown(typeof(BasicModData), nameof(GetLogLevelsDropdownItems))]
        public LogLevel LoggingLevel
        {
            get => loggingLevel; set
            {
                loggingLevel = value;
                LogUtils.SetLogLevel(GetEffectiveLogLevel(loggingLevel));
                LogUtils.SetStackTracing(loggingLevel > LogLevel.Normal && logStacktraces);
                LogUtils.SetDisplayErrorsOnUI(loggingLevel > LogLevel.Normal && showErrorsPopups);
                OnLoggingEnabledChanged?.Invoke(value);
            }
        }

        [SettingsUISection(kAboutTab, kLogSection)]
        [SettingsUIHideByCondition(typeof(BasicModData), nameof(ShowLogStacktraces))]
        public bool LogStacktraces
        {
            get => logStacktraces; set
            {
                logStacktraces = value;
                LogUtils.SetStackTracing(loggingLevel > LogLevel.Normal && value);
            }
        }

        [SettingsUISection(kAboutTab, kLogSection)]
        public bool ShowErrorsPopups
        {
            get => showErrorsPopups && LogUtils.GetDisplayErrorsOnUI(); set
            {
                showErrorsPopups = value;
                LogUtils.SetDisplayErrorsOnUI(value);
            }
        }

        public bool ShowLogStacktraces() => loggingLevel == LogLevel.Normal;

        private Level GetEffectiveLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Verbose => Level.Verbose,
                LogLevel.Trace => Level.Trace,
                LogLevel.Debug => Level.Debug,
                _ => Level.Info
            };
        }

        public DropdownItem<int>[] GetLogLevelsDropdownItems()
        {
            var items = new DropdownItem<int>[]
            {
                new() {
                    value = (int)LogLevel.Normal,
                    displayName = GetEnumValueLocaleID(LogLevel.Normal),
                },
                new() {
                    value = (int)LogLevel.Debug,
                    displayName = GetEnumValueLocaleID(LogLevel.Debug),
                },
                new() {
                    value = (int)LogLevel.Trace,
                    displayName = GetEnumValueLocaleID(LogLevel.Trace),
                },
                new() {
                    value = (int)LogLevel.Verbose,
                    displayName = GetEnumValueLocaleID(LogLevel.Verbose),
                },
            };

            return items;
        }

        [SettingsUISection(kAboutTab, null)]
        public string Version => BasicIMod.FullVersion;

        public sealed override void SetDefaults()
        {
            LoggingLevel = LogLevel.Normal;
            OnSetDefaults();
        }

        public abstract void OnSetDefaults();

        public string GetEnumValueLocaleID(string classname, string value) => $"Options.{id}.{classname.ToUpper()}[{value}]";

        public enum LogLevel
        {
            Normal,
            Debug,
            Trace,
            Verbose
        }
    }
}