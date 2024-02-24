using Game.UI;
using HarmonyLib;
using System.Reflection;
using static Game.UI.NameSystem;

namespace Belzont.Utils
{
    public static class NameSystemExtensions
    {
        public static AccessTools.StructFieldRef<NameSystem.Name, NameType> fieldRefNameType = HarmonyLib.AccessTools.StructFieldRefAccess<NameSystem.Name, NameType>("m_NameType");
        public static AccessTools.StructFieldRef<NameSystem.Name, string> fieldRefNameId = HarmonyLib.AccessTools.StructFieldRefAccess<NameSystem.Name, string>("m_NameID");
        public static AccessTools.StructFieldRef<NameSystem.Name, string[]> fieldRefNameArgs = HarmonyLib.AccessTools.StructFieldRefAccess<NameSystem.Name, string[]>("m_NameArgs");
        public static NameSystem.NameType GetNameType(this NameSystem.Name name) => fieldRefNameType(ref name);
        public static string GetNameID(this NameSystem.Name name) => fieldRefNameId(ref name);
        public static string[] GetNameArgs(this NameSystem.Name name) => fieldRefNameArgs(ref name);

        internal static ValuableName ToValueableName(this NameSystem.Name name) => new(name);

        public class ValuableName
        {
            public readonly string __Type;
            public readonly string name;
            public readonly string nameId;
            public readonly string[] nameArgs;

            internal ValuableName(NameSystem.Name name)
            {
                var type = name.GetNameType();
                switch (type)
                {
                    default:
                    case NameSystem.NameType.Custom:
                        __Type = "names.CustomName";
                        this.name = name.GetNameID();
                        nameId = null;
                        nameArgs = null;
                        break;
                    case NameSystem.NameType.Localized:
                        __Type = "names.LocalizedName";
                        this.name = null;
                        nameId = name.GetNameID();
                        nameArgs = null;
                        break;
                    case NameSystem.NameType.Formatted:
                        __Type = "names.FormattedName";
                        this.name = null;
                        nameId = name.GetNameID();
                        nameArgs = name.GetNameArgs();
                        break;
                }
            }
        }
    }
}
