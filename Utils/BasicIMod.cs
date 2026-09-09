using Belzont.AssemblyUtility;
using Belzont.Systems;
using Belzont.Utils;
using BelzontCommons.Assets;
using Colossal;
using Colossal.Core;
using Colossal.IO.AssetDatabase;
using Colossal.Localization;
using Colossal.OdinSerializer.Utilities;
using Game;
using Game.Modding;
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
        internal static KlyteModDescriptionAttribute ModAssemblyDescription => modAssemblyDescription ??= (BasicIMod.Instance?.GetType() ?? typeof(BasicIMod)).Assembly.GetCustomAttribute<KlyteModDescriptionAttribute>() ?? new();
        internal static KlyteModDescriptionAttribute modAssemblyDescription;

        internal static bool IsReadyToLoad = ModAssemblyDescription.ModId != "0";

        private bool ForceLoadingAttributes()
        {
            if (!IsReadyToLoad)
            {
                if (GameManager.instance.modManager.TryGetExecutableAsset(Instance as IMod, out var asset))
                {
                    Assembly assembly = Assembly.LoadFrom(asset.path);
                    modAssemblyDescription = assembly.GetCustomAttributes<KlyteModDescriptionAttribute>().FirstOrDefault();
                    if (modAssemblyDescription is null)
                    {
                        LogUtils.DoErrorLog($"The mod '{assembly.FullName}' is corrupted; it's missing the mod description DLL attribute. Recompile it!");
                        modAssemblyDescription = new();
                    }
                }
                else
                {
                    modAssemblyDescription = null;
                    LogUtils.DoInfoLog($"Initialization not complete yet. Waiting for {GetType().Assembly} {GetType().Assembly.GetCustomAttribute<KlyteModDescriptionAttribute>()}");
                    return false;
                }
            }
            DelayedLoad();
            return true;
        }

        public void OnLoad(UpdateSystem updateSystem)
        {
            Instance = this;
            UpdateSystem = updateSystem;
            LogUtils.DoInfoLog($"Starting {GetType().Assembly}");
            if (!ForceLoadingAttributes())
            {
                MainThreadDispatcher.RegisterUpdater(ForceLoadingAttributes);
                return;
            }
        }

        private void DelayedLoad()
        {
            OnLoad();
            Redirector.OnWorldCreated(UpdateSystem.World);
            UpdateSystem.UpdateAt<WhatsNewUISystem>(SystemUpdatePhase.UIUpdate);
            DoOnCreateWorld(UpdateSystem);
            RegisterBelzontSystems();
            MainThreadDispatcher.RegisterUpdater(() =>
            {
                try
                {
                    LogUtils.DoInfoLog($"CouiHost => {CouiHost}");
                    GameManager.instance.userInterface.view.uiSystem.AddHostLocation(CouiHost, new HashSet<(string, int)> { (ModInstallFolder, 0) });
                    MainThreadDispatcher.RegisterUpdater(RegisterAtEuis);
                    GameManager.instance.userInterface.view.uiSystem.defaultUIView.Listener.ReadyForBindings += SelfRegiterUIEvents;
                    SelfRegiterUIEvents();
                }
                catch (Exception ex)
                {
                    LogUtils.DoErrorLog("Error during delayed load of UI events registration!", ex);
                }
                return true;
            });
            MainThreadDispatcher.RegisterUpdater(LoadLocales);
            MainThreadDispatcher.RegisterUpdater(RegisterAssets);
        }

        private void RegisterBelzontSystems()
        {
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBasicSystem), new[] { GetType().Assembly });
            foreach (var type in targetTypes)
            {
                if (type.IsCastableTo(typeof(SystemBase)))
                {
                    GetManagedSystem(type);
                }
            }
        }

        private bool RegisterAssets()
        {
            if (ModAssemblyDescription.ModId == "0") return false;
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
            return true;
        }

        protected virtual void AfterRegisterAssets()
        {

        }

        public abstract void OnDispose();


        public abstract BasicModData CreateSettingsFile();
        public void OnLoad()
        {
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
        private static string DisplayName => (ModAssemblyDescription.DisplayName?.Length ?? -1) < 1 ? throw new Exception("DisplayName not set!") : ModAssemblyDescription.DisplayName;
        public string SimpleName => DisplayName;
        public string SafeName => DisplayName.Replace(" ", "");
        public virtual string Acronym => Regex.Replace(DisplayName, "[^A-Z]", "");
        public virtual string GitHubRepoPath { get; } = "";
        public virtual string ModRootFolder => Path.Combine(KFileUtils.BASE_FOLDER_PATH, SafeName);
        public string Description => ModAssemblyDescription.ShortDescription ?? throw new Exception("ShortDescription not set!");

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

        internal bool LoadLocales()
        {
            if (ModAssemblyDescription.ModId == "0") return false;
            var file = Path.Combine(ModInstallFolder, $"i18n/i18n.csv");
            previouslyLoadedDictionaries ??= new();
            UnloadLocales();

            // en-US md is the base for every language; missing keys/files fall through to it (same pattern as CSV en column).
            var enMdEntries = LoadMarkdownLocaleEntries(MarkdownBaseLocale);
            MemorySource enMdSource = enMdEntries.Count > 0 ? new MemorySource(enMdEntries) : null;
            var commonsCsvLines = LoadBelzontCommonsI18nCsvLines();
            var commonsEnEntries = LoadBelzontCommonsLocaleEntriesFromColumn(commonsCsvLines, "en-US", ModData);
            MemorySource commonsEnSource = commonsEnEntries.Count > 0 ? new MemorySource(commonsEnEntries) : null;

            if (File.Exists(file))
            {
                var fileLines = File.ReadAllLines(file).Select(x => x.Split('\t'));
                var enColumn = Array.IndexOf(fileLines.First(), "en-US");
                var enMemoryFile = new MemorySource(LocaleFileForColumn(fileLines, enColumn));
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    AddBelzontCommonsLocaleSources(lang, commonsCsvLines, commonsEnSource, commonsEnEntries, out var commonsEntriesForLang);

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
                    var modGen = new ModGenI18n(ModData, commonsEntriesForLang);
                    previouslyLoadedDictionaries.Enqueue((lang, modGen));
                    GameManager.instance.localizationManager.AddSource(lang, modGen);
                    // Md after CSV/ModGen so same-key entries in i18n/{lang}/*.md win over CSV.
                    AddMarkdownLocaleSources(lang, enMdSource);
                }
            }
            else
            {
                foreach (var lang in GameManager.instance.localizationManager.GetSupportedLocales())
                {
                    AddBelzontCommonsLocaleSources(lang, commonsCsvLines, commonsEnSource, commonsEnEntries, out var commonsEntriesForLang);
                    var modGen = new ModGenI18n(ModData, commonsEntriesForLang);
                    previouslyLoadedDictionaries.Enqueue((lang, modGen));
                    GameManager.instance.localizationManager.AddSource(lang, modGen);
                    AddMarkdownLocaleSources(lang, enMdSource);
                }
            }

            return true;
        }

        /// <summary>
        /// Same overlay rules as mod disk i18n: register en-US commons as base for every language;
        /// for other langs, prefer a column in <c>i18n.csv</c>, else a standalone embedded <c>{lang}.csv</c>.
        /// </summary>
        private void AddBelzontCommonsLocaleSources(
            string lang,
            string[][] commonsCsvLines,
            MemorySource commonsEnSource,
            Dictionary<string, string> commonsEnEntries,
            out Dictionary<string, string> commonsEntriesForLang)
        {
            commonsEntriesForLang = commonsEnEntries;

            if (commonsEnSource != null)
            {
                previouslyLoadedDictionaries.Enqueue((lang, commonsEnSource));
                GameManager.instance.localizationManager.AddSource(lang, commonsEnSource);
            }

            if (lang == "en-US")
            {
                return;
            }

            if (TryLoadBelzontCommonsLangOverlay(commonsCsvLines, lang, ModData, out var overlay) && overlay.Count > 0)
            {
                commonsEntriesForLang = overlay;
                var commonsLangSource = new MemorySource(overlay);
                previouslyLoadedDictionaries.Enqueue((lang, commonsLangSource));
                GameManager.instance.localizationManager.AddSource(lang, commonsLangSource);
            }
        }

        private void UnloadLocales()
        {
            while (previouslyLoadedDictionaries.TryDequeue(out var src))
            {
                GameManager.instance.localizationManager.RemoveSource(src.Item1, src.Item2);
                ForgetUserSource(src.Item1, src.Item2);
            }
        }

        private static FieldInfo userSourcesField;
        private static bool userSourcesFieldResolved;

        /// <summary>
        /// <see cref="LocalizationManager.RemoveSource"/> drops the source from the locale but keeps the
        /// pair in its private user-source list, which is replayed by every <c>LoadAvailableLocales</c>
        /// (bulk asset changes). Without this, each reload leaves another generation behind and keys
        /// deleted from the files come back from the older ones.
        /// </summary>
        private static void ForgetUserSource(string localeId, IDictionarySource source)
        {
            if (!userSourcesFieldResolved)
            {
                userSourcesFieldResolved = true;
                userSourcesField = typeof(LocalizationManager).GetField("m_UserSources", BindingFlags.Instance | BindingFlags.NonPublic);
                if (userSourcesField is null)
                {
                    LogUtils.DoWarnLog("LocalizationManager.m_UserSources not found; stale locale sources will pile up on each translation reload.");
                }
            }
            if (userSourcesField?.GetValue(GameManager.instance.localizationManager) is List<(string, IDictionarySource)> userSources)
            {
                userSources.Remove((localeId, source));
            }
        }

        private const string MarkdownBaseLocale = "en-US";

        /// <summary>
        /// Registers <c>i18n/en-US/*.md</c> as the base for <paramref name="lang"/>, then overlays
        /// <c>i18n/{lang}/*.md</c> when present. Keys missing from the language folder keep en-US content.
        /// </summary>
        private void AddMarkdownLocaleSources(string lang, MemorySource enMdSource)
        {
            if (enMdSource != null)
            {
                previouslyLoadedDictionaries.Enqueue((lang, enMdSource));
                GameManager.instance.localizationManager.AddSource(lang, enMdSource);
            }

            if (lang == MarkdownBaseLocale) return;

            var langEntries = LoadMarkdownLocaleEntries(lang);
            if (langEntries.Count == 0) return;

            var langMdSource = new MemorySource(langEntries);
            previouslyLoadedDictionaries.Enqueue((lang, langMdSource));
            GameManager.instance.localizationManager.AddSource(lang, langMdSource);
        }

        /// <summary>
        /// Loads <c>i18n/{lang}/*.md</c> (YAML-like frontmatter <c>key</c>/<c>entry</c> + body).
        /// Deterministic path order; later files overwrite earlier keys within the same folder.
        /// </summary>
        private Dictionary<string, string> LoadMarkdownLocaleEntries(string lang)
        {
            var entries = new Dictionary<string, string>();
            var langDir = Path.Combine(AdditionalI18nFilesFolder, lang);
            if (!Directory.Exists(langDir)) return entries;

            foreach (var mdPath in Directory.EnumerateFiles(langDir, "*.md", SearchOption.TopDirectoryOnly).OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            {
                if (!TryParseMarkdownLocaleFile(mdPath, out var key, out var value))
                {
                    if (TraceMode) LogUtils.DoTraceLog($"Skipping markdown locale (missing key frontmatter): {mdPath}");
                    continue;
                }
                var processed = ProcessKey(key, ModData);
                // Last loaded file wins for duplicate keys within this folder.
                entries[processed] = value;
            }

            return entries;
        }

        private static bool TryParseMarkdownLocaleFile(string path, out string key, out string value)
        {
            key = null;
            value = null;
            var text = File.ReadAllText(path);
            if (text.Length == 0) return false;

            // Strip UTF-8 BOM if present.
            if (text[0] == '\uFEFF') text = text[1..];

            if (!text.StartsWith("---", StringComparison.Ordinal)) return false;

            var afterOpen = text.IndexOf('\n');
            if (afterOpen < 0) return false;
            var closeIdx = text.IndexOf("\n---", afterOpen, StringComparison.Ordinal);
            if (closeIdx < 0) return false;

            var frontmatter = text.Substring(afterOpen + 1, closeIdx - afterOpen - 1);
            var bodyStart = closeIdx + 4; // past "\n---"
            if (bodyStart < text.Length && text[bodyStart] == '\r') bodyStart++;
            if (bodyStart < text.Length && text[bodyStart] == '\n') bodyStart++;

            foreach (var rawLine in frontmatter.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r').Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                var colon = line.IndexOf(':');
                if (colon <= 0) continue;
                var field = line[..colon].Trim();
                var fieldValue = line[(colon + 1)..].Trim().Trim('"').Trim('\'');
                if (field.Equals("key", StringComparison.OrdinalIgnoreCase)
                    || field.Equals("entry", StringComparison.OrdinalIgnoreCase))
                {
                    key = fieldValue;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(key)) return false;

            value = bodyStart < text.Length ? text[bodyStart..].TrimEnd('\r', '\n') : "";
            return true;
        }

        private static Dictionary<string, string> LocaleFileForColumn(IEnumerable<string[]> fileLines, int valueColumn)
        {
            return fileLines.Skip(1)
                .Where(x => x.Length > 0 && !x[0].IsNullOrWhitespace())
                .GroupBy(x => x[0])
                .Select(x => x.First())
                .Select(x => (Key: ProcessKey(x[0], ModData), Value: ReplaceSpecialChars(RemoveQuotes(x.ElementAtOrDefault(valueColumn) is string s && !s.IsNullOrWhitespace() ? s : x.ElementAtOrDefault(1)))))
                .Where(x => !x.Key.IsNullOrWhitespace())
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private static Dictionary<string, string> CommonsLocaleFileForColumn(IEnumerable<string[]> fileLines, int valueColumn, BasicModData modData)
        {
            var acronym = Instance?.Acronym ?? string.Empty;
            var logFileName = Path.GetFileName(LogUtils.GetLogLocation());
            return fileLines.Skip(1)
                .Where(x => x.Length > 0 && !x[0].IsNullOrWhitespace())
                .GroupBy(x => x[0])
                .Select(x => x.First())
                .Select(x =>
                {
                    var key = ProcessKey(x[0], modData).Replace("{Acronym}", acronym);
                    key = modData.FixLocaleId(key);
                    var raw = x.ElementAtOrDefault(valueColumn) is string s && !s.IsNullOrWhitespace()
                        ? s
                        : x.ElementAtOrDefault(1);
                    var value = ReplaceSpecialChars(RemoveQuotes(raw)).Replace("{logFileName}", logFileName);
                    return (Key: key, Value: value);
                })
                .Where(x => !x.Key.IsNullOrWhitespace())
                .ToDictionary(x => x.Key, x => x.Value);
        }

        private static string[][] LoadBelzontCommonsI18nCsvLines()
        {
            if (!KResourceLoader.ResourceExistsBelzont("i18n.i18n.csv"))
            {
                return null;
            }

            return KResourceLoader.LoadResourceStringLinesBelzont("i18n.i18n.csv")
                .Select(x => x.Split('\t'))
                .ToArray();
        }

        private static string[][] LoadBelzontCommonsStandaloneCsvLines(string lang)
        {
            var resourceName = $"i18n.{lang}.csv";
            if (!KResourceLoader.ResourceExistsBelzont(resourceName))
            {
                return null;
            }

            return KResourceLoader.LoadResourceStringLinesBelzont(resourceName)
                .Select(x => x.Split('\t'))
                .ToArray();
        }

        private static Dictionary<string, string> LoadBelzontCommonsLocaleEntriesFromColumn(string[][] fileLines, string lang, BasicModData modData)
        {
            if (fileLines is null || fileLines.Length == 0)
            {
                return new Dictionary<string, string>();
            }

            var enColumn = Array.IndexOf(fileLines[0], "en-US");
            if (enColumn < 0) enColumn = 1;
            var valueColumn = lang == "en-US" ? enColumn : Array.IndexOf(fileLines[0], lang);
            if (valueColumn < 0) valueColumn = enColumn;
            return CommonsLocaleFileForColumn(fileLines, valueColumn, modData);
        }

        /// <summary>
        /// Prefer a language column in the main commons <c>i18n.csv</c>; otherwise a standalone embedded <c>{lang}.csv</c>
        /// (same order as mod disk locales).
        /// </summary>
        private static bool TryLoadBelzontCommonsLangOverlay(
            string[][] mainCsvLines,
            string lang,
            BasicModData modData,
            out Dictionary<string, string> overlay)
        {
            overlay = null;
            if (mainCsvLines != null && mainCsvLines.Length > 0)
            {
                var valueColumn = Array.IndexOf(mainCsvLines[0], lang);
                if (TraceMode) LogUtils.DoTraceLog($"Commons i18n: trying language {lang}, main csv column={valueColumn}");
                if (valueColumn > 0)
                {
                    overlay = CommonsLocaleFileForColumn(mainCsvLines, valueColumn, modData);
                    return overlay.Count > 0;
                }
            }

            var standalone = LoadBelzontCommonsStandaloneCsvLines(lang);
            if (standalone != null && standalone.Length > 0)
            {
                if (TraceMode) LogUtils.DoTraceLog($"Commons i18n: loading standalone embedded i18n.{lang}.csv");
                // Standalone files are key + value (column 1), matching mod i18n/{lang}.csv.
                overlay = CommonsLocaleFileForColumn(standalone, 1, modData);
                return overlay.Count > 0;
            }

            return false;
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

        private static string RemoveQuotes(string v) => v != null && v.StartsWith("\"") && v.EndsWith("\"") ? v[1..^1].Replace("\"\"", "\"") : v??"<NULL?>";

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
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), [GetType().Assembly]);
            foreach (var type in targetTypes)
            {
                LogUtils.DoInfoLog($"Setting up event caller for {type.FullName}");
                (GetManagedSystem(type) as IBelzontBindable).SetupCaller(eventCaller);
            }
        }
        public void SetupEventBinder(Action<string, Delegate> eventCaller)
        {
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), [GetType().Assembly]);
            foreach (var type in targetTypes)
            {
                LogUtils.DoInfoLog($"Setting up event binder for {type.FullName}");
                (GetManagedSystem(type) as IBelzontBindable).SetupEventBinder(eventCaller);
            }
        }
        public void SetupCallBinder(Action<string, Delegate> eventCaller)
        {
            var targetTypes = ReflectionUtils.GetInterfaceImplementations(typeof(IBelzontBindable), [GetType().Assembly]);
            foreach (var type in targetTypes)
            {
                LogUtils.DoInfoLog($"Setting up call binder for {type.FullName}");
                (GetManagedSystem(type) as IBelzontBindable).SetupCallBinder(eventCaller);
            }
        }

        public void SelfRegiterUIEvents()
        {
            SetupCallBinder((callAddress, action) =>
            {
                var callName = $"{EuisModderIdentifier}::{EuisModAcronym}.{callAddress}";
                if (BasicIMod.DebugMode) LogUtils.DoDebugLog($"Register call: {callName}");
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
                    LogUtils.DoErrorLog($"The mod {Name} requires Extra UI Screens mod to work!");
                    return;
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
            private readonly BasicModData modData;
            private readonly Dictionary<string, string> commonsEntries;

            public ModGenI18n(BasicModData data, Dictionary<string, string> commonsEntries = null)
            {
                modData = data;
                this.commonsEntries = commonsEntries ?? new Dictionary<string, string>();
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

                var result = new Dictionary<string, string>
                {
                    [modData.GetSettingsLocaleID()] = baseName + " (" + settingsMenuName[1],
                };

                // Enum dropdown locale IDs from ModSetting may differ from ::E ProcessKey; mirror commons values.
                MapEnumLocale(result, LogLevel.Normal, "LogLevel", "Normal");
                MapEnumLocale(result, LogLevel.Debug, "LogLevel", "Debug");
                MapEnumLocale(result, LogLevel.Trace, "LogLevel", "Trace");
                MapEnumLocale(result, LogLevel.Verbose, "LogLevel", "Verbose");

                return result;
            }

            private void MapEnumLocale(Dictionary<string, string> result, LogLevel value, string enumName, string enumValue)
            {
                var csvKey = modData.FixLocaleId(modData.GetEnumValueLocaleID(enumName, enumValue));
                if (!commonsEntries.TryGetValue(csvKey, out var translated) || translated.IsNullOrWhitespace())
                {
                    return;
                }

                result[modData.GetEnumValueLocaleID(value)] = translated;
                result[csvKey] = translated;
            }

            public void Unload()
            {
            }
        }
    }

}

