using Colossal;
using Game.SceneFlow;
using Game.UI;
using Game.UI.Localization;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Belzont.Utils
{
    public class KFileUtils
    {
        #region File & Prefab Utils
        public static readonly string BASE_FOLDER_PATH = Path.Combine(Application.persistentDataPath, "ModsData", ".Klyte45Mods");
        public static readonly string OLD_BASE_FOLDER_PATH = Path.Combine(Application.persistentDataPath, "ModsData", "Klyte45Mods");

        public static FileInfo EnsureFolderCreation(string folderName)
        {
            if (File.Exists(folderName) && (File.GetAttributes(folderName) & FileAttributes.Directory) != FileAttributes.Directory)
            {
                File.Delete(folderName);
            }
            if (!Directory.Exists(folderName))
            {
                if (new DateTime().Year < 2027 && folderName.StartsWith(BASE_FOLDER_PATH) && !Directory.Exists(BASE_FOLDER_PATH) && Directory.Exists(OLD_BASE_FOLDER_PATH))
                {
                    try
                    {
                        Directory.Move(OLD_BASE_FOLDER_PATH, BASE_FOLDER_PATH);
                        return EnsureFolderCreation(folderName);
                    }
                    catch (Exception e)
                    {
                        var errorText = $"Failed transferring old mod data folder from 'Klyte45Mods' ({OLD_BASE_FOLDER_PATH}) to the new '.Klyte45Mods' - notice the dot at beginning of the new name.\n\nTo migrate your old data from Klyte45 mods, please move the files manually using Windows Explorer/line commands on Windows.\n\nDespite of this, you can use the mod normally.";

                        GameManager.instance.onGameLoadingComplete += ShowErrorMoveFolder;

                        LogUtils.DoErrorLog(errorText, e);
                        Directory.CreateDirectory(folderName);
                    }
                }
                else
                {
                    Directory.CreateDirectory(folderName);
                }
            }
            return new FileInfo(folderName);
        }

        private static void ShowErrorMoveFolder(Colossal.Serialization.Entities.Purpose purpose, Game.GameMode mode)
        {
            var errorText = $"Failed transferring old mod data folder from 'Klyte45Mods' ({OLD_BASE_FOLDER_PATH}) to the new '.Klyte45Mods' - notice the dot at beginning of the new name.\n\nTo migrate your old data from Klyte45 mods, please move the files manually using Windows Explorer/line commands on Windows.\n\nDespite of this, you can use the mod normally.";
            var dialog2 = new MessageDialog(
                               LocalizedString.Value("Error moving Klyte45 mods files to new location"),
                               LocalizedString.Value(errorText),
                               null,
                               false,
                               LocalizedString.Id("Common.OK"),
                               LocalizedString.Value("Go to folder")
                               );
            GameManager.instance.userInterface.appBindings.ShowMessageDialog(dialog2, (x) =>
            {
                switch (x)
                {
                    case 2:
                        RemoteProcess.OpenFolder(Path.GetDirectoryName(OLD_BASE_FOLDER_PATH));
                        break;
                }

            });
            GameManager.instance.onGameLoadingComplete -= ShowErrorMoveFolder;
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
