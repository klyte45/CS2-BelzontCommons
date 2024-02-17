using Belzont.Interfaces;
using System;
namespace Belzont.AssemblyUtility
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    sealed class KlyteModDescriptionAttribute : Attribute
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
            this.DisplayName = DisplayName;
            this.ShortDescription = ShortDescription;
        }


    }
}