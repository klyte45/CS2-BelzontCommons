using System;
using System.Web;
namespace Belzont.AssemblyUtility
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    public sealed class KlyteModDescriptionAttribute : Attribute
    {
        public string ModId { get; }
        public string DisplayName { get; }
        public string ShortDescription { get; }


        public KlyteModDescriptionAttribute(
            string ModId,
            string DisplayName,
            string ShortDescription
            )
        {
            this.ModId = ModId;
            this.DisplayName = HttpUtility.HtmlDecode(DisplayName);
            this.ShortDescription = HttpUtility.HtmlDecode(ShortDescription);
        }


    }
}