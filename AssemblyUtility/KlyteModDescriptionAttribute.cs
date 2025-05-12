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
        public string ForumsURL { get; }
        public string GitHubURL { get; }

        public KlyteModDescriptionAttribute(
            string ModId,
            string DisplayName,
            string ShortDescription,
            string ForumsURL,
            string GitHubURL
            )
        {
            this.ModId = ModId;
            this.DisplayName = HttpUtility.HtmlDecode(DisplayName);
            this.ShortDescription = HttpUtility.HtmlDecode(ShortDescription);
            this.ForumsURL = ForumsURL;
            this.GitHubURL = GitHubURL;
        }

        public KlyteModDescriptionAttribute()
        {
            ModId = "0";
            DisplayName = "0";
            ShortDescription = "0";
            ForumsURL = "0";
            GitHubURL = "0";
        }
    }
}