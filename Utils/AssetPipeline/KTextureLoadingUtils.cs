using UnityEngine;

namespace BelzontWE.Commons.Utils.AssetPipeline
{
    /// <summary>
    /// Utility methods for loading textures from disk or memory with size validation.
    /// </summary>
    public static class KTextureLoadingUtils
    {
        /// <summary>
        /// Loads a PNG/JPG from disk, validates dimensions match expected size.
        /// Returns null if file missing, unreadable, or wrong dimensions.
        /// </summary>
        public static Texture2D TryLoadTexture(string file, int width, int height)
        {
            if (!System.IO.File.Exists(file)) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(System.IO.File.ReadAllBytes(file)) && tex.width == width && tex.height == height)
            {
                return tex;
            }
            else
            {
                Object.Destroy(tex);
                return null;
            }
        }

        /// <summary>
        /// Loads a PNG/JPG from byte array, validates dimensions match expected size.
        /// Returns null if unreadable or wrong dimensions.
        /// </summary>
        public static Texture2D TryLoadTexture(byte[] contents, int width, int height)
        {
            if (contents == null || contents.Length == 0) return null;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(contents) && tex.width == width && tex.height == height)
            {
                return tex;
            }
            else
            {
                Object.Destroy(tex);
                return null;
            }
        }
    }
}
