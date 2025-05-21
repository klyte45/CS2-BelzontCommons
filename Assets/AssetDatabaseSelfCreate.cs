using Colossal;
using Colossal.IO.AssetDatabase;
using System.Threading;

namespace BelzontCommons.Assets
{
    internal abstract class AssetDatabaseSelfCreate
    {
        public abstract IAssetDatabase CreateDatabase();
        public abstract void SelfPopulate();

        protected void SelfPopulate<T>(AssetDatabase<T> assetDatabase) where T : struct, IAssetDatabaseDescriptor<T>
        {
            assetDatabase.PopulateFromDataSource(true, CancellationToken.None, TaskManager.instance.progress.GetSubProgress());
            assetDatabase.PopulateFromDataSource(false, CancellationToken.None, TaskManager.instance.progress.GetSubProgress());
        }
    }
}
