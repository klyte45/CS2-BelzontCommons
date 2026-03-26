using Unity.Mathematics;
using UnityEngine;

namespace Belzont.Utils
{
    public static class FloatNExtensions
    {
        public static float GetAngleXZ(this float3 dir) => math.atan2(dir.z, dir.x) * math.TODEGREES;
        public static float SqrDistance(this float3 a, float3 b)
        {
            float3 vector = a - b;
            return (vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z);
        }

        public static float SqrDistance(this float2 a, float2 b)
        {
            var vector = a - b;
            return (vector.x * vector.x) + (vector.y * vector.y);
        }

        public static float[] ToArray(this float3 f) => new[] { f.x, f.y, f.z };
        public static float GetAngleToPoint(this float2 from, float2 to)
        {
            float ca = to.x - from.x;
            float co = -to.y + from.y;
            //LogUtils.DoLog($"ca = {ca},co = {co};");
            if (co == 0)
            {
                if (ca < 0)
                {
                    return 270;
                }
                else
                {
                    return 90;
                }
            }
            if (co < 0)
            {
                return 360 - (((Mathf.Atan(ca / co) * Mathf.Rad2Deg) + 360) % 360 % 360);
            }
            else
            {
                return 360 - ((Mathf.Atan(ca / co) * Mathf.Rad2Deg + 180 + 360) % 360);
            }
        }
    }
}
