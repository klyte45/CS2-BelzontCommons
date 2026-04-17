using Colossal.IO.AssetDatabase.VirtualTexturing;
using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;

namespace BelzontWE.Commons.Utils.AssetPipeline
{
    /// <summary>
    /// General-purpose VT preprocessing utilities: BC7 validation, format selection,
    /// tile count calculation, and <see cref="AtlassingUtils.PreProcessData"/> invocation.
    /// WE-specific upload, GUID generation, and tile-file management live in WEAtlasVTUtils.
    /// </summary>
    public static class KVTPreprocessingUtils
    {
        /// <summary>
        /// Default VT tile size for unit tests where <c>TextureStreamingSystem</c> is unavailable.
        /// Production code should use <c>tss.tileSize</c> instead.
        /// </summary>
        public const int VT_TILE_SIZE = 512;

        /// <summary>
        /// Padding border in pixels around each VT tile for texture filtering.
        /// </summary>
        public const int VT_PADDING = 8;

        /// <summary>
        /// Returns the <see cref="GraphicsFormat"/> for a given atlas layer.
        /// </summary>
        /// <param name="linear">
        ///   <c>true</c> for linear layers (normal);
        ///   <c>false</c> for sRGB layers (main, mask, control, emissive).
        /// </param>
        public static GraphicsFormat GetBC7Format(bool linear)
            => linear ? GraphicsFormat.RGBA_BC7_UNorm : GraphicsFormat.RGBA_BC7_SRGB;

        /// <summary>
        /// Validates inputs for <see cref="PreprocessForVT"/> without referencing
        /// <c>Colossal.IO.AssetDatabase</c> types, so it can be unit-tested in isolation.
        /// </summary>
        public static void ValidatePreprocessInputs(byte[] bc7Data, int width, int height, int tileSize = VT_TILE_SIZE)
        {
            if (bc7Data == null) throw new ArgumentNullException(nameof(bc7Data));
            if (tileSize <= 0 || !IsPowerOf2(tileSize))
                throw new ArgumentOutOfRangeException(nameof(tileSize), $"Tile size ({tileSize}) must be a positive power of 2.");
            if (width < tileSize)
                throw new ArgumentOutOfRangeException(nameof(width), $"Atlas width ({width}) must be ≥ tileSize ({tileSize}).");
            if (height < tileSize)
                throw new ArgumentOutOfRangeException(nameof(height), $"Atlas height ({height}) must be ≥ tileSize ({tileSize}).");
            if (!IsPowerOf2(width))
                throw new ArgumentException($"Atlas width ({width}) must be a power of 2.", nameof(width));
            if (!IsPowerOf2(height))
                throw new ArgumentException($"Atlas height ({height}) must be a power of 2.", nameof(height));

            int mip0Bytes = KAtlasBC7Utils.GetBC7SizeBytes(width, height);
            if (bc7Data.Length < mip0Bytes)
                throw new ArgumentException($"Expected at least {mip0Bytes} bytes for {width}\u00d7{height} BC7 mip0 data, got {bc7Data.Length}.", nameof(bc7Data));
        }

        public static bool IsPowerOf2(int x) => x > 0 && (x & x - 1) == 0;

        /// <summary>
        /// Preprocesses raw BC7 bytes into the game's VT tile layout
        /// using <see cref="AtlassingUtils.PreProcessData"/>.
        /// </summary>
        /// <param name="bc7Data">
        ///   Concatenated BC7 block data for a single atlas layer: [mip0 bytes][mip1 bytes]...
        /// </param>
        /// <param name="width">Atlas width in pixels (must be power-of-two, ≥ tileSize).</param>
        /// <param name="height">Atlas height in pixels (must be power-of-two, ≥ tileSize).</param>
        /// <param name="format">
        ///   <see cref="GraphicsFormat.RGBA_BC7_SRGB"/> for sRGB layers or
        ///   <see cref="GraphicsFormat.RGBA_BC7_UNorm"/> for linear layers (normal).
        /// </param>
        /// <param name="tileSize">VT tile size from <c>TextureStreamingSystem.tileSize</c>.</param>
        /// <returns>VT-tiled byte data ready for registration into the streaming system.</returns>
        public static NativeArray<byte> PreprocessForVT(byte[] bc7Data, int width, int height, GraphicsFormat format, int tileSize = VT_TILE_SIZE)
        {
            ValidatePreprocessInputs(bc7Data, width, height, tileSize);

            var layerInfo = new AtlassingUtils.LayerInfo(tileSize, format);

            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)tileSize, 2.0);

            int mipLevelsNeeded = maxLevel + 2;
            int totalSrcBytes = 0;
            int mipW = width, mipH = height;
            for (int m = 0; m < mipLevelsNeeded; m++)
            {
                totalSrcBytes += KAtlasBC7Utils.GetBC7SizeBytes(mipW, mipH);
                mipW = Math.Max(4, mipW / 2);
                mipH = Math.Max(4, mipH / 2);
            }

            var input = new NativeArray<byte>(totalSrcBytes, Allocator.TempJob, NativeArrayOptions.ClearMemory);
            NativeArray<byte>.Copy(bc7Data, 0, input, 0, Math.Min(bc7Data.Length, totalSrcBytes));

            try
            {
                var inputSlice = new NativeSlice<byte>(input);
                AtlassingUtils.PreProcessData(inputSlice, out var processedData, width, height, tileSize, maxLevel, VT_PADDING, layerInfo);
                return processedData;
            }
            finally
            {
                input.Dispose();
            }
        }

        /// <summary>
        /// Computes the total number of VT tiles for a given texture at the specified max level.
        /// </summary>
        public static int GetTileCount(int width, int height, int maxLevel, int tileSize = VT_TILE_SIZE)
            => AtlassingUtils.TextureRelativeTileIndex(width, height, maxLevel + 1, 0, 0, tileSize);

        /// <summary>
        /// Computes the expected preprocessed byte count for a given atlas layer.
        /// </summary>
        public static int GetPreprocessedByteCount(int width, int height, GraphicsFormat format, int tileSize = VT_TILE_SIZE)
        {
            var layerInfo = new AtlassingUtils.LayerInfo(tileSize, format);
            int maxLevel = (int)Math.Log(Math.Min(width, height) / (double)tileSize, 2.0);
            int tileCount = GetTileCount(width, height, maxLevel, tileSize);
            return tileCount * (layerInfo.tileBlockSize + layerInfo.trilinearTileBlockSize) * layerInfo.blockSizeInBytes;
        }
    }
}
