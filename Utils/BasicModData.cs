using Belzont.AssemblyUtility;
using Game.Modding;
using Game.Settings;
using System.Reflection;


namespace Belzont.Interfaces
{

    public abstract partial class BasicModData : ModSetting

    {
        public const string kAboutTab = "About";

        public static BasicModData Instance { get; private set; }

        public BasicIMod ModInstance { get; private set; }
        protected BasicModData(IMod mod) : base(mod)
        {
            ModInstance = mod as BasicIMod;
        }


        [SettingsUISection(kAboutTab, null)]

        public bool DebugMode { get; set; }

        [SettingsUISection(kAboutTab, null)]
        public string Version => BasicIMod.FullVersion;


        [SettingsUISection(kAboutTab, null)]

        public string CanonVersion => ModInstance?.GetType()?.Assembly?.GetCustomAttribute<KlyteModCanonVersionAttribute>()?.CanonVersion ?? "<N/D>";

        public sealed override void SetDefaults()
        {
            DebugMode = false;
            OnSetDefaults();
        }

        public abstract void OnSetDefaults();

        public string GetEnumValueLocaleID(string classname, string value) => $"Options.{id}.{classname.ToUpper()}[{value}]";


    }
}