using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.Extensions;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Accessories
{
    public class FlamingFlowerItem : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "FlamingFlower";

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 38;
            Item.height = 36;

            Item.rare = ItemRarityID.Orange;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped = true;
        }
    }

    public class FlamingFlowerPlayer : ModPlayer
    {
        public bool FlamingFlowerEquipped;

        public override void ResetEffects()
        {
            FlamingFlowerEquipped = false;
        }
    }

    public class FlamingFlowerGlobalProjectile : GlobalProjectile, IPostDrawYoyoStringProjectile/*, IDrawDistortionProjectile*/
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation) { return proj.IsCounterweight(); }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            if (!Main.player[proj.owner].GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return;

            proj.CritChance += 50;
        }

        public void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            /*var owner = Main.player[proj.owner];

            if (!owner.GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return;

            var vectorFromProjToPlayer = owner.Center - proj.Center;
            var vectorFromProjToClone = (vectorFromProjToPlayer * 2);

            proj.position += vectorFromProjToClone;

            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height);
                var origin = new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.OrangeRed, Color.Transparent, MathF.Sin(segmentIndex / (float)segmentCount * MathF.PI));

                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, pos, rect, colour, rotation, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });

            var cwTexture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/FlamingFlower_Counterweights");
            var cwRectangle = new Rectangle(proj.whoAmI % 2 * 10, 0, 10, 10);
            var cwOrigin = new Vector2(5, 5);

            Main.spriteBatch.Draw(cwTexture.Value, proj.Center - Main.screenPosition, cwRectangle, Color.White with { A = 170 }, -proj.rotation, cwOrigin, proj.scale, SpriteEffects.None, 0);

            proj.position -= vectorFromProjToClone;*/
        }

        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            /*var owner = Main.player[proj.owner];

            if (!owner.GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return;

            var cloneHitbox = proj.Hitbox;
            var vectorFromProjToPlayer = owner.Center - proj.Center;
            var vectorFromProjToClone = vectorFromProjToPlayer * 2;
            cloneHitbox.Offset(vectorFromProjToClone.ToPoint());

            if (Vector2.DistanceSquared(target.Center, proj.Center) < Vector2.DistanceSquared())
            {
                modifiers.hit;
            }*/
        }

        public override bool? Colliding(Projectile proj, Rectangle projHitbox, Rectangle targetHitbox)
        {
            var owner = Main.player[proj.owner];

            if (!owner.GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return null;

            var cloneHitbox = projHitbox;
            var vectorFromProjToPlayer = owner.Center - proj.Center;
            var vectorFromProjToClone = vectorFromProjToPlayer * 2;

            cloneHitbox.Offset(vectorFromProjToClone.ToPoint());

            if (!cloneHitbox.Intersects(targetHitbox)) return null;
            return true;
        }

        /*public override void AI(Projectile proj)
        {
            if (!Main.player[proj.owner].GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return;

            Lighting.AddLight(proj.Center, Color.OrangeRed.ToVector3() * 0.3f);

            if (!Main.rand.NextBool(5)) return;

            var dust = Main.dust[Dust.NewDust(proj.position, proj.width, proj.height, DustID.Torch, proj.velocity.X * 0.1f, proj.velocity.Y * 0.1f)];
            dust.noGravity = true;
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            if (!Main.player[proj.owner].GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return true;

            var scale = proj.scale * 0.75f;
            var darkTexture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Extra_1");
            var flowerTexture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/FlamingFlower_Flower");
            var position = proj.Center - Main.screenPosition;
            var rect = new Rectangle(0, 0, flowerTexture.Height(), flowerTexture.Height());
            var origin = new Vector2(flowerTexture.Height(), flowerTexture.Height()) / 2;

            Main.spriteBatch.Draw(darkTexture.Value, position, null, Color.White, 0f, darkTexture.Size() / 2, scale * 0.3f, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, lightColor, (float)Main.timeForVisualEffects * 0.025f, origin, scale, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, lightColor, (float)Main.timeForVisualEffects * 0.025f + MathHelper.PiOver2, origin, scale, SpriteEffects.None, 0);

            rect.X += flowerTexture.Height();

            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f, origin, scale * 1.1f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f + MathHelper.PiOver2, origin, scale * 0.9f, SpriteEffects.None, 0);
            rect.X += flowerTexture.Height();

            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f + MathHelper.PiOver4, origin, scale * 0.9f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f + MathHelper.PiOver2 + MathHelper.PiOver4, origin, scale * 0.9f, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f, origin, 0.7f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f + MathHelper.PiOver2, origin, scale * 0.6f, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f + MathHelper.PiOver4, origin, scale * 0.5f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(flowerTexture.Value, position, rect, Color.White, (float)Main.timeForVisualEffects * 0.020f + MathHelper.PiOver2 + MathHelper.PiOver4, origin, scale * 0.4f, SpriteEffects.None, 0);

            Main.spriteBatch.Draw(darkTexture.Value, position, null, Color.White, 0f, darkTexture.Size() / 2, scale * 0.185f, SpriteEffects.None, 0);

            return true;
        }

        public void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            var owner = Main.player[proj.owner];

            if (!Main.player[proj.owner].GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return;

            DrawUtils.DrawYoyoString(proj, mountedCenter, (index, position, rotation, height, color, segmentCount) =>
            {
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, TextureAssets.FishingLine.Width(), (int)height);
                var origin = new Vector2(TextureAssets.FishingLine.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, Color.OrangeRed, EaseFunctions.InExpo(index / (float)segmentCount) * 5f);

                Main.spriteBatch.Draw(TextureAssets.FishingLine.Value, pos, rect, colour, rotation, origin, 1f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None, 0f);
            });
        }

        public void DrawDistortion(Projectile proj)
        {
            if (!Main.player[proj.owner].GetModPlayer<FlamingFlowerPlayer>().FlamingFlowerEquipped) return;

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Extra_2");
            var position = proj.Center - Main.screenPosition;

            Main.spriteBatch.Draw(texture.Value, position, null, new Color(1, 1, 0.075f), (float)Main.timeForVisualEffects * 0.05f, texture.Size() / 2, proj.scale * 0.4f, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(texture.Value, position, null, new Color(1, 1, 0.075f), (float)Main.timeForVisualEffects * 0.025f, texture.Size() / 2, proj.scale * 0.35f, SpriteEffects.None, 0);
        }*/
    }
}