#if !THUNDERSTORE
using Game.Modding;
using Game.Settings;
#else
using System;
using System.Xml.Serialization;
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

        public static BasicModData Instance { get; private set; }
#if THUNDERSTORE

        public BasicIMod ModInstance => BasicIMod.Instance;
        [XmlIgnore]
        public string id { get; }

        public BasicModData()
        {
            Type type = ModInstance.GetType();
            id = type.Assembly.GetName().Name + "." + type.Namespace + "." + type.Name;
        }
#else
        public BasicIMod ModInstance { get; private set; }
        protected BasicModData(IMod mod) : base(mod)
        {
            ModInstance = mod as BasicIMod;
        }
#endif
#if THUNDERSTORE
        [XmlAttribute]
#else
        [SettingsUISection(kAboutTab, null)]
#endif
        public bool DebugMode { get; set; }
#if !THUNDERSTORE
        [SettingsUISection(kAboutTab, null)]
#endif
        public string Version => BasicIMod.FullVersion;

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
        public string GetPathForOption(string optionName) => id + "." + GetType().Name + "." + optionName;
        public string GetPathForAggroupator(string aggroupatorName) => id + "." + aggroupatorName;
        public string GetSettingsLocaleID() => "Options.SECTION[" + id + "]";
        public string GetOptionLabelLocaleID(string optionName) => "Options.OPTION[" + GetPathForOption(optionName) + "]";
        public string GetOptionDescLocaleID(string optionName) => "Options.OPTION_DESCRIPTION[" + GetPathForOption(optionName) + "]";
        public string GetOptionWarningLocaleID(string optionName) => "Options.WARNING[" + GetPathForOption(optionName) + "]";
        public string GetOptionTabLocaleID(string tabName) => "Options.TAB[" + GetPathForAggroupator(tabName) + "]";
        public string GetOptionGroupLocaleID(string groupName) => "Options.GROUP[" + GetPathForAggroupator(groupName) + "]";
#endif

    }
}