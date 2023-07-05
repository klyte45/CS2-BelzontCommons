using Belzont.Utils;
using Colossal.UI;
using Game.SceneFlow;
using Game.UI.Menu;
using System.Collections.Generic;
using System.Reflection;

namespace Belzont.Interfaces
{
    public class OptionsUISystemRedirector : Redirector, IRedirectable
    {

        public void Awake()
        {
            AddRedirect(typeof(OptionsUISystem).GetMethod("OnCreate", RedirectorUtils.allFlags), null, GetType().GetMethod("AfterOnCreate", RedirectorUtils.allFlags));
        }
        private static PropertyInfo OptionsUISystemOptions = typeof(OptionsUISystem).GetProperty("options", RedirectorUtils.allFlags);

        private static void AfterOnCreate(OptionsUISystem __instance)
        {
            var optionsList = ((List<OptionsUISystem.Page>)OptionsUISystemOptions.GetValue(__instance));
            optionsList.Add(BasicIMod.Instance.BuildModPage());
            var uiSys = GameManager.instance.userInterface.view.uiSystem;
            ((DefaultResourceHandler)uiSys.resourceHandler).HostLocationsMap.Add(BasicIMod.Instance.CouiHost, new List<string> { BasicIMod.ModInstallFolder });
        }
    }
}
