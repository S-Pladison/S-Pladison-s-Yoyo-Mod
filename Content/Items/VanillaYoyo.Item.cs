using SPYoyoMod.Common.Configs;
using Terraria;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class VanillaYoyoItem : GlobalItem
    {
        private readonly int yoyoType;

        public VanillaYoyoItem(int yoyoType)
        {
            this.yoyoType = yoyoType;
        }

        public sealed override bool AppliesToEntity(Item entity, bool lateInstantiation) { return entity.type.Equals(yoyoType); }
        public sealed override bool IsLoadingEnabled(Terraria.ModLoader.Mod mod) { return ModContent.GetInstance<ServerSideConfig>().ReworkedVanillaYoyos; }
    }
}