using Unity.Entities;

namespace Belzont.Utils
{
    public static class EntityManagerExtensions
    {
        public static void SafeSetComponentEnabled<T>(this EntityManager entityManager, Entity entity) where T : struct, IEnableableComponent
        {
            if (!entityManager.HasComponent<T>(entity))
            {
                entityManager.AddComponent<T>(entity);
            }

            entityManager.SetComponentEnabled<T>(entity, true);
        }
    }
}