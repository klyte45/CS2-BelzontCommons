using Belzont.Utils;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.OdinSerializer.Utilities;
using Colossal.UI;
using Game;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Entities;
using UnityEngine;

namespace Belzont.Interfaces
{
    public abstract class BasicIMod
    {
        protected UpdateSystem m_updateSystem;
        public void OnCreateWorld(UpdateSystem updateSystem)
        {
            m_updateSystem = updateSystem;
            Redirector.OnWorldCreated(m_updateSystem.World);
            LoadLocales();

            var uiSys = GameManager.instance.userInterface.view.uiSystem;
            LogUtils.DoLog($"CouiHost => {CouiHost}");
            ((DefaultResourceHandler)uiSys.resourceHandler).HostLocationsMap.Add(CouiHost, new List<string> { ModInstallFolder });

            DoOnCreateWorld(updateSystem);

        }
        public abstract void OnDispose();
        public abstract BasicModData CreateSettingsFile();
        public void OnLoad()
        {
            ModData = CreateSettingsFile();
            ModData.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(SafeName, ModData);

            KFileUtils.EnsureFolderCreation(ModSettingsRootFolder);
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

        #region Saved shared config
        public static string CurrentSaveVersion { get; }
        #endregion

        #region Old CommonProperties Overridable
        public abstract string SimpleName { get; }
        public virtual string IconName { get; } = "ModIcon";
        public abstract string SafeName { get; }
        public virtual string GitHubRepoPath { get; } = "";
        public abstract string Acronym { get; }
        public virtual string[] AssetExtraDirectoryNames { get; } = new string[0];
        public virtual string[] AssetExtraFileNames { get; } = new string[] { };
        public virtual string ModRootFolder => KFileUtils.BASE_FOLDER_PATH + SafeName;
        public abstract string Description { get; }

        #endregion

        #region Old CommonProperties Static
        public static BasicIMod Instance => ModData.ModInstance;
        public static BasicModData ModData { get; private set; }
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
                    var thisFullName = Instance.GetType().Assembly.FullName;
                    ExecutableAsset thisInfo = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == thisFullName));
                    if (thisInfo is null)
                    {
                        throw new Exception("This mod info was not found!!!!");
                    }
                    m_modInstallFolder = Path.GetDirectoryName(thisInfo.GetMeta().path);
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

        private readonly Dictionary<string, Coroutine> TasksRunningOnController = new Dictionary<string, Coroutine>();
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
        private void LoadLocales()
        {
            var file = Path.Combine(ModInstallFolder, $"i18n/i18n.csv");
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
                GameManager.instance.localizationManager.AddSource("en-US", new ModGenI18n(ModData));
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
            return m_updateSystem.World.GetOrCreateSystemManaged<T>();
        }
        public ComponentSystemBase GetManagedSystem(Type t)
        {
            return m_updateSystem.World.GetOrCreateSystemManaged(t);
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

}

