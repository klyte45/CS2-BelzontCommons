#if !THUNDERSTORE
using Game.Modding;
using Game.Settings;
#else
using System;
using IMod = Belzont.Interfaces.IBasicIMod;
#endif

namespace Belzont.Interfaces
{
#if THUNDERSTORE
    public abstract partial class BasicModData    
#else
       public abstract partial class BasicModData  : ModSetting
#endif
    {
        public const string kAboutTab = "About";

        public IBasicIMod ModInstance { get; private set; }
        public BasicModData Instance { get; private set; }
#if THUNDERSTORE
        public string id { get; }

        public BasicModData(IMod mod)
        {
            Type type = mod.GetType();
            id = type.Assembly.GetName().Name + "." + type.Namespace + "." + type.Name;
            ModInstance = mod as IBasicIMod;
        }
#else
        protected BasicModData(IMod mod) : base(mod)
        {
            ModInstance = mod as BasicIMod;
        }
#endif
#if !THUNDERSTORE
        [SettingsUISection(kAboutTab, null)]
#endif
        public bool DebugMode { get; set; }
#if !THUNDERSTORE
        [SettingsUISection(kAboutTab, null)]
#endif
        public string Version => IBasicIMod.FullVersion;

        public

#if !THUNDERSTORE
            sealed override
#endif
            void SetDefaults()
        {
            DebugMode = false;
            OnSetDefaults();
        }

        public abstract void OnSetDefaults();

        public string GetEnumValueLocaleID(string classname, string value) => $"Options.{id}.{classname.ToUpper()}[{value}]";

#if THUNDERSTORE
        public string GetSettingsLocaleID() => "Options.SECTION[" + id + "]";
        public string GetOptionLabelLocaleID(string optionName) => "Options.OPTION[" + id + "." + GetType().Name + "." + optionName + "]";
        public string GetOptionDescLocaleID(string optionName) => "Options.OPTION_DESCRIPTION[" + id + "." + GetType().Name + "." + optionName + "]";
        public string GetOptionWarningLocaleID(string optionName) => "Options.WARNING[" + id + "." + GetType().Name + "." + optionName + "]";
        public string GetOptionTabLocaleID(string tabName) => "Options.TAB[" + id + "." + tabName + "]";
        public string GetOptionGroupLocaleID(string groupName) => "Options.GROUP[" + id + "." + groupName + "]";
#endif

    }
}