#if BEPINEX_CS2
using BepInEx.Logging;
#endif
using System;
using UnityEngine;

namespace Belzont.Utils
{
    public static class KTextureUtils
    {
        public static Texture2D New(int width, int height, TextureFormat format = TextureFormat.RGBA32, bool linear = true)
        {
            Texture2D texture2D = new Texture2D(width, height, format, false, linear)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            return texture2D;
        }
        public static Texture2D NewSingleColorForUI(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        public static Texture2D DeCompress(this Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Default);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height, source.format, false, true)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };

            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        private static byte[] GetBytes(Texture2D img)
        {
            byte[] bytes = img.EncodeToPNG();
            if (bytes is null)
            {
                bytes = img.DeCompress().EncodeToPNG();
            }

            return bytes;
        }
        public static string ToBase64(this Texture2D src)
        {
            byte[] imageData = src.EncodeToPNG();
            return Convert.ToBase64String(imageData);
        }

        public static Texture2D Base64ToTexture2D(string encodedData, bool linear = true)
        {
            byte[] imageData = Convert.FromBase64String(encodedData);

            GetImageSize(imageData, out int width, out int height);

            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, linear)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };
            texture.LoadImage(imageData);
            return texture;
        }
        private static void GetImageSize(byte[] imageData, out int width, out int height)
        {
            width = ReadInt(imageData, 3 + 15);
            height = ReadInt(imageData, 3 + 15 + 2 + 2);
        }
        private static int ReadInt(byte[] imageData, int offset)
        {
            return (imageData[offset] << 8) | imageData[offset + 1];
        }
        public static Texture2D MakeReadable(this Texture texture)
        {
            if (texture is Texture2D t2d && t2d.isReadable) return t2d;
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0);
            Graphics.Blit(texture, temporary);
            Texture2D result = temporary.ToTexture2D();
            RenderTexture.ReleaseTemporary(temporary);
            return result;
        }

        public static Texture2D ToTexture2D(this RenderTexture rt)
        {
            RenderTexture active = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D texture2D = new Texture2D(rt.width, rt.height);
            texture2D.ReadPixels(new Rect(0f, 0f, rt.width, rt.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = active;
            return texture2D;
        }
    }
}
