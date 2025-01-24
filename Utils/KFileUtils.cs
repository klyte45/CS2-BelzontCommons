using System.IO;
using System.Linq;
using UnityEngine;

namespace Belzont.Utils
{
    public class KFileUtils
    {
        #region File & Prefab Utils
        public static readonly string BASE_FOLDER_PATH = Path.Combine(Application.persistentDataPath, "ModsData", "Klyte45Mods");

        public static FileInfo EnsureFolderCreation(string folderName)
        {
            if (File.Exists(folderName) && (File.GetAttributes(folderName) & FileAttributes.Directory) != FileAttributes.Directory)
            {
                File.Delete(folderName);
            }
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            return new FileInfo(folderName);
        }
        public static bool IsFileCreated(string fileName) => File.Exists(fileName);
        public static string[] GetAllFilesEmbeddedAtFolder(string packageDirectory, string extension)
        {
            var executingAssembly = KResourceLoader.RefAssemblyMod;
            string folderName = $"Klyte.{packageDirectory}";
            return executingAssembly
                .GetManifestResourceNames()
                .Where(r => r.StartsWith(folderName) && r.EndsWith(extension))
                .Select(r => r[(folderName.Length + 1)..])
                .ToArray();
        }

        public static string RemoveInvalidFilenameChars(string fileName)
        {
            return string.Join("_", (fileName ?? "").Split(Path.GetInvalidFileNameChars()));
        }
        #endregion
    }
}
