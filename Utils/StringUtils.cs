#if BEPINEX_CS2
using BepInEx.Logging;
#endif
using Colossal.OdinSerializer.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Unity.Collections;

namespace Belzont.Utils
{
    public static class StringUtils
    {
        public static NativeArray<ushort> ToUshortNativeArray(this string s, Allocator alloc) => new(SplitIntoCharacters(s).Select(x => (ushort)char.ConvertToUtf32(x, 0)).ToArray(), alloc);

        public static IEnumerable<string> SplitIntoCharacters(this string s) => Regex.Split(s, string.Empty).Where(x => x.Length > 0);

        public static string TrimToNull(this string str)
        {
            str = str?.Trim();
            return str?.Length == 0 ? null : str;
        }

        public static string Truncate(this string value, int maxLength)
        {
            return value?.Length > maxLength
                ? value[..maxLength]
                : value ?? "";
        }

        public static string SiteMdToGameMd(string markdownText)
        {
            if (markdownText.IsNullOrWhitespace()) return "";
            markdownText = new Regex(@"<").Replace(markdownText, "\\<");
            markdownText = new Regex(@">").Replace(markdownText, "\\>");
            markdownText = new Regex(@"\[([^\]]+)\]\((https://[^)]+)\)").Replace(markdownText, "<$2|$1>");
            foreach (var s in new[] { "__", "_", "\\*\\*\\*", "(?<!\\*)\\*(?!\\*)", "`" })
            {
                markdownText = new Regex($@"(\s|^){s}([^\s][^\r\n]*[^\s]){s}(\s|\r?\Z)").Replace(markdownText, "$1\\<$2\\>$3");
            }
            return markdownText;
        }
    }
}
