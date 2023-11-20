using System;
namespace Belzont.AssemblyUtility
{
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
    sealed class KlyteModCanonVersionAttribute : Attribute
    {
        public string CanonVersion { get; }
        public string ThunderstoreVersion { get; }
        public KlyteModCanonVersionAttribute(string canonVersion, string thunderstore)
        {
            CanonVersion = canonVersion;
            ThunderstoreVersion = thunderstore;
        }
    }
}