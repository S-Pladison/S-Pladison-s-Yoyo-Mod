using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class FreezingPointItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "FreezingPoint";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 20;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<FreezingPointProjectile>();

            Item.rare = ItemRarityID.Blue;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }
    }

    public class FreezingPointProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "FreezingPoint";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;

        private int timer;

        public override void AI()
        {
            timer++;

            if (timer > 90)
            {
                var isSliding = false;
                var slidingPos = Vector2.Zero;
                var slidingFlip = 0f;

                var center = Projectile.Center.ToTileCoordinates();
                var height = FreezingPointIceProjectile.HitboxHeight / 32f;

                for (int i = 0; i <= height; i++)
                {
                    var tileCoord = new Point(center.X, center.Y + i);

                    if (WorldGen.InWorld(tileCoord.X, tileCoord.Y) && WorldGen.SolidTile(tileCoord))
                    {
                        isSliding = true;
                        slidingPos = tileCoord.ToWorldCoordinates(Projectile.Center.X - center.X * 16f, -FreezingPointIceProjectile.HitboxHeight / 2f);
                        slidingFlip = 1f;
                        break;
                    }
                }

                for (int i = -1; i >= -height; i--)
                {
                    var tileCoord = new Point(center.X, center.Y + i);

                    if (WorldGen.InWorld(tileCoord.X, tileCoord.Y) && WorldGen.SolidTile(tileCoord))
                    {
                        if (isSliding)
                        {
                            isSliding = false;
                            slidingPos = Vector2.Zero;
                            slidingFlip = 0f;
                        }
                        else
                        {
                            isSliding = true;
                            slidingPos = tileCoord.ToWorldCoordinates(Projectile.Center.X - center.X * 16f, FreezingPointIceProjectile.HitboxHeight / 2f + 16f);
                            slidingFlip = -1f;
                        }
                        break;
                    }
                }

                var position = isSliding ? slidingPos : Projectile.Center;
                var type = ModContent.ProjectileType<FreezingPointIceProjectile>();

                Projectile.NewProjectile(Projectile.GetSource_FromAI(), position, Vector2.UnitX * 10f, type, Projectile.damage, Projectile.knockBack, Projectile.owner, slidingFlip);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), position, Vector2.UnitX * -10f, type, Projectile.damage, Projectile.knockBack, Projectile.owner, slidingFlip);

                timer = 0;
            }
        }
    }

    public class FreezingPointIceProjectile : ModProjectile
    {
        public const int HitboxHeight = 16 * 5;
        public const int InitTimeLeft = 65;

        private static readonly EasingBuilder colorEasing = new(
            (EasingFunctions.InOutCubic, 0.2f, 0f, 1f),
            (EasingFunctions.Linear, 0.8f, 1f, 1f)
        );

        public override string Texture => ModAssets.ProjectilesPath + "FreezingPointIce";
        public bool IsSliding => SlidingFlip != 0f;
        public int SlidingFlip => (int)Projectile.ai[0];
        public float TimeLeftProgress => 1f - Projectile.timeLeft / (float)InitTimeLeft;

        private Point lastShakingTilePos;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 12;
            Projectile.height = HitboxHeight;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.hide = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity.X *= 0.92f;

            if (!IsSliding) return;

            Projectile.direction = MathF.Sign(Projectile.velocity.X);

            var tilePos = ((SlidingFlip > 0 ? Projectile.Bottom : Projectile.Top) + SlidingFlip * Vector2.UnitY * 8f).ToTileCoordinates();

            if (tilePos == lastShakingTilePos || !WorldGen.InWorld(tilePos.X, tilePos.Y) || !WorldGen.SolidTile(Main.tile[tilePos.X, tilePos.Y])) return;

            var power = -SlidingFlip * 8f;

            Projectile.NewProjectile(Projectile.GetSource_FromAI(), tilePos.ToWorldCoordinates(), Vector2.Zero, ModContent.ProjectileType<FreezingPointShakingTileProjectile>(), 0, 0, Projectile.owner, power);

            lastShakingTilePos = tilePos;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.Knockback += 1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var texture = TextureAssets.Projectile[Type];

            var position = (SlidingFlip >= 0 ? Projectile.Bottom : Projectile.Top) + new Vector2(Projectile.direction * -20, 0) - Main.screenPosition;
            var origin = SlidingFlip >= 0 ? new Vector2(texture.Width() * 0.5f, texture.Height()) : new Vector2(texture.Width() * 0.5f, 0);

            var effect = SpriteEffects.None;
            effect |= Projectile.direction >= 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            effect |= SlidingFlip >= 0 ? SpriteEffects.None : SpriteEffects.FlipVertically;

            Main.spriteBatch.Draw(texture.Value, position, null, lightColor * colorEasing.Evaluate(TimeLeftProgress), 0f, origin, 1f, effect, 0f);

            return false;
        }
    }

    public class FreezingPointShakingTileProjectile : ModProjectile
    {
        public const int InitTimeLeft = 25;

        public override string Texture => ModAssets.MiscPath + "Invisible";
        public ref float Power => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.hide = true;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var coord = Projectile.Center.ToTileCoordinates();
            var tile = Framing.GetTileSafely(coord);

            if (tile == null || !tile.HasTile || !WorldGen.SolidTile(tile)) return false;

            var texture = TextureAssets.Tile[tile.TileType].Value;
            var position = coord.ToWorldCoordinates(0, 0) + new Vector2(0, (float)Math.Sin((1 - Projectile.timeLeft / (float)InitTimeLeft) * MathHelper.Pi)) * Power - Main.screenPosition;
            var frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
            var color = ColorUtils.Multiply((lightColor * 0.7f) with { A = lightColor.A }, WorldGen.paintColor(tile.TileColor));

            Main.spriteBatch.Draw(texture, position, frame, color);

            return false;
        }
    }
}