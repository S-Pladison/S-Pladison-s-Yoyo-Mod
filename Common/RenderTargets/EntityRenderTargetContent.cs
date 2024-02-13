using Microsoft.Xna.Framework;
using SPYoyoMod.Utils.Rendering;
using Terraria;

namespace SPYoyoMod.Common.RenderTargets
{
    public abstract class EntityRenderTargetContent<T> : RenderTargetContent where T : Entity
    {
        public override Point Size => new(Main.screenWidth, Main.screenHeight);

        private int entityIndex;

        public abstract bool CanDrawEntity(T entity);
        public abstract void DrawEntity(T entity);

        public virtual bool CanRender()
        {
            return true;
        }

        public override bool PreRender()
        {
            if (!CanRender()) return false;

            entityIndex = -1;

            var entities = DrawUtils.GetActiveForDrawEntities<T>();

            for (var i = 0; i < entities.Count; i++)
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

            for (var i = entityIndex; i < entities.Count; i++)
            {
                var entity = entities[i];

                if (!CanDrawEntity(entity)) continue;

                DrawEntity(entity);
            }
        }
    }
}