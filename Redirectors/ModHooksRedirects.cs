#if THUNDERSTORE
using Belzont.Interfaces;
using Belzont.Utils;
using Game;
using Game.SceneFlow;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Belzont.Thunderstore
{
    public class ModHooksRedirects : Redirector, IRedirectableWorldless
    {
        public void Awake()
        {
            AddRedirect(typeof(GameManager).GetMethod("CreateSystems", RedirectorUtils.allFlags), GetType().GetMethod("LoadMods", RedirectorUtils.allFlags), GetType().GetMethod("OnCreateWorld", RedirectorUtils.allFlags));
        }
        private static List<BasicIMod> basicMods = new List<BasicIMod>();
        private static FieldInfo updateSystemField = typeof(GameManager).GetField("m_UpdateSystem", RedirectorUtils.allFlags);
        private static void LoadMods()
        {
            var modImplementations = ReflectionUtils.GetSubtypesRecursive(typeof(BasicIMod<>), null);
            foreach (var modImplementation in modImplementations)
            {
                var basicMod = modImplementation.GetConstructor(new System.Type[0]).Invoke(new object[0]) as BasicIMod;
                basicMod.OnLoad();
                basicMods.Add(basicMod);
            }
        }
        private static void OnCreateWorld(GameManager __instance)
        {
            var updateSystem = updateSystemField.GetValue(__instance) as UpdateSystem;
            foreach (var basicMod in basicMods)
            {
                basicMod.OnCreateWorld(updateSystem);
            }
        }
    }
}
#endif