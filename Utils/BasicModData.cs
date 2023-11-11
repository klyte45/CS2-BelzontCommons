using Game.Modding;
using Game.Settings;

namespace Belzont.Interfaces
{
    public abstract class BasicModData : ModSetting
    {
        public const string kAboutTab = "About";

        public BasicIMod ModInstance { get; private set; }
        public BasicModData Instance { get; private set; }

        protected BasicModData(IMod mod) : base(mod)
        {
            ModInstance = mod as BasicIMod;
        }

        [SettingsUISection(kAboutTab, null)]
        public bool DebugMode { get; set; }

        [SettingsUISection(kAboutTab, null)]
        public string Version => BasicIMod.FullVersion;

        public sealed override void SetDefaults()
        {
            DebugMode = false;
            OnSetDefaults();
        }

        public abstract void OnSetDefaults();

        public string GetEnumValueLocaleID(string classname, string value) => $"Options.{id}.{classname.ToUpper()}[{value}]";

    }
}
