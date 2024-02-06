using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Renderers;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class RallyItem : VanillaYoyoItem
    {
        public static float MovementSpeedForFullBonus { get => 8.5f; }
        public static int DamageFullBonus { get => 8; }

        public RallyItem() : base(ItemID.Rally) { }

        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            damage.Flat += GetBonusValue(player);
        }

        public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            damage -= GetBonusValue(player);
        }

        public static float GetBonusFactor(Player player)
            => MathHelper.Clamp(player.velocity.Length() / MovementSpeedForFullBonus, 0f, 1f);

        public static int GetBonusValue(Player player)
            => (int)(GetBonusFactor(player) * DamageFullBonus);
    }

    public class RallyProjectile : VanillaYoyoProjectile
    {
        private SpriteTrailRenderer spriteTrailRenderer;

        public RallyProjectile() : base(ProjectileID.Rally) { }

        public override void PostAI(Projectile proj)
        {
            spriteTrailRenderer?.SetNextPoint(proj.Center + proj.velocity, proj.rotation);
        }

        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage.Flat += RallyItem.GetBonusValue(Main.player[proj.owner]);
        }

        public override void ModifyHitPlayer(Projectile proj, Player target, ref Player.HurtModifiers modifiers)
        {
            modifiers.SourceDamage.Flat += RallyItem.GetBonusValue(Main.player[proj.owner]);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            spriteTrailRenderer ??= InitSpriteTrailRenderer();
            spriteTrailRenderer.SetMaxPoints((int)(RallyItem.GetBonusFactor(Main.player[proj.owner]) * 10f));
            spriteTrailRenderer.Draw(Main.spriteBatch, -Main.screenPosition, lightColor);

            return true;
        }

        private static SpriteTrailRenderer InitSpriteTrailRenderer()
        {
            Main.instance.LoadProjectile(ProjectileID.Rally);

            var texture = TextureAssets.Projectile[ProjectileID.Rally];
            var renderer = new SpriteTrailRenderer(10, texture, texture.Size() * 0.5f, SpriteEffects.None);
            renderer.SetFadingColor(new Color(110, 110, 135, 200));
            renderer.SetScale(f => MathHelper.Lerp(0.9f, 0.7f, f));

            return renderer;
        }
    }
}