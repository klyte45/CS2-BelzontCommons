using Belzont.Utils;
using Colossal;
using Colossal.Logging;
using Colossal.OdinSerializer.Utilities;
using Game.Modding;
using Game.Settings;
using Game.UI.Widgets;
using System;
using System.IO;
using UnityEngine;


namespace Belzont.Interfaces
{

    [SettingsUIShowGroupName(kLogSection, kAlertModalsSection)]
    public abstract partial class BasicModData : ModSetting

    {
        public const string kAboutTab = "About";
        public const string kLogSection = "Logging";
        public const string kAlertModalsSection = "AlertModals";
        private LogLevel loggingLevel = LogLevel.Normal;
        private bool logStacktraces = false;
        private bool showErrorsPopups = false;
        private bool showWhatsNewOnNewVersion = true;
        private string lastWhatsNewVersionShown = string.Empty;

        public event Action<LogLevel> OnLoggingEnabledChanged;

        public static BasicModData Instance { get; private set; }

        public BasicIMod ModInstance { get; private set; }
        protected BasicModData(IMod mod) : base(mod)
        {
            ModInstance = mod as BasicIMod;
            Instance = this;
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
        [SettingsUIHideByCondition(typeof(BasicModData), nameof(ShowLogStacktraces))]
        public bool ShowErrorsPopups
        {
            get => showErrorsPopups && LogUtils.GetDisplayErrorsOnUI(); set
            {
                showErrorsPopups = value;
                LogUtils.SetDisplayErrorsOnUI(value);
            }
        }

        public bool ShowLogStacktraces() => !BasicIMod.DebugMode;

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

        [SettingsUISection(kAboutTab, null)]
        [SettingsUIButtonGroup("1")]
        [SettingsUIButton]
        public bool GoToLocalesFolder { set { RemoteProcess.OpenFolder(BasicIMod.Instance.AdditionalI18nFilesFolder); } }

        [SettingsUISection(kAboutTab, null)]
        [SettingsUIButtonGroup("1")]
        [SettingsUIButton]
        public bool ReloadLocales { set { BasicIMod.Instance.LoadLocales(); } }

        [SettingsUISection(kAboutTab, null)]
        [SettingsUIButtonGroup("2")]
        [SettingsUIHideByCondition(typeof(BasicModData), nameof(CheckForumsLinkUnset))]
        [SettingsUIButton]
        public bool GoToForum { set => Application.OpenURL(BasicIMod.ModAssemblyDescription.ForumsURL); }

        [SettingsUISection(kAboutTab, null)]
        [SettingsUIButtonGroup("2")]
        [SettingsUIHideByCondition(typeof(BasicModData), nameof(CheckGitHubLinkUnset))]
        [SettingsUIButton]
        public bool GoToGitHub { set => Application.OpenURL(BasicIMod.ModAssemblyDescription.GitHubURL); }

        [SettingsUISection(kAboutTab, null)]
        [SettingsUIButtonGroup("3")]
        [SettingsUIButton]
        public bool GoToLogFolder { set => RemoteProcess.OpenFolder(Path.GetDirectoryName(LogUtils.GetLogLocation())); }

        [SettingsUISection(kAboutTab, kAlertModalsSection)]
        public bool ShowWhatsNewOnNewVersion
        {
            get => showWhatsNewOnNewVersion;
            set => showWhatsNewOnNewVersion = value;
        }

        [SettingsUISection(kAboutTab, kAlertModalsSection)]
        [SettingsUIHideByCondition(typeof(BasicModData), nameof(CheckChangelogUnavailable))]
        [SettingsUIButton]
        public bool ShowWhatsNewPanel
        {
            set
            {
                if (BasicIMod.Instance is null) return;
                BasicIMod.Instance.GetManagedSystem<Belzont.Systems.WhatsNewUISystem>()?.RequestShowFromSettings();
            }
        }

        [SettingsUIHidden]
        public string LastWhatsNewVersionShown
        {
            get => lastWhatsNewVersionShown;
            set => lastWhatsNewVersionShown = value ?? string.Empty;
        }

        public sealed override void SetDefaults()
        {
            LoggingLevel = LogLevel.Normal;
            ShowWhatsNewOnNewVersion = true;
            LastWhatsNewVersionShown = string.Empty;
            OnSetDefaults();
        }

        public bool CheckForumsLinkUnset() => BasicIMod.ModAssemblyDescription.ForumsURL.IsNullOrWhitespace();
        public bool CheckGitHubLinkUnset() => BasicIMod.ModAssemblyDescription.GitHubURL.IsNullOrWhitespace();
        public bool CheckChangelogUnavailable() => !KResourceLoader.ResourceExistsMod("changelog.md");

        public abstract void OnSetDefaults();

        public string GetEnumValueLocaleID(string classname, string value) => $"Options.{id}.{classname.ToUpper()}[{value}]";
        public string FixLocaleId(string localeId) => localeId.Replace(GetType().Name, nameof(BasicModData));

        public enum LogLevel
        {
            Normal,
            Debug,
            Trace,
            Verbose
        }
    }
}