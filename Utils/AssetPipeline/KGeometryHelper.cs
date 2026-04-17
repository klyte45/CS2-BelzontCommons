using Belzont.Utils;
using Colossal.Mathematics;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace BelzontWE.Commons.Utils.AssetPipeline
{
    /// <summary>
    /// General-purpose geometry helpers: triangle index arrays, cube vertex data,
    /// and the DecalCubeFromPlanes utility for generating decal cube geometry.
    /// </summary>
    public static class KGeometryHelper
    {
        public static readonly int[] kTriangleIndices = new int[]
        {
            0, 3, 1,
            3, 2, 1
        };

        public static
#if !DEBUG
    readonly
#endif
        int[] kTriangleIndicesCube = new int[]
        {
               0,                      1,   3,
               1,                      0,   2,
               4,                      5,   7,
               5,                      4,   6,
               8,                       9,   11,
               9,                       8,   10,
                12,                       13,    15,
                13,                       12,    14,
                16,                       17,    19,
                17,                       16,    18,
                20,                       21,    23,
                21,                       20,    22,
        };

        public static readonly Vector3[] kVerticesPositionsCube =
        {
            new(- 1,+ 1,+ 1),            new(+ 1,+ 1,- 1),            new(- 1,+ 1,- 1),            new(+ 1,+ 1,+ 1),
            new(+ 1,+ 1,+ 1),            new(- 1,- 1,+ 1),            new(+ 1,- 1,+ 1),            new(- 1,+ 1,+ 1),
            new(- 1,+ 1,- 1),            new(- 1,- 1,+ 1),            new(- 1,+ 1,+ 1),            new(- 1,- 1,- 1),
            new(+ 1,- 1,- 1),            new(- 1,+ 1,- 1),            new(+ 1,+ 1,- 1),            new(- 1,- 1,- 1),
            new(+ 1,- 1,- 1),            new(+ 1,+ 1,+ 1),            new(+ 1,- 1,+ 1),            new(+ 1,+ 1,- 1),
            new(- 1,- 1,+ 1),            new(+ 1,- 1,- 1),            new(+ 1,- 1,+ 1),            new(- 1,- 1,- 1),
        };

        public static readonly Vector2[] kUvCube = kVerticesPositionsCube.Select(x => new Vector2(x.x <= 0 ? 0 : 1, x.z <= 0 ? 0 : 1)).ToArray();

        public static void DecalCubeFromPlanes(Vector3[] originalVertices, Vector2[] originalUv, out Vector3[][] cubeVertices, out int[][] cubeTris, out Vector2[][] uvCube, out Matrix4x4[] cubeOffsets)
        {
            var verticesGroup = originalVertices.Select((x, i) => (x, i)).GroupBy(x => x.i / 4);

            var verticesBounds = verticesGroup.Select(x =>
            {
                var list = x.Select(x => x.x).ToList();
                return (minx: list.Min(x => x.x), maxx: list.Max(x => x.x), miny: list.Min(x => x.y), maxy: list.Max(x => x.y));
            });

            cubeVertices = verticesBounds
                .Select(x =>
                {
                    return kVerticesPositionsCube.Select((y, j) => (Vector3)new float3(math.sign(y.x) * .5f, y.y * -.5f, math.sign(y.z) * .5f)).ToArray();
                })
                .ToArray();
            cubeTris = verticesGroup.Select((_, i) => kTriangleIndicesCube).ToArray();
            cubeOffsets = verticesBounds.Select(x =>
            {
                var bounds = new Bounds2(new(x.minx, x.miny), new(x.maxx, x.maxy));
                var itemProportion = (x.maxx - x.minx) / (x.maxy - x.miny);
                return Matrix4x4.Translate((Vector3)new float3((bounds.min + bounds.Size() / 2) * new float2(-1, 1), 0).xzy) * Matrix4x4.Scale(new float3(bounds.Size(), 1).xzy);
            }).ToArray();

            uvCube = originalUv.Select((x, i) => (x, i)).GroupBy(x => x.i / 4).Select(x =>
            {
                var list = x.Select(x => x.x).ToList();
                return (minx: list.Min(x => x.x), maxx: list.Max(x => x.x), miny: list.Min(x => x.y), maxy: list.Max(x => x.y));
            })
                .Select(x => kUvCube.Select((y, j) => new Vector2(y.x < 0.5f ? x.minx : x.maxx, y.y < 0.5f ? x.miny : x.maxy)).ToArray())
                .ToArray();
        }
    }
}
