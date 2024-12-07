using Game.SceneFlow;
using System.Linq;
using System.Reflection;
using static Game.Modding.ModManager;

namespace Belzont.Utils
{
    public static class ModManagementUtils
    {
        public static ModInfo GetModDataFromMainAssembly(Assembly mainAssembly) => GameManager.instance.modManager.First(x => x.asset.assembly == mainAssembly);
    }
}
