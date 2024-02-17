﻿#if BEPINEX_CS2
#else
#endif
using Belzont.AssemblyUtility;
using Belzont.Utils;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.OdinSerializer.Utilities;
using Game;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;

namespace Belzont.Interfaces
{
    public abstract class BasicIMod
    {
        protected UpdateSystem UpdateSystem { get; set; }

        internal static KlyteModDescriptionAttribute modAssemblyDescription => typeof(BasicIMod)?.Assembly?.GetCustomAttribute<KlyteModDescriptionAttribute>();

        public void OnLoad(UpdateSystem updateSystem)
        {
            OnLoad();
            UpdateSystem = updateSystem;
            Redirector.OnWorldCreated(UpdateSystem.World);
            LoadLocales();
            LogUtils.DoInfoLog($"CouiHost => {CouiHost}");
            GameManager.instance.userInterface.view.uiSystem.AddHostLocation(CouiHost, new HashSet<string> { ModInstallFolder });
            DoOnCreateWorld(updateSystem);
        }
        public abstract void OnDispose();


        public abstract BasicModData CreateSettingsFile();
        public void OnLoad()
        {
            Instance = this;
            ModData = CreateSettingsFile();
            ModData.RegisterInOptionsUI();
            AssetDatabase.global.LoadSettings(SafeName, ModData);
            KFileUtils.EnsureFolderCreation(ModSettingsRootFolder);

            Redirector.PatchAll();
            DoOnLoad();
        }

        public abstract void DoOnCreateWorld(UpdateSystem updateSystem);

        public abstract void DoOnLoad();


        #region Saved shared config
        public static string CurrentSaveVersion { get; }
        #endregion

        #region Old CommonProperties Overridable
        private static string DisplayName = (modAssemblyDescription.DisplayName?.Length ?? -1) < 1 ? throw new Exception("DisplayName not set!") : modAssemblyDescription.DisplayName;
        public string SimpleName => DisplayName;
        public string SafeName => DisplayName.Replace(" ", "");
        public virtual string Acronym => Regex.Replace(DisplayName, "[^A-Z]", "");
        public virtual string GitHubRepoPath { get; } = "";
        public virtual string ModRootFolder => Path.Combine(KFileUtils.BASE_FOLDER_PATH, SafeName);
        public string Description => modAssemblyDescription.ShortDescription ?? throw new Exception("ShortDescription not set!");

        #endregion

        #region Old CommonProperties Static
        public static BasicIMod Instance { get; private set; }
        public static BasicModData ModData { get; protected set; }
        public static bool DebugMode => ModData?.DebugMode ?? true;


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

                    LogUtils.DoInfoLog($"Mod location: {m_modInstallFolder}");
                }
                return m_modInstallFolder;
            }
        }
        private const string kVersionSuffix = "";

        private static string m_modInstallFolder;
        public static string MinorVersion => Instance.MinorVersion_ + kVersionSuffix;
        public static string MajorVersion => Instance.MajorVersion_ + kVersionSuffix;
        public static string FullVersion => Instance.FullVersion_ + kVersionSuffix;
        public static string Version => Instance.Version_ + kVersionSuffix;
        #endregion

        #region CommonProperties Fixed
        public string Name => $"{SimpleName} {Version}";
        public string GeneralName => $"{SimpleName} (v{Version})";
        protected Dictionary<string, Coroutine> TasksRunningOnController { get; } = new Dictionary<string, Coroutine>();
        private string MinorVersion_ => MajorVersion_ + "." + GetType().Assembly.GetName().Version.Build;
        private string MajorVersion_ => GetType().Assembly.GetName().Version.Major + "." + GetType().Assembly.GetName().Version.Minor;
        private string FullVersion_ => MinorVersion_ + " r" + GetType().Assembly.GetName().Version.Revision;
        private string Version_ =>
           GetType().Assembly.GetName().Version.Minor == 0 && GetType().Assembly.GetName().Version.Build == 0
                    ? GetType().Assembly.GetName().Version.Major.ToString()
                    : GetType().Assembly.GetName().Version.Build > 0
                        ? MinorVersion_
                        : MajorVersion_;

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
                string PrepareFieldName(string f) => f.Replace(modData.GetType().Name, nameof(BasicModData));

                var settingsMenuName = Instance.GeneralName.Split(" (");
                var versionLenghtRef = settingsMenuName[1].Replace(".", "").Length;
                while (settingsMenuName[0].Length > 26 - versionLenghtRef)
                {
                    var splittedName = settingsMenuName[0].Split(" ");
                    if (!splittedName.Any(x => x.Length > 2)) { break; }
                    var biggestIndex = -1;
                    var sizeBiggest = 2;
                    for (int i = 0; i < splittedName.Length; i++)
                    {
                        if (splittedName[i].Length <= sizeBiggest) continue;
                        biggestIndex = i;
                        sizeBiggest = splittedName[i].Length;
                    }
                    if (biggestIndex < 0) break;
                    splittedName[biggestIndex] = splittedName[biggestIndex][0] + ".";
                    settingsMenuName[0] = string.Join(" ", splittedName);
                }


                return new Dictionary<string, string>
                {
                    [modData.GetSettingsLocaleID()] = settingsMenuName[0] + " (" + settingsMenuName[1],
                    [modData.GetOptionTabLocaleID(BasicModData.kAboutTab)] = "About",
                    [PrepareFieldName(modData.GetOptionLabelLocaleID(nameof(BasicModData.DebugMode)))] = "Debug Mode",
                    [PrepareFieldName(modData.GetOptionDescLocaleID(nameof(BasicModData.DebugMode)))] = "Turns on the log debugging for this mod",
                    [PrepareFieldName(modData.GetOptionLabelLocaleID(nameof(BasicModData.Version)))] = "Mod Version",
                    [PrepareFieldName(modData.GetOptionDescLocaleID(nameof(BasicModData.Version)))] = "The current mod version.\n\nIf version ends with 'B', it's a version compiled for BepInEx framework.",
                };
            }

            public void Unload()
            {
            }
        }
    }

}

