using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class AmazonItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.JungleYoyo;
    }

    public class AmazonProjectile : VanillaYoyoProjectile
    {
        public override int YoyoType => ProjectileID.JungleYoyo;

        private Vector2? startToReturnPosition;
        private bool initialized;

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            proj.localAI[1] = 0f;
        }

        public override void AI(Projectile proj)
        {
            if (!initialized)
            {
                ModContent.GetInstance<AmazonEffectHandler>()?.ProjectileObserver.Add(proj);
                initialized = true;
            }

            var owner = Main.player[proj.owner];
            var isReturning = proj.ai[0] == -1;

            // Update proj.localAI[1] (proj scale mult)

            if (isReturning)
            {
                if (!startToReturnPosition.HasValue)
                {
                    startToReturnPosition = proj.Center;
                }

                proj.localAI[1] = Vector2.DistanceSquared(owner.Center, proj.Center) / Vector2.DistanceSquared(owner.Center, startToReturnPosition.Value);
            }
            else
            {
                proj.localAI[1] += 0.2f;
            }

            proj.localAI[1] = MathHelper.Clamp(proj.localAI[1], 0f, 1f);

            // Poisoning from string

            var _ = float.NaN;

            foreach (var target in Main.npc.Where(x => x.active && (x.CanBeChasedBy(proj) || x.type.Equals(NPCID.TargetDummy))))
            {
                if (Collision.CheckAABBvLineCollision(target.Hitbox.TopLeft(), target.Hitbox.Size(), proj.Center, owner.MountedCenter, 4, ref _))
                {
                    target.AddBuff(BuffID.Poisoned, 60 * 5);
                }
            }

            // Spawn dusts

            if (Main.rand.NextBool(9))
            {
                var dustPosition = proj.Center + Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)) * 63f * proj.localAI[1];
                Dust.NewDustPerfect(dustPosition, ModContent.DustType<AmazonDust>());
            }
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 60 * 5);

            // Reducing knockback from npc
            // Vanilla: 16f
            // New: 10f

            var vector = Vector2.Normalize(proj.Center - target.Center);
            var scaleFactor = -6f;
            proj.velocity += vector * scaleFactor;

            proj.netUpdate = true;
        }

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_Ring", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var segmentCount = (int)MathF.Ceiling(15 * proj.localAI[1]);

            for (var i = 0; i < segmentCount; i++)
            {
                var origin = new Vector2(10, 15);
                var frame = new Rectangle((((i + 23) * 37) ^ proj.whoAmI) % 3 * 20, 0, 20, 30);
                var angle = i * MathHelper.TwoPi / segmentCount + (float)Main.timeForVisualEffects * 0.025f;
                var pos = position + Vector2.UnitX.RotatedBy(angle) * 63f * proj.localAI[1];
                var color = Lighting.GetColor((pos + Main.screenPosition).ToTileCoordinates(), Color.White);

                Main.spriteBatch.Draw(texture.Value, pos, frame, color, angle, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            DrawUtils.DrawYoyoString(proj, mountedCenter, (segmentCount, segmentIndex, position, rotation, height, color) =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "FishingLine_WithShadow", AssetRequestMode.ImmediateLoad);
                var pos = position - Main.screenPosition;
                var rect = new Rectangle(0, 0, texture.Width(), (int)height);
                var origin = new Vector2(texture.Width() * 0.5f, 0f);
                var colour = Color.Lerp(Color.Transparent, Lighting.GetColor(position.ToTileCoordinates(), new Color(90, 155, 60)), EasingFunctions.InQuart(segmentIndex / (float)segmentCount) * 5f);

                Main.spriteBatch.Draw(texture.Value, pos, rect, colour, rotation, origin, 1f, SpriteEffects.None, 0f);
            });
        }

        public static void DrawMask(Projectile proj)
        {
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_GrassMask", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;

            Main.spriteBatch.Draw(texture.Value, position, null, Color.White, 0f, texture.Size() * 0.5f, proj.localAI[1], SpriteEffects.None, 0f);
        }
    }

    public class AmazonDust : ModDust
    {
        private static readonly EasingBuilder ProgressEasing = new(
            (EasingFunctions.InOutCirc, 0.2f, 0f, 1f),
            (EasingFunctions.Linear, 0.6f, 1f, 1f),
            (EasingFunctions.InOutCirc, 0.2f, 1f, 0f)
        );

        public override string Texture => ModAssets.DustsPath + "Amazon";

        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.frame = new Rectangle(18 * Main.rand.Next(3), 0, 18, 18);
            dust.color = Color.White;
            dust.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            dust.customData = (Main.rand.NextBool() ? SpriteEffects.FlipVertically : SpriteEffects.None) | (Main.rand.NextBool() ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }

        public override bool Update(Dust dust)
        {
            dust.velocity.Y += 0.055f;
            dust.scale += 0.025f;
            return true;
        }

        public override bool PreDraw(Dust dust)
        {
            var position = dust.position - Main.screenPosition;
            var color = Lighting.GetColor(dust.position.ToTileCoordinates(), dust.color);
            var scale = ProgressEasing.Evaluate(dust.scale) * 0.9f;
            var effect = dust.customData is SpriteEffects spriteEffects ? spriteEffects : SpriteEffects.None;

            Main.spriteBatch.Draw(Texture2D.Value, position, dust.frame, color, dust.velocity.ToRotation(), new Vector2(8), scale, effect, 0);
            return false;
        }
    }

    public class AmazonGlobalNPC : GlobalNPC
    {
        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!npc.poisoned) return;

            foreach (var proj in Main.projectile.Where(p => p.active && p.type == ProjectileID.JungleYoyo))
            {
                if (Vector2.Distance(proj.Center, npc.Center) > (16f * 4f * proj.localAI[1])) continue;

                // Default: -12;
                // New: -36;

                npc.lifeRegen -= 24;
                break;
            }
        }
    }

    public class AmazonEffectHandler : ILoadable
    {
        public const int RadiusFromProjCenter = 7;

        public ProjectileObserver ProjectileObserver { get; private set; }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ProjectileObserver = new(p => p.type != ProjectileID.JungleYoyo);

            ModEvents.OnPostUpdateEverything += ProjectileObserver.Update;
            ModEvents.OnWorldUnload += ProjectileObserver.Clear;

            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);
                DrawBehindTiles();
            };

            ModEvents.OnPostDrawTiles += DrawOverTiles;
        }

        void ILoadable.Unload() { }

        public IReadOnlyList<Point> GetTilePoints()
        {
            var tilesInAreasHashSet = new HashSet<Point>();

            foreach (var proj in ProjectileObserver.GetEntityInstances())
            {
                var projCenter = proj.Center.ToTileCoordinates();
                var trueRadius = (int)MathF.Ceiling(proj.localAI[1] * RadiusFromProjCenter);
                var xStart = projCenter.X - trueRadius;
                var xEnd = projCenter.X + trueRadius;

                for (var x = xStart; x <= xEnd; x++)
                {
                    var yStart = projCenter.Y - trueRadius;
                    var yEnd = projCenter.Y + trueRadius;

                    for (var y = yStart; y <= yEnd; y++)
                    {
                        var distance = (int)MathF.Ceiling((float)Math.Sqrt(Math.Pow(x - projCenter.X, 2) + Math.Pow(y - projCenter.Y, 2)));

                        if (distance > trueRadius) continue;

                        var yLocalStart = y;
                        var yLocalEnd = yEnd - (y - yStart);

                        for (var i = yLocalStart; i <= yLocalEnd; i++)
                        {
                            if (!WorldGen.InWorld(x, i)) continue;

                            tilesInAreasHashSet.Add(new Point(x, i));
                        }

                        break;
                    }
                }
            }

            return tilesInAreasHashSet.ToList();
        }

        public void DrawBehindTiles()
        {
            Main.spriteBatch.End(out var spriteBatchSnapshot);
            DrawGrassTarget<AmazonWallsRenderTargetContent>();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }

        public void DrawOverTiles()
        {
            DrawGrassTarget<AmazonTilesRenderTargetContent>();
        }

        public void DrawGrassTarget<T>() where T : RenderTargetContent
        {
            var grassRTContent = ModContent.GetInstance<T>();

            if (!grassRTContent.IsRenderedInThisFrame || !grassRTContent.TryGetRenderTarget(out var grassTarget)) return;

            var maskRTContent = ModContent.GetInstance<AmazonMaskRenderTargetContent>();

            if (!maskRTContent.IsRenderedInThisFrame || !maskRTContent.TryGetRenderTarget(out var maskTarget)) return;

            var effectAsset = ModContent.Request<Effect>(ModAssets.EffectsPath + "AmazonEffect", AssetRequestMode.ImmediateLoad);
            var effect = effectAsset.Value;
            var effectParameters = effect.Parameters;

            effectParameters["Texture1"].SetValue(maskTarget);
            effectParameters["ScreenSize"].SetValue(maskTarget.Size());
            effectParameters["Zoom"].SetValue(new Vector2(Main.GameZoomTarget));

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect, Matrix.Identity);
            Main.spriteBatch.Draw(grassTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }
    }

    public class AmazonWallsRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth, Main.screenHeight);

        public override bool PreRender()
        {
            return ModContent.GetInstance<AmazonEffectHandler>()?.ProjectileObserver.AnyEntity ?? false;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_GrassWall", AssetRequestMode.ImmediateLoad);

            foreach (var tilePos in ModContent.GetInstance<AmazonEffectHandler>().GetTilePoints())
            {
                var tile = Main.tile[tilePos.X, tilePos.Y];

                if (tile.TileType <= 0
                    || !WorldGen.InWorld(tilePos.X, tilePos.Y - 1)
                    || WorldGen.SolidTile(tilePos.X, tilePos.Y - 1)
                    || !WorldGen.SolidOrSlopedTile(tile) && !TileID.Sets.Platforms[tile.TileType] && !tile.IsHalfBlock) continue;

                void DrawWall(int wallPosX, int wallPosY)
                {
                    var position = new Vector2(wallPosX, wallPosY).ToWorldCoordinates(0, 0) - Main.screenPosition - Vector2.One * 2;

                    Main.spriteBatch.Draw(texture.Value, position, null, Lighting.GetColor(wallPosX, wallPosY, Color.White));
                }

                DrawWall(tilePos.X, tilePos.Y);

                var randValue = tilePos.X ^ tilePos.Y;

                if (randValue % 5 == 0
                    || Main.tile[tilePos.X, tilePos.Y - 1].TileType == TileID.TargetDummy) continue;

                DrawWall(tilePos.X, tilePos.Y - 1);

                if (randValue % 7 != 0) continue;

                DrawWall(tilePos.X, tilePos.Y - 2);
            }

            Main.spriteBatch.End();
        }
    }

    public class AmazonTilesRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth, Main.screenHeight);

        public override bool PreRender()
        {
            return ModContent.GetInstance<AmazonEffectHandler>()?.ProjectileObserver.AnyEntity ?? false;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_GrassTile", AssetRequestMode.ImmediateLoad);

            foreach (var tilePos in ModContent.GetInstance<AmazonEffectHandler>().GetTilePoints())
            {
                var tile = Main.tile[tilePos.X, tilePos.Y];

                if (!tile.HasTile
                    || !WorldGen.SolidOrSlopedTile(tilePos.X, tilePos.Y) && !TileID.Sets.Platforms[tile.TileType]
                    || WorldGen.SolidOrSlopedTile(tilePos.X, tilePos.Y - 1)) continue;

                var position = tilePos.ToWorldCoordinates(0, 0) - Main.screenPosition;
                var frame = new Rectangle(0, 0, 16, 16);

                if (tile.IsHalfBlock) position.Y += 8;
                else if (tile.Slope == SlopeType.SlopeDownRight) frame.X += 16;
                else if (tile.Slope == SlopeType.SlopeDownLeft) frame.X += 32;

                Main.spriteBatch.Draw(texture.Value, position, frame, Lighting.GetColor(tilePos.X, tilePos.Y, Color.White));
            }

            Main.spriteBatch.End();
        }
    }

    public class AmazonMaskRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth, Main.screenHeight);

        public override bool PreRender()
        {
            return ModContent.GetInstance<AmazonEffectHandler>()?.ProjectileObserver.AnyEntity ?? false;
        }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var proj in ModContent.GetInstance<AmazonEffectHandler>().ProjectileObserver.GetEntityInstances())
            {
                AmazonProjectile.DrawMask(proj);
            }

            Main.spriteBatch.End();
        }
    }
}