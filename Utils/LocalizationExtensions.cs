using Colossal.Localization;
using Game.SceneFlow;
using Game.UI.Localization;

namespace Belzont.Utils
{
    public static class LocalizationExtensions
    {
        private static LocalizationDictionary LocalizationDictionary => GameManager.instance.localizationManager.activeDictionary;

        /// <summary>
        /// One-off translation from a key.
        /// If the key is not found in the dictionary, it is returned as is.
        /// </summary>
        public static string Translate(string key) => LocalizationDictionary.TryGetValue(key, out var value)
                ? value
                : key;

        /// <summary>
        /// Same as <see cref="Translate(string)"/> but with variables interpolation.
        /// </summary>
        /// <param name="key">
        /// A key that points to a string that <see cref="string.Format(string,object[])"/> can format.
        /// </param>
        /// <param name="args">Values to interpolate</param>
        public static string Translate(string key, params object[] args) => args is null ? Translate(key) : string.Format(Translate(key), args);


        public static string Translate(this LocalizedString item)
        {
            if (item.id == null) return item.value;
            var result = Translate(item.id);
            if (item.args != null)
            {
                foreach (var arg in item.args)
                {
                    result = result.Replace($"{{{arg.Key}}}", arg.Value is LocalizedString ls ? ls.Translate() : "<?>");
                }
            }
            return result;
        }
    }
}
