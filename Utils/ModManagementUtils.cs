using Colossal.IO.AssetDatabase;
using System.Linq;
using System.Reflection;

namespace Belzont.Utils
{
    public static class ModManagementUtils
    {
        public static AssetData GetModDataFromMainAssembly(Assembly mainAssembly) => AssetDatabase.global.GetAssets<ExecutableAsset>().First(x => x.assembly == mainAssembly);
    }
}
