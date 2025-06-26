using Unity.Mathematics;
using UnityEngine;

namespace Belzont.Utils
{
    public static class FloatNExtensions
    {
        public static float GetAngleXZ(this float3 dir) => Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
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

        //public static segment3 ToRayY(this float3 vector) => new Segment3(new float3(vector.x, -999999f, vector.z), new float3(vector.x, 999999f, vector.z));
    }
}
