#if THUNDERSTORE
using Game.Reflection;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
#endif
using Belzont.Utils;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.OdinSerializer.Utilities;
using Colossal.UI;
using Game;
using Game.Modding;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Belzont.Interfaces
{
    public
#if THUNDERSTORE
        interface 
#else
    abstract class
#endif
        IBasicIMod
    {
        protected abstract UpdateSystem UpdateSystem { get; set; }
        public void OnCreateWorld(UpdateSystem updateSystem)
        {
            UpdateSystem = updateSystem;
            Redirector.OnWorldCreated(UpdateSystem.World);
            LoadLocales();
#if THUNDERSTORE
            var optionsList = ((List<OptionsUISystem.Page>)typeof(OptionsUISystem).GetProperty("options", RedirectorUtils.allFlags).GetValue(updateSystem.World.GetOrCreateSystemManaged<OptionsUISystem>()));
            optionsList.Add(Instance.BuildModPage());
#endif

            var uiSys = GameManager.instance.userInterface.view.uiSystem;
            LogUtils.DoLog($"CouiHost => {CouiHost}");
            ((DefaultResourceHandler)uiSys.resourceHandler).HostLocationsMap.Add(CouiHost, new List<string> { ModInstallFolder });

            DoOnCreateWorld(updateSystem);

        }
        public abstract void OnDispose();

#if THUNDERSTORE
        protected abstract void LoadModData();
#endif
        public abstract BasicModData CreateSettingsFile();
        public void OnLoad()
        {
#if THUNDERSTORE
            KFileUtils.EnsureFolderCreation(ModSettingsRootFolder);
            LoadModData();
#else
            ModData = CreateSettingsFile();
            ModData.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(SafeName, ModData);
            KFileUtils.EnsureFolderCreation(ModSettingsRootFolder);
#endif

            Redirector.PatchAll();

            Type[] newComponents = ReflectionUtils.GetStructForInterfaceImplementations(typeof(IComponentData), new[] { GetType().Assembly })
                .Union(ReflectionUtils.GetStructForInterfaceImplementations(typeof(IBufferElementData), new[] { GetType().Assembly })).ToArray();

            if (newComponents.Length > 0)
            {
                LogUtils.DoInfoLog($"Registering {newComponents.Length} components found at mod {SimpleName}");
                if (DebugMode)
                {
                    LogUtils.DoLog("Loaded component count: " + TypeManager.GetTypeCount());
                    LogUtils.DoLog("Loading found components:\n\t" + string.Join("\n\t", newComponents.Select(x => x.ToString())));
                }
                var AddAllComponents = typeof(TypeManager).GetMethod("AddAllComponentTypes", RedirectorUtils.allFlags);
                int startTypeIndex = TypeManager.GetTypeCount();
                Dictionary<int, HashSet<TypeIndex>> writeGroupByType = new();
                Dictionary<Type, int> descendantCountByType = newComponents.Select(x => (x, 0)).ToDictionary(x => x.x, x => x.Item2);

                AddAllComponents.Invoke(null, new object[] { newComponents, startTypeIndex, writeGroupByType, descendantCountByType });

                LogUtils.DoLog("Post loaded component count: " + TypeManager.GetTypeCount());
            }
            else
            {
                LogUtils.DoInfoLog($"No components found at mod {SimpleName}");
            }
            DoOnLoad();
        }

        public abstract void DoOnCreateWorld(UpdateSystem updateSystem);

        public abstract void DoOnLoad();
#if THUNDERSTORE
        public string ModDataFilePath => Path.Combine(ModSettingsRootFolder, "settings.xml");

#endif

        #region Saved shared config
        public static string CurrentSaveVersion { get; }
        #endregion

        #region Old CommonProperties Overridable
        public abstract string SimpleName { get; }
        public abstract string SafeName { get; }
        public abstract string Acronym { get; }
        public abstract string IconName { get; }
        public abstract string GitHubRepoPath { get; }
        public abstract string[] AssetExtraDirectoryNames { get; }
        public abstract string[] AssetExtraFileNames { get; }
        public virtual string ModRootFolder => KFileUtils.BASE_FOLDER_PATH + SafeName;
        public abstract string Description { get; }

        #endregion

        #region Old CommonProperties Static
        public static IBasicIMod Instance => ModData.ModInstance;
        public static BasicModData ModData { get; protected set; }
        public static bool DebugMode => ModData.DebugMode;


        private static ulong m_modId;
        public static ulong ModId
        {
            get
            {
                if (m_modId == 0)
                {
                    m_modId = ulong.MaxValue;
                }
                return m_modId;
            }
        }

        private static string m_rootFolder;

        public static string ModSettingsRootFolder
        {
            get
            {
                if (m_rootFolder == null)
                {
                    m_rootFolder = Instance.ModRootFolder;
                }
                return m_rootFolder;
            }
        }

        public static string ModInstallFolder
        {
            get
            {
                if (m_modInstallFolder is null)
                {
#if THUNDERSTORE
                    m_modInstallFolder = typeof(IBasicIMod).Assembly.Location;
#else
                    var thisFullName = Instance.GetType().Assembly.FullName;
                    ExecutableAsset thisInfo = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == thisFullName));
                    if (thisInfo is null)
                    {
                        throw new Exception("This mod info was not found!!!!");
                    }
                    m_modInstallFolder = Path.GetDirectoryName(thisInfo.GetMeta().path);
#endif
                    LogUtils.DoInfoLog($"Mod location: {m_modInstallFolder}");
                }
                return m_modInstallFolder;
            }
        }

        private static string m_modInstallFolder;

        public static string MinorVersion => Instance.MinorVersion_;
        public static string MajorVersion => Instance.MajorVersion_;
        public static string FullVersion => Instance.FullVersion_;
        public static string Version => Instance.Version_;
        #endregion

        #region CommonProperties Fixed
        public string Name => $"{SimpleName} {Version}";
        public string GeneralName => $"{SimpleName} (v{Version})";

        protected virtual Dictionary<string, Coroutine> TasksRunningOnController { get; }
        private string MinorVersion_ => MajorVersion + "." + GetType().Assembly.GetName().Version.Build;
        private string MajorVersion_ => GetType().Assembly.GetName().Version.Major + "." + GetType().Assembly.GetName().Version.Minor;
        private string FullVersion_ => MinorVersion + " r" + GetType().Assembly.GetName().Version.Revision;
        private string Version_ =>
           GetType().Assembly.GetName().Version.Minor == 0 && GetType().Assembly.GetName().Version.Build == 0
                    ? GetType().Assembly.GetName().Version.Major.ToString()
                    : GetType().Assembly.GetName().Version.Build > 0
                        ? MinorVersion
                        : MajorVersion;

        public string CouiHost => $"{Acronym.ToLower()}.k45";

        #endregion

        #region UI
#if THUNDERSTORE
        protected abstract IEnumerable<OptionsUISystem.Section> GenerateModOptionsSections();

        internal OptionsUISystem.Page BuildModPage()
        {
            OptionsUISystem.Page page = new()
            {
                id = $"K45_{Instance.Acronym}"
            };
            List<OptionsUISystem.Section> sections = page.sections;
            sections.AddRange(GenerateModOptionsSections());
            OptionsUISystem.Section section = new()
            {
                id = $"K45.{Instance.Acronym}.About",
                items = new List<IWidget>
                {
                    Instance.AddBoolField($"K45.{Acronym}.DebugMode", new DelegateAccessor<bool>(()=>DebugMode,(x)=>ModData.DebugMode=x)),
                    Instance.AddValueField($"K45.{Instance.Acronym}.Version",()=>FullVersion)
                }
            };
            sections.Add(section);
            return page;
        }
#endif

        private void LoadLocales()
        {
            var file = Path.Combine(ModInstallFolder, $"i18n.csv");
            if (File.Exists(file))
            {
                var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
                var enColumn = Array.IndexOf(fileLines.First(), "en-US");
                var enMemoryFile = new MemorySource(LocaleFileForColumn(fileLines, enColumn));
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    GameManager.instance.localizationManager.AddSource(lang, enMemoryFile);
                    if (lang != "en-US")
                    {
                        var valueColumn = Array.IndexOf(fileLines.First(), lang);
                        if (valueColumn > 0)
                        {
                            var i18nFile = new MemorySource(LocaleFileForColumn(fileLines, valueColumn));
                            GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                        }
                    }
                    GameManager.instance.localizationManager.AddSource(lang, new ModGenI18n(ModData));
                }
            }
            else
            {
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    GameManager.instance.localizationManager.AddSource("en-US", new ModGenI18n(ModData));
                }
            }
        }

        private static Dictionary<string, string> LocaleFileForColumn(IEnumerable<string[]> fileLines, int valueColumn)
        {
            return fileLines.Skip(1).GroupBy(x => x[0]).Select(x => x.First()).ToDictionary(x => ProcessKey(x[0], ModData), x => RemoveQuotes(x.ElementAtOrDefault(valueColumn) is string s && !s.IsNullOrWhitespace() ? s : x.ElementAtOrDefault(1)));
        }


        private static string ProcessKey(string key, BasicModData modData)
        {
            if (!key.StartsWith("::")) return key;
            var prefix = key[..3];
            var suffix = key[3..];
            return prefix switch
            {
                "::L" => modData.GetOptionLabelLocaleID(suffix),
                "::G" => modData.GetOptionGroupLocaleID(suffix),
                "::D" => modData.GetOptionDescLocaleID(suffix),
                "::T" => modData.GetOptionTabLocaleID(suffix),
                "::W" => modData.GetOptionWarningLocaleID(suffix),
                "::E" => suffix.Split(".", 2) is string[] enumVal && enumVal.Length == 2 ? modData.GetEnumValueLocaleID(enumVal[0], enumVal[1]) : suffix,
                _ => suffix
            };
        }

        private static string RemoveQuotes(string v) => v != null && v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1] : v;

        #endregion

        #region UI Event binding register


        public T GetManagedSystem<T>() where T : ComponentSystemBase
        {
            return UpdateSystem.World.GetOrCreateSystemManaged<T>();
        }
        public ComponentSystemBase GetManagedSystem(Type t)
        {
            return UpdateSystem.World.GetOrCreateSystemManaged(t);
        }

        public void SetupCaller(Action<string, object[]> eventCaller)
        {
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), new[] { GetType().Assembly });
            foreach (var type in targetTypes)
            {
                (GetManagedSystem(type) as IBelzontBindable).SetupCaller(eventCaller);
            }
        }
        public void SetupEventBinder(Action<string, Delegate> eventCaller)
        {
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), new[] { GetType().Assembly });
            foreach (var type in targetTypes)
            {
                (GetManagedSystem(type) as IBelzontBindable).SetupEventBinder(eventCaller);
            }
        }
        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), new[] { GetType().Assembly });
            foreach (var type in targetTypes)
            {
                (GetManagedSystem(type) as IBelzontBindable).SetupCallBinder(eventCaller);
            }
        }

        #endregion

        private class ModGenI18n : IDictionarySource
        {
            private BasicModData modData;
            public ModGenI18n(BasicModData data)
            {
                modData = data;
            }

            public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
            {
                return new Dictionary<string, string>
                {
                    [modData.GetSettingsLocaleID()] = Instance.GeneralName,
                    [modData.GetOptionTabLocaleID(BasicModData.kAboutTab)] = "About",
                    [modData.GetOptionLabelLocaleID(nameof(BasicModData.DebugMode)).Replace(modData.GetType().Name, nameof(BasicModData))] = "Debug Mode",
                    [modData.GetOptionDescLocaleID(nameof(BasicModData.DebugMode)).Replace(modData.GetType().Name, nameof(BasicModData))] = "Turns on the log debugging for this mod",
                    [modData.GetOptionLabelLocaleID(nameof(BasicModData.Version)).Replace(modData.GetType().Name, nameof(BasicModData))] = "Mod Version",
                    [modData.GetOptionDescLocaleID(nameof(BasicModData.Version)).Replace(modData.GetType().Name, nameof(BasicModData))] = "The current mod version.\n\nThe 4th digit being higher than 1000 indicates beta version.",
                };
            }

            public void Unload()
            {
            }
        }
    }

#if !THUNDERSTORE
    public abstract class BasicIMod : IBasicIMod
    {
        protected sealed override UpdateSystem UpdateSystem { get; set; }
        public override string IconName { get; } = "ModIcon";
        public override string GitHubRepoPath { get; } = "";
        public override string[] AssetExtraDirectoryNames { get; } = new string[0];
        public override string[] AssetExtraFileNames { get; } = new string[] { };
        protected sealed override Dictionary<string, Coroutine> TasksRunningOnController { get; } = new Dictionary<string, Coroutine>();

    }
#else
    internal static class IBasicIModExtensions
    {


        public static bool GetButtonsGroup(this IBasicIMod mod, string groupName, out ButtonRow buttons, Button item)
        {
            var AutomaticSettingsSButtonGroups = typeof(AutomaticSettings).GetField("sButtonGroups", RedirectorUtils.allFlags);
            var sButtonGroups = (Dictionary<string, ButtonRow>)AutomaticSettingsSButtonGroups.GetValue(null);
            if (sButtonGroups.TryGetValue(groupName, out buttons))
            {
                Button[] array = new Button[buttons.children.Length + 1];
                buttons.children.CopyTo(array, 0);
                array[array.Length - 1] = item;
                buttons.children = array;
                return false;
            }
            buttons = new ButtonRow
            {
                children = new Button[]
                {
                    item
                }
            };
            sButtonGroups.Add(groupName, buttons);
            return true;
        }
        public static IWidget AddOptionField<T>(this IBasicIMod<T> mod, string path, string groupName, Action<T> onSet, T value, Func<bool> disabledFn = null, string warningI18n = null) where T : BasicModData
        {
            return mod.GetButtonsGroup(groupName, out ButtonRow item, new ButtonWithConfirmation
            {
                path = path,
                displayName = $"Options.OPTION[{path}]",
                action = delegate
                {
                    onSet(value);
                },
                disabled = disabledFn,
                confirmationMessage = (warningI18n != null ? new LocalizedString?(LocalizedString.Id(warningI18n)) : null)
            })
                ? item
                : (IWidget)null;
        }
        public static IWidget AddBoolField(this IBasicIMod mod, string path, DelegateAccessor<bool> accessor, Func<bool> disabledFn = null)
        {
            return new ToggleField
            {
                path = path,
                displayName = $"Options.OPTION[{path}]",
                accessor = accessor,
                disabled = disabledFn
            };
        }

        public static IWidget AddValueField(this IBasicIMod mod, string path, Func<string> getter)
        {
            return new ValueField
            {
                path = path,
                displayName = $"Options.OPTION[{path}]",
                accessor = new DelegateAccessor<string>(getter, (x) => { })
            };
        }
    }
#endif

}

