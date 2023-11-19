#if THUNDERSTORE

using Belzont.Utils;
using Game.Reflection;
using Game.UI.Localization;
using Game.UI.Menu;
using Game.UI.Widgets;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Entities;

namespace Belzont.Interfaces
{
    public abstract class BasicIMod<T> : BasicIMod where T : BasicModData
    {
        public abstract T CreateNewModData();
        public void SaveModData()
        {
            File.WriteAllText(ModDataFilePath, XmlUtils.DefaultXmlSerialize(ModData));
        }
        protected override void LoadModData()
        {
            if (File.Exists(ModDataFilePath))
            {
                try
                {
                    ModData = XmlUtils.DefaultXmlDeserialize<T>(File.ReadAllText(ModDataFilePath)) ?? CreateNewModData();
                }
                catch
                {
                    LogUtils.DoWarnLog("The settings xml was invalid! Generating new data.");
                    ModData = CreateNewModData();
                }
            }
            else
            {
                ModData = CreateNewModData();
            }
        }


        public static new T ModData { get => (T)BasicIMod.ModData; protected set => BasicIMod.ModData = value; } 

    }
}
#endif