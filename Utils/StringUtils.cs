﻿#if BEPINEX_CS2
using BepInEx.Logging;
#endif
namespace Belzont.Utils
{
    public static class StringUtils
    {
        public static string TrimToNull(this string str)
        {
            str = str.Trim();
            return str.Length == 0 ? null : str;
        }
    }
}
