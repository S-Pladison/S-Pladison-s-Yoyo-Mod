using Microsoft.Xna.Framework;
using SPYoyoMod.Utils;
using Terraria;

namespace SPYoyoMod.Common.RenderTargets
{
    /*public abstract class EntityRenderTargetContent<T> : RenderTargetContent where T : Entity
    {
        private readonly Dictionary<int, T> entities;

        public EntityRenderTargetContent()
        {
            entities = new Dictionary<int, T>();
        }

        public void AddEntity(T entity)
        {
            entities[entity.whoAmI] = entity;
        }
    }*/

    public abstract class EntityRenderTargetContent<T> : RenderTargetContent where T : Entity
    {
        public override Point Size { get => new(Main.screenWidth, Main.screenHeight); }

        private int entityIndex;

        public virtual bool CanRender() { return true; }
        public abstract bool CanDrawEntity(T entity);
        public abstract void DrawEntity(T entity);

        public override bool PreRender()
        {
            if (!CanRender()) return false;

            entityIndex = -1;

            var entities = DrawUtils.GetActiveForDrawEntities<T>();

            for (int i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];

                if (!CanDrawEntity(entity)) continue;

                entityIndex = i;
                break;
            }

            return entityIndex >= 0;
        }

        protected void DrawEntities()
        {
            var entities = DrawUtils.GetActiveForDrawEntities<T>();

            DrawEntity(entities[entityIndex]);

            entityIndex++;

            for (int i = entityIndex; i < entities.Count; i++)
            {
                var entity = entities[i];

                if (!CanDrawEntity(entity)) continue;

                DrawEntity(entity);
            }
        }
    }
}