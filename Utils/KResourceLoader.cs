using Belzont.Interfaces;
using Colossal.IO.AssetDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Belzont.Utils
{
    public static class KResourceLoader
    {
        public static Assembly RefAssemblyMod => BasicIMod.Instance.GetType().Assembly;
        private static string NamespaceMod => $"{RefAssemblyMod.FullName.Split(",")[0]}.";
        public static Assembly RefAssemblyBelzont => typeof(KResourceLoader).Assembly;

        public static byte[] LoadResourceDataMod(string name) => LoadResourceData(NamespaceMod + name, RefAssemblyMod);
        public static byte[] LoadResourceDataBelzont(string name) => LoadResourceData("Belzont." + name, RefAssemblyBelzont);
        private static byte[] LoadResourceData(string name, Assembly refAssembly)
        {
            var stream = (UnmanagedMemoryStream)refAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                LogUtils.DoInfoLog("Could not find resource: " + name);
                return null;
            }

            var read = new BinaryReader(stream);
            return read.ReadBytes((int)stream.Length);
        }

        public static bool ResourceExistsMod(string name) => RefAssemblyMod.GetManifestResourceStream(NamespaceMod + name) != null;
        public static bool ResourceExistsBelzont(string name) => RefAssemblyBelzont.GetManifestResourceStream("Belzont." + name) != null;

        public static string LoadResourceStringMod(string name) => LoadResourceString(NamespaceMod + name, RefAssemblyMod);
        public static string LoadResourceStringBelzont(string name) => LoadResourceString("Belzont." + name, RefAssemblyBelzont);
        private static string LoadResourceString(string name, Assembly refAssembly)
        {
            var stream = (UnmanagedMemoryStream)refAssembly.GetManifestResourceStream(name);
            if (stream == null)
            {
                LogUtils.DoInfoLog("Could not find resource: " + name);
                return null;
            }

            var read = new StreamReader(stream);
            return read.ReadToEnd();
        }
        public static Texture2D LoadTextureMod(string filename, string folder = "Images")
        {
            return LoadTexture(NamespaceMod + $"UI.{folder}.{filename}.png", RefAssemblyMod);
        }
        private static Texture2D LoadTexture(string filename, Assembly refAssembly)
        {
            try
            {
                var texture = KTextureUtils.New(1, 1);
                texture.LoadImage(LoadResourceData(filename, refAssembly));
                return texture;
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog("The file could not be read:" + e.Message);
            }

            return null;
        }
        public static IEnumerable<string> LoadResourceStringLinesMod(string name) => LoadResourceStringLines(NamespaceMod + name, RefAssemblyMod);
        public static IEnumerable<string> LoadResourceStringLinesBelzont(string name) => LoadResourceStringLines("Belzont." + name, RefAssemblyBelzont);
        private static IEnumerable<string> LoadResourceStringLines(string name, Assembly refAssembly)
        {
            using (var stream = (UnmanagedMemoryStream)refAssembly.GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    LogUtils.DoInfoLog("Could not find resource: " + name);
                    yield break;
                }

                using (var reader = new StreamReader(stream))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }
#if ASSET_BUNDLE_ON
        public static AssetBundle LoadBundle(string filename, Assembly refAssembly = null)
        {
            refAssembly = refAssembly ?? RefAssemblyMod;
            try
            {
                return AssetBundle.LoadFromMemory(LoadResourceData(refAssembly.GetName().Name + "." + filename, refAssembly));
            }
            catch (Exception e)
            {
                LogUtils.DoErrorLog("The file could not be read:" + e.Message);
            }

            return null;
        }
#endif
    }
}
