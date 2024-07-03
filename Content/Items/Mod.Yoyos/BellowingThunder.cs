using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Graphics;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BellowingThunderAssets
    {
        public const string ItemPath = $"{_path}BellowingThunder_Item";
        public const string ProjPath = $"{_path}BellowingThunder_Proj";

        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Yoyos/BellowingThunder/";
    }

    public sealed class BellowingThunderItem : YoyoBaseItem
    {
        public override string Texture => BellowingThunderAssets.ItemPath;
        public override int GamepadExtraRange => 10;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.damage = 27;
            Item.knockBack = 3.5f;
            Item.crit = 6;

            Item.shoot = ModContent.ProjectileType<BellowingThunderProjectile>();

            Item.rare = ItemRarityID.Orange;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 4, silver: 0, copper: 0);
        }
    }

    public sealed class BellowingThunderProjectile : YoyoBaseProjectile, IInitializableProjectile
    {
        private YoyoStringRenderer _stringRenderer;

        public override string Texture => BellowingThunderAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 235f;
        public override float TopSpeed => 14f;

        public void Initialize(Projectile _)
        {
            _stringRenderer = new YoyoStringRenderer(Projectile, new IDrawYoyoStringSegment.Gradient(
                (Color.Transparent, true), (Color.Transparent, true), (Color.Cyan, true))
            );
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            _stringRenderer.Draw(mountedCenter + Projectile.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero);
        }
    }
}