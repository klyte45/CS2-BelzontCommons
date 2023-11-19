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
    public interface IBasicIMod<T> : IBasicIMod where T : BasicModData
    {
        public void Start()
        {
            OnLoad();
            DefaultWorldInitialization.DefaultWorldInitialized +=
        }
        public abstract T CreateNewModData();
        public void SaveModData()
        {
            File.WriteAllText(ModDataFilePath, XmlUtils.DefaultXmlSerialize(ModData));
        }
        void IBasicIMod.LoadModData()
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


        public static new T ModData { get => (T)IBasicIMod.ModData; protected set => IBasicIMod.ModData = value; } 

    }
}
#endif