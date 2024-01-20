using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class EchoCounterweightItem : ModItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "EchoCounterweight"; }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 40;
            Item.height = 36;

            Item.rare = ItemRarityID.Pink;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<EchoCounterweightPlayer>().EchoCounterweightEquipped = true;
            player.ProvideRandomCounterweight();
        }

        public override void AddRecipes()
        {
            for (int counterweightType = ItemID.BlackCounterweight; counterweightType <= ItemID.YellowCounterweight; counterweightType++)
            {
                var recipe = CreateRecipe();
                recipe.AddIngredient(counterweightType, 1);
                recipe.AddIngredient(ItemID.WarriorEmblem, 1);
                recipe.AddIngredient(ItemID.CrystalShard, 20);
                recipe.AddIngredient(ItemID.SoulofSight, 7);
                recipe.AddTile(TileID.MythrilAnvil);
                recipe.Register();
            }
        }
    }

    public class EchoCounterweightPlayer : ModPlayer
    {
        public bool EchoCounterweightEquipped;

        public override void ResetEffects()
        {
            EchoCounterweightEquipped = false;
        }
    }

    public class EchoCounterweightGlobalProjectile : GlobalProjectile, IPostDrawYoyoStringProjectile
    {
        private SpriteTrailRenderer trailRenderer;

        public Asset<Texture2D> CounterweightTexture { get => ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/EchoCounterweight_Counterweights"); }
        public override bool InstancePerEntity { get => true; }
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation) { return proj.IsCounterweight(); }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            if (Main.dedServ || !IsAccessoryEquiped(proj, out Player owner)) return;

            ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/EchoCounterweight_Counterweights");

            trailRenderer = new SpriteTrailRenderer(5, CounterweightTexture, new Vector2(10, 10), SpriteEffects.None)
                .SetFrame(new Rectangle(Main.rand.Next(0, 2) * 20, 0, 20, 20))
                .SetColor(f => Color.Lerp(Color.White, Color.Purple, f) * (1 - f));
        }

        public override void PostAI(Projectile proj)
        {
            if (Main.dedServ || !IsAccessoryEquiped(proj, out Player owner)) return;

            var vectorFromProjToPlayer = owner.Center - proj.Center;
            var vectorFromProjToClone = (vectorFromProjToPlayer * 2);

            trailRenderer.SetNextPoint(proj.Center - proj.velocity + vectorFromProjToClone, -proj.rotation);
        }

        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!IsAccessoryEquiped(proj, out Player owner)) return;

            modifiers.SourceDamage += 0.25f;
            modifiers.HitDirectionOverride = (target.Center.X - owner.Center.X) >= 0 ? 1 : -1;
        }

        public override void ModifyHitPlayer(Projectile proj, Player target, ref Player.HurtModifiers modifiers)
        {
            if (!IsAccessoryEquiped(proj, out Player owner)) return;

            modifiers.SourceDamage += 0.25f;
            modifiers.HitDirectionOverride = (target.Center.X - owner.Center.X) >= 0 ? 1 : -1;
        }

        public override bool? Colliding(Projectile proj, Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsAccessoryEquiped(proj, out Player owner)) return null;

            var cloneHitbox = projHitbox;
            var vectorFromProjToPlayer = owner.Center - proj.Center;
            var vectorFromProjToClone = vectorFromProjToPlayer * 2;

            cloneHitbox.Offset(vectorFromProjToClone.ToPoint());

            if (!cloneHitbox.Intersects(targetHitbox)) return null;
            return true;
        }

        void IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            var owner = Main.player[proj.owner];

            if (!owner.GetModPlayer<EchoCounterweightPlayer>().EchoCounterweightEquipped) return;

            var vectorFromProjToPlayer = owner.Center - proj.Center;
            var vectorFromProjToClone = (vectorFromProjToPlayer * 2);

            proj.position += vectorFromProjToClone;

            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height);
                var origin = new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, new Color(79, 230, 124), EaseFunctions.InQuart(segmentIndex / (float)segmentCount) * 2f);

                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, pos, rect, colour, rotation, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });

            trailRenderer.Draw(Main.spriteBatch, -Main.screenPosition, Color.LightGray with { A = (byte)(255f * 0.75f) });

            var cwRectangle = trailRenderer.Frame;
            var cwOrigin = trailRenderer.Origin;

            Main.spriteBatch.Draw(CounterweightTexture.Value, proj.Center - Main.screenPosition, cwRectangle, Color.White with { A = 170 }, -proj.rotation, cwOrigin, proj.scale, SpriteEffects.None, 0);

            proj.position -= vectorFromProjToClone;
        }

        private static bool IsAccessoryEquiped(Projectile proj, out Player owner)
        {
            owner = Main.player[proj.owner];

            return owner.GetModPlayer<EchoCounterweightPlayer>().EchoCounterweightEquipped;
        }
    }
}