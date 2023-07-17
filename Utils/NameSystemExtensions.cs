using Game.UI;
using System.Reflection;

namespace Belzont.Utils
{
    public static class NameSystemExtensions
    {
        private static readonly FieldInfo NameTypeFI = typeof(NameSystem.Name).GetField("m_NameType", RedirectorUtils.allFlags);
        private static readonly FieldInfo NameIDFI = typeof(NameSystem.Name).GetField("m_NameID", RedirectorUtils.allFlags);
        private static readonly FieldInfo NameArgsFI = typeof(NameSystem.Name).GetField("m_NameArgs", RedirectorUtils.allFlags);
        public static NameSystem.NameType GetNameType(this NameSystem.Name name) => (NameSystem.NameType)NameTypeFI.GetValue(name);
        public static string GetNameID(this NameSystem.Name name) => (string)NameIDFI.GetValue(name);
        public static string[] GetNameArgs(this NameSystem.Name name) => (string[])NameArgsFI.GetValue(name);

        internal static ValuableName ToValueableName(this NameSystem.Name name) => new(name);

        internal struct ValuableName
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
