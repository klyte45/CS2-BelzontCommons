using System;

namespace Kwytto.Utils
{
    public static class NumberExtensions
    {
        public static unsafe float ToFloatBitFlags(this int val)
        {
            return *((float*)&val);
        }
        public static unsafe string ToHexString(this float f)
        {
            var i = *((int*)&f);
            return "0x" + i.ToString("X8");
        }
        public static unsafe float FromHexString(string s)
        {
            try
            {
                var i = Convert.ToInt32(s, 16);
                return *((float*)&i);
            }
            catch
            {
                return 0;
            }
        }
    }
}
