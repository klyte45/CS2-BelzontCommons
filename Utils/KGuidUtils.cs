using System;
using Unity.Mathematics;

namespace Belzont.Utils
{
    public static class KGuidUtils
    {
        public unsafe static Guid ToGuid(this Colossal.Hash128 hash128)
        {
            uint4* pnt = &hash128.value;
            return *(Guid*)pnt;
        }
    }
}
