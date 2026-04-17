using Belzont.Utils;
using Colossal.AssetPipeline.Native;
using System;
using UnityEngine;
using BlockCompressionFlags = Colossal.AssetPipeline.Native.NativeTextures.BlockCompressionFlags;
using BlockCompressionFormat = Colossal.AssetPipeline.Native.NativeTextures.BlockCompressionFormat;

namespace BelzontWE.Commons.Utils.AssetPipeline
{
    /// <summary>
    /// Utility methods for BC7 texture compression and decompression using the
    /// game's own native encoder (NativeTextures.BlockCompress via PipelinePlugin.dll).
    /// Requires the game runtime to be active (PipelinePlugin.dll loaded).
    /// </summary>
    public static class KAtlasBC7Utils
    {
        /// <summary>
        /// Returns the byte count required to store a BC7-compressed texture of
        /// the given dimensions (4×4 blocks × 16 bytes each).
        /// </summary>
        public static int GetBC7SizeBytes(int width, int height)
            => (width + 3) / 4 * ((height + 3) / 4) * 16;

        /// <summary>
        /// Compresses a readable RGBA32 <see cref="Texture2D"/> to raw BC7 bytes
        /// using the game's own CPU encoder (<c>NativeTextures.BlockCompress</c>,
        /// effort level 3).
        /// </summary>
        /// <param name="source">Must be RGBA32 format and CPU-readable.</param>
        /// <param name="linear">
        ///   <c>true</c> for linear/UNorm textures (normal maps, masks, control);
        ///   <c>false</c> for sRGB textures (basecolor, emissive).
        /// </param>
        /// <returns>Raw BC7 block data compatible with <c>AtlassingUtils.PreProcessData</c>.</returns>
        public static unsafe byte[] CompressToBC7(Texture2D source, bool linear)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            Texture2D toCompress = source;
            bool isTemp = false;
            if (source.format != TextureFormat.RGBA32 || !source.isReadable)
            {
                var readable = source.MakeReadable(out _);
                toCompress = new Texture2D(readable.width, readable.height, TextureFormat.RGBA32, false, linear);
                toCompress.SetPixels(readable.GetPixels());
                toCompress.Apply(false, false);
                isTemp = true;
            }

            var raw = toCompress.GetRawTextureData();
            int width = toCompress.width;
            int height = toCompress.height;
            var dst = new byte[GetBC7SizeBytes(width, height)];

            // sRGB textures use perceptual quality; linear textures use no bias.
            // Matches game's own TextureImporter.CompressBC sRGB flag usage.
            var flags = linear ? BlockCompressionFlags.None : BlockCompressionFlags.Perceptual;

            fixed (byte* srcPtr = raw)
            fixed (byte* dstPtr = dst)
            {
                // NOTE: BlockCompress returns 0 on FAILURE, non-zero on SUCCESS.
                // This matches the game's own TextureImporter which checks (result == 0) to detect failure.
                int result = NativeTextures.BlockCompress(
                    (IntPtr)srcPtr, width, height,
                    (IntPtr)dstPtr,
                    BlockCompressionFormat.BC7,
                    flags,
                    effort: 3);

                if (result == 0)
                    throw new InvalidOperationException($"NativeTextures.BlockCompress failed (code {result}) for {width}x{height} texture (original format: {source.format}).");
            }

            if (isTemp) UnityEngine.Object.Destroy(toCompress);

            return dst;
        }

        /// <summary>
        /// Compresses a readable RGBA32 <see cref="Texture2D"/> and all its Unity-generated mip levels
        /// to a concatenated raw BC7 byte array: [mip0][mip1][mip2]...
        /// </summary>
        /// <param name="source">The source atlas layer texture.</param>
        /// <param name="linear"><c>true</c> for linear/UNorm layers; <c>false</c> for sRGB layers.</param>
        /// <returns>Concatenated BC7 block data for all generated mip levels (mip0 first).</returns>
        public static byte[] CompressToBC7WithMipChain(Texture2D source, bool linear)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            Texture2D mipmapped = source;
            bool isTemp = false;
            if (!source.isReadable || source.format != TextureFormat.RGBA32 || source.mipmapCount <= 1)
            {
                Color[] mip0Pixels;
                if (!source.isReadable)
                {
                    var readable = source.MakeReadable(out _);
                    mip0Pixels = readable.GetPixels();
                    UnityEngine.Object.Destroy(readable);
                }
                else
                {
                    mip0Pixels = source.GetPixels();
                }
                mipmapped = new Texture2D(source.width, source.height, TextureFormat.RGBA32, mipChain: true, linear: linear);
                mipmapped.SetPixels(mip0Pixels);
                mipmapped.Apply(updateMipmaps: true, makeNoLongerReadable: false);
                isTemp = true;
            }

            int mipCount = mipmapped.mipmapCount;
            var chunks = new byte[mipCount][];
            for (int mip = 0; mip < mipCount; mip++)
            {
                int mipW = Math.Max(1, mipmapped.width >> mip);
                int mipH = Math.Max(1, mipmapped.height >> mip);
                var mipTex = new Texture2D(mipW, mipH, TextureFormat.RGBA32, mipChain: false, linear: linear);
                mipTex.SetPixels(mipmapped.GetPixels(mip));
                mipTex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                chunks[mip] = CompressToBC7(mipTex, linear);
                UnityEngine.Object.Destroy(mipTex);
            }

            if (isTemp) UnityEngine.Object.Destroy(mipmapped);

            int total = 0;
            foreach (var c in chunks) total += c.Length;
            var result = new byte[total];
            int offset = 0;
            foreach (var c in chunks)
            {
                Buffer.BlockCopy(c, 0, result, offset, c.Length);
                offset += c.Length;
            }
            return result;
        }

        /// <summary>
        /// Creates a GPU-only <see cref="Texture2D"/> from raw BC7 block data.
        /// The returned texture has <c>makeNoLongerReadable = true</c> (no CPU copy).
        /// </summary>
        /// <param name="width">Texture width in pixels.</param>
        /// <param name="height">Texture height in pixels.</param>
        /// <param name="bc7Data">Raw BC7 bytes (output of <see cref="CompressToBC7"/>).</param>
        /// <param name="linear"><c>true</c> for linear/UNorm; <c>false</c> for sRGB.</param>
        public static Texture2D CreateFromBC7(int width, int height, byte[] bc7Data, bool linear)
        {
            if (bc7Data == null) throw new ArgumentNullException(nameof(bc7Data));
            int mip0Size = GetBC7SizeBytes(width, height);
            if (bc7Data.Length < mip0Size)
                throw new ArgumentException($"Expected at least {mip0Size} bytes for {width}×{height} BC7 texture, got {bc7Data.Length}.", nameof(bc7Data));

            var tex = new Texture2D(width, height, TextureFormat.BC7, false, linear);
            if (bc7Data.Length == mip0Size)
            {
                tex.LoadRawTextureData(bc7Data);
            }
            else
            {
                var mip0 = new byte[mip0Size];
                Buffer.BlockCopy(bc7Data, 0, mip0, 0, mip0Size);
                tex.LoadRawTextureData(mip0);
            }
            tex.Apply(false, makeNoLongerReadable: true);
            return tex;
        }
    }
}
