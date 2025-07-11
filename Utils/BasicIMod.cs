﻿using Belzont.AssemblyUtility;
using Belzont.Utils;
using BelzontCommons.Assets;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.OdinSerializer.Utilities;
using Game;
using Game.Prefabs;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Unity.Entities;
using UnityEngine;
using static Belzont.Interfaces.BasicModData;

namespace Belzont.Interfaces
{
    public abstract class BasicIMod
    {
        protected UpdateSystem UpdateSystem { get; set; }
        internal static KlyteModDescriptionAttribute modAssemblyDescription => (BasicIMod.Instance?.GetType() ?? typeof(BasicIMod)).Assembly.GetCustomAttribute<KlyteModDescriptionAttribute>() ?? new();

        public void OnLoad(UpdateSystem updateSystem)
        {
            OnLoad();
            UpdateSystem = updateSystem;
            Redirector.OnWorldCreated(UpdateSystem.World);
            DoOnCreateWorld(updateSystem);
            GameManager.instance.RegisterUpdater(() =>
            {
                LogUtils.DoInfoLog($"CouiHost => {CouiHost}");
                GameManager.instance.userInterface.view.uiSystem.AddHostLocation(CouiHost, new HashSet<(string, int)> { (ModInstallFolder, 0) });
                GameManager.instance.RegisterUpdater(RegisterAtEuis);
                GameManager.instance.userInterface.view.uiSystem.defaultUIView.Listener.ReadyForBindings += SelfRegiterUIEvents;
                SelfRegiterUIEvents();
            });
            GameManager.instance.RegisterUpdater(LoadLocales);
            GameManager.instance.RegisterUpdater(RegisterAssets);
        }

        private void RegisterAssets()
        {
            var databaseStructs = GetType().Assembly.DefinedTypes.Where(x => x.InheritsFrom(typeof(AssetDatabaseSelfCreate)));
            foreach (var databaseType in databaseStructs)
            {
                var constructor = databaseType.GetConstructor(new Type[0]);
                if (constructor == null) continue;
                var dbKey = constructor.Invoke(null) as AssetDatabaseSelfCreate;
                var dbInstance = dbKey.CreateDatabase();
                AssetDatabase.global.RegisterDatabase(dbInstance).Wait();

                dbKey.SelfPopulate();

                var prefabSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();


                foreach (PrefabAsset prefabAsset in dbInstance.GetAssets(default(SearchFilter<PrefabAsset>)))
                {
                    PrefabBase prefabBase = prefabAsset.Load() as PrefabBase;
                    if (prefabBase != null)
                    {
                        prefabSystem.AddPrefab(prefabBase, null, null, null);
                    }
                }
            }
            AfterRegisterAssets();
        }

        protected virtual void AfterRegisterAssets()
        {

        }

        public abstract void OnDispose();


        public abstract BasicModData CreateSettingsFile();
        public void OnLoad()
        {
            Instance = this;
            ModData = CreateSettingsFile();
            ModData.RegisterInOptionsUI();
            ModData.RegisterKeyBindings();
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
        private static string DisplayName => (modAssemblyDescription.DisplayName?.Length ?? -1) < 1 ? throw new Exception("DisplayName not set!") : modAssemblyDescription.DisplayName;
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
        public static bool DebugMode => ModData?.LoggingLevel >= LogLevel.Debug;
        public static bool TraceMode => ModData?.LoggingLevel >= LogLevel.Trace;
        public static bool VerboseMode => ModData?.LoggingLevel >= LogLevel.Verbose;


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
                    ExecutableAsset thisInfo = AssetDatabase.global.GetAsset(SearchFilter<ExecutableAsset>.ByCondition(x => x.definition?.FullName == thisFullName))
                        ?? throw new Exception("This mod info was not found!!!!");
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


        #endregion

        #region UI 

        private Queue<(string, IDictionarySource)> previouslyLoadedDictionaries;
        internal string AdditionalI18nFilesFolder => Path.Combine(ModInstallFolder, $"i18n/");

        internal void LoadLocales()
        {
            var file = Path.Combine(ModInstallFolder, $"i18n/i18n.csv");
            previouslyLoadedDictionaries ??= new();
            UnloadLocales();

            var baseModData = new ModGenI18n(ModData);
            if (File.Exists(file))
            {
                var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
                var enColumn = Array.IndexOf(fileLines.First(), "en-US");
                var enMemoryFile = new MemorySource(LocaleFileForColumn(fileLines, enColumn));
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    previouslyLoadedDictionaries.Enqueue((lang, enMemoryFile));
                    GameManager.instance.localizationManager.AddSource(lang, enMemoryFile);
                    if (lang != "en-US")
                    {
                        var valueColumn = Array.IndexOf(fileLines.First(), lang);
                        if (TraceMode) LogUtils.DoTraceLog($"Trying load language: {lang} at folder {AdditionalI18nFilesFolder} or valueColumn with {valueColumn} items");
                        if (valueColumn > 0)
                        {
                            var i18nFile = new MemorySource(LocaleFileForColumn(fileLines, valueColumn));
                            previouslyLoadedDictionaries.Enqueue((lang, i18nFile));
                            GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                        }
                        else if (File.Exists(Path.Combine(AdditionalI18nFilesFolder, lang + ".csv")))
                        {
                            var csvFileEntries = File.ReadAllLines(Path.Combine(AdditionalI18nFilesFolder, lang + ".csv")).Select(x => x.Split("\t"));
                            var i18nFile = new MemorySource(LocaleFileForColumn(csvFileEntries, 1));
                            previouslyLoadedDictionaries.Enqueue((lang, i18nFile));
                            GameManager.instance.localizationManager.AddSource(lang, i18nFile);
                        }
                    }
                    previouslyLoadedDictionaries.Enqueue((lang, baseModData));
                    GameManager.instance.localizationManager.AddSource(lang, baseModData);
                }
            }
            else
            {
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    previouslyLoadedDictionaries.Enqueue((lang, baseModData));
                    GameManager.instance.localizationManager.AddSource(lang, baseModData);
                }
            }
        }

        private void UnloadLocales()
        {
            while (previouslyLoadedDictionaries.TryDequeue(out var src))
            {
                GameManager.instance.localizationManager.RemoveSource(src.Item1, src.Item2);
            }
        }

        private static Dictionary<string, string> LocaleFileForColumn(IEnumerable<string[]> fileLines, int valueColumn)
        {
            return fileLines.Skip(1).GroupBy(x => x[0]).Select(x => x.First()).ToDictionary(x => ProcessKey(x[0], ModData), x => ReplaceSpecialChars(RemoveQuotes(x.ElementAtOrDefault(valueColumn) is string s && !s.IsNullOrWhitespace() ? s : x.ElementAtOrDefault(1))));
        }

        private static string ReplaceSpecialChars(string v)
        {
            return v.Replace("\\n", "\n").Replace("\\t", "\t");
        }

        private static string ProcessKey(string key, BasicModData modData)
        {
            if (!key.StartsWith("::")) return key;
            if (key.StartsWith("::N"))
            {
                if (key.StartsWith("::NT"))
                {
                    return $"Menu.NOTIFICATION_TITLE[{key[4..]}]";
                }
                else if (key.StartsWith("::ND"))
                {
                    return $"Menu.NOTIFICATION_DESCRIPTION[{key[4..]}]";
                }
            }
            if (key == "::M") return modData.GetBindingMapLocaleID();
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
                "::B" => modData.GetBindingKeyLocaleID(suffix),
                "::H" => modData.GetBindingKeyHintLocaleID(suffix),
                _ => suffix
            };
        }

        //Options.OPTION[BelzontWE.BelzontWE.WriteEverywhereCS2Mod.WEModData.ToolReduceMovementStrenght]

        private static string RemoveQuotes(string v) => v != null && v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1].Replace("\"\"", "\"") : v;

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

        public void SelfRegiterUIEvents()
        {
            SetupCallBinder((callAddress, action) =>
            {
                var callName = $"{EuisModderIdentifier}::{EuisModAcronym}.{callAddress}";
                if (BasicIMod.DebugMode) LogUtils.DoLog($"Register call: {callName}");
                GameManager.instance.userInterface.view.View.BindCall(callName, action);
            });
            SetupCaller((callAddress, args) =>
            {
                var targetView = GameManager.instance.userInterface.view.View;
                if (!targetView.IsReadyForBindings()) return;
                var eventNameFull = $"{EuisModderIdentifier}::{EuisModAcronym}.{callAddress}";
                var argsLenght = args is null ? 0 : args.Length;
                switch (argsLenght)
                {
                    case 0: targetView.TriggerEvent(eventNameFull); break;
                    case 1: targetView.TriggerEvent(eventNameFull, args[0]); break;
                    case 2: targetView.TriggerEvent(eventNameFull, args[0], args[1]); break;
                    case 3: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2]); break;
                    case 4: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3]); break;
                    case 5: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4]); break;
                    case 6: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5]); break;
                    case 7: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5], args[6]); break;
                    case 8: targetView.TriggerEvent(eventNameFull, args[0], args[1], args[2], args[3], args[4], args[5], args[6], args[7]); break;
                    default:
                        LogUtils.DoWarnLog($"Too much arguments for trigger event! {argsLenght}: {args}");
                        break;
                }
                if (TraceMode) LogUtils.DoTraceLog($"Triggered event: {eventNameFull}");
            });
            SetupEventBinder((callAddress, action) =>
            {
                var eventName = $"{EuisModderIdentifier}::{EuisModAcronym}.{callAddress}";
                LogUtils.DoInfoLog($"Register event: {eventName}");
                GameManager.instance.userInterface.view.View.RegisterForEvent(eventName, action);
            });

        }

        #endregion

        #region EUIS utility

        protected virtual bool UseEuisRegister => false;
        protected virtual bool EuisIsMandatory => false;

        public virtual string EuisModderIdentifier => "k45";
        public virtual string EuisModAcronym => Acronym.ToLower();

        protected virtual Dictionary<string, EuisAppRegister> EuisApps { get; }

        private void RegisterAtEuis()
        {
            if (!UseEuisRegister) return;
            var euisAsset = BridgeUtils.FindExecutableAsset("ExtraUIScreens");
            if (euisAsset is null)
            {
                if (EuisIsMandatory)
                {
                    throw new Exception($"The mod {Name} requires Extra UI Screens mod to work!");
                }
                else return;
            }
            var bridgeClass = euisAsset.assembly.GetExportedTypes().FirstOrDefault(x => x.Name == "EuisExternalRegisterBridge")
                ?? throw new Exception($"Incorrect version of EUIS loaded for mod {Name}!\nEnsure its version is 0.2.0 or higher.");
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), new[] { GetType().Assembly });


            bridgeClass.GetMethod("RegisterModForEUIS").Invoke(null, new object[] { EuisModderIdentifier, EuisModAcronym, Delegate.Combine(SetupCaller), Delegate.Combine(SetupEventBinder), Delegate.Combine(SetupCallBinder) });
            var registerAppMethod = bridgeClass.GetMethod("RegisterAppForEUIS");
            foreach (var app in EuisApps)
            {
                registerAppMethod.Invoke(null, new object[] { EuisModderIdentifier, EuisModAcronym, app.Key, app.Value.DisplayName, app.Value.UrlJs, app.Value.UrlCss, app.Value.UrlIcon }); ;
            }
        }

        protected string CouiHost => $"{EuisModAcronym}.{EuisModderIdentifier}";

        protected record EuisAppRegister(string DisplayName, string UrlJs, string UrlCss, string UrlIcon) { }

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


                var settingsMenuName = Instance.GeneralName.Split(" (");
                var baseName = Regex.Replace(settingsMenuName[0], "\\[[^\\]]+\\]", "").Trim();
                var versionLenghtRef = settingsMenuName[1].Replace(".", "").Length;
                while (baseName.Length > 26 - versionLenghtRef)
                {
                    var splittedName = baseName.Split(" ");
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
                    baseName = string.Join(" ", splittedName);
                }


                return new Dictionary<string, string>
                {
                    [modData.GetSettingsLocaleID()] = baseName + " (" + settingsMenuName[1],
                    [modData.GetOptionTabLocaleID(BasicModData.kAboutTab)] = "About",
                    [modData.GetOptionGroupLocaleID(BasicModData.kLogSection)] = "Logging",
                    [modData.GetOptionGroupLocaleID(BasicModData.kChangelogSection)] = "Changelog",
                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.Changelog)))] = $"Changelog\n{Utils.StringUtils.SiteMdToGameMd(KResourceLoader.LoadResourceStringMod("changelog.md"))}",

                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.LoggingLevel)))] = "Logging Level",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.LoggingLevel)))] = "Changes the log level of this mod. Verbose mode generates A LOT of logging, be careful.",

                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.LogStacktraces)))] = "Log Stacktraces",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.LogStacktraces)))] = "Add stacktrace information telling when the log was generated in the code",

                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.ShowErrorsPopups)))] = "Show this mod errors on UI",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.ShowErrorsPopups)))] = "Only disable it on emergencies!",
                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.Version)))] = "Mod Version",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.Version)))] = "The current mod version.",

                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.GoToLocalesFolder)))] = "Go To Translations folder",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.GoToLocalesFolder)))] = "Open the folder where the additional translations files shall be placed:\r\n- They should be named as <[langID].csv> and it contents must be in the format <[KEY][_TAB_character_][VALUE]>.\r\n- New lines in value content must be replaced by <\\\\n> char sequence.\r\n- Don't forget to keep the values inside curly braces <{}> in the new translation.\r\n- Share it later at forums to get it added with the mod package at Paradox Mods.\r\n- Check Localization.log at game logs folder to get the available langIDs.",
                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.ReloadLocales)))] = "Reload translations",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.ReloadLocales)))] = "When doing translations, click here to load your modfied file for testing purposes.",
                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.GoToForum)))] = "Go to forums",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.GoToForum)))] = "Access the mod forum discussion at Paradox Mods.",
                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.GoToGitHub)))] = "Go to repository",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.GoToGitHub)))] = "Access the mod repository to get access to the mod sources.",
                    [modData.FixLocaleId(modData.GetOptionLabelLocaleID(nameof(BasicModData.GoToLogFolder)))] = "Go to log folder",
                    [modData.FixLocaleId(modData.GetOptionDescLocaleID(nameof(BasicModData.GoToLogFolder)))] = $"Access game mod folders. The log name of this mod is <{Path.GetFileName(LogUtils.GetLogLocation())}>",

                    [modData.GetEnumValueLocaleID(LogLevel.Normal)] = "Normal",
                    [modData.GetEnumValueLocaleID(LogLevel.Debug)] = "Debug",
                    [modData.GetEnumValueLocaleID(LogLevel.Trace)] = "Trace (Beware...) ",
                    [modData.GetEnumValueLocaleID(LogLevel.Verbose)] = "Verbose (Don't let it for much time!)",
                };
            }

            public void Unload()
            {
            }
        }
    }

}

