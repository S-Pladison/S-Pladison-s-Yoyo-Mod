using Microsoft.Xna.Framework;
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
                var ai0 = isSliding ? slidingFlip : 0f;

                Projectile.NewProjectile(Projectile.GetSource_FromAI(), position, Vector2.UnitX * 8f, type, Projectile.damage, Projectile.knockBack, Projectile.owner, ai0);
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), position, Vector2.UnitX * -8f, type, Projectile.damage, Projectile.knockBack, Projectile.owner, ai0);

                timer = 0;
            }
        }
    }

    public class FreezingPointIceProjectile : ModProjectile
    {
        public const int HitboxHeight = 16 * 6;

        public override string Texture => ModAssets.ProjectilesPath + "FreezingPoint";
        public bool IsSliding => SlidingFlip != 0f;
        public int SlidingFlip => (int)Projectile.ai[0];

        private Point lastShakingTilePos;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 12;
            Projectile.height = HitboxHeight;

            Projectile.timeLeft = 60;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            Projectile.velocity.X *= 0.92f;

            if (!IsSliding) return;

            var tilePos = ((SlidingFlip > 0 ? Projectile.Bottom : Projectile.Top) + SlidingFlip * Vector2.UnitY * 8f).ToTileCoordinates();

            if (tilePos == lastShakingTilePos || !WorldGen.InWorld(tilePos.X, tilePos.Y) || !WorldGen.SolidTile(Main.tile[tilePos.X, tilePos.Y])) return;

            var power = -SlidingFlip * Projectile.velocity.Length() * 2f;

            Projectile.NewProjectile(Projectile.GetSource_FromAI(), tilePos.ToWorldCoordinates(), Vector2.Zero, ModContent.ProjectileType<FreezingPointShakingTileProjectile>(), 0, 0, Projectile.owner, power);

            lastShakingTilePos = tilePos;
        }
    }

    public class FreezingPointShakingTileProjectile : ModProjectile
    {
        public const int InitTimeLeft = 25;

        public override string Texture => ModAssets.MiscPath + "Invisible";
        public ref float SlidingFlipPower => ref Projectile.ai[0];

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
            var position = coord.ToWorldCoordinates(0, 0) + new Vector2(0, (float)Math.Sin((1 - Projectile.timeLeft / (float)InitTimeLeft) * MathHelper.Pi)) * SlidingFlipPower - Main.screenPosition;
            var frame = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
            var color = ColorUtils.Multiply((lightColor * 0.7f) with { A = lightColor.A }, WorldGen.paintColor(tile.TileColor));

            Main.spriteBatch.Draw(texture, position, frame, color);

            return false;
        }
    }
}