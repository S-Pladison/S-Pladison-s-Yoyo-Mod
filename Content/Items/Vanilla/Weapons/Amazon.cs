using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics.RenderTargets;
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
                ModContent.GetInstance<AmazonEffectHandler>()?.AddProjectile(proj);
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

            // Aura

            foreach (var npc in Main.npc.Where(x => x.active))
            {
                if (Vector2.Distance(proj.Center, npc.Center) > (16f * 4f * proj.localAI[1])) continue;
                if (!npc.TryGetGlobalNPC(out AmazonGlobalNPC amazonGlobalNPC)) continue;

                amazonGlobalNPC.NearAmazonYoyo = true;
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
            DrawUtils.DrawGradientYoyoStringWithShadow(proj, mountedCenter, (Color.Transparent, true), (new Color(90, 155, 60), false));
        }

        public static void DrawMask(Projectile proj)
        {
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_GrassMask", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;

            Main.spriteBatch.Draw(texture.Value, position, null, Color.White, 0f, texture.Size() * 0.5f, proj.localAI[1], SpriteEffects.None, 0f);
        }
    }

    public class AmazonGlobalNPC : GlobalNPC
    {
        public bool NearAmazonYoyo { get; set; }
        public override bool InstancePerEntity { get => true; }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            if (!NearAmazonYoyo) return;

            NearAmazonYoyo = false;

            if (!npc.poisoned) return;

            // Default: -12;
            // New: -36;
            npc.lifeRegen -= 24;

            if (damage < 3)
            {
                damage = 3;
            }
        }
    }

    public class AmazonDust : ModDust
    {
        private static readonly EasingBuilder progressEasing = new(
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
            var scale = progressEasing.Evaluate(dust.scale) * 0.9f;
            var effect = dust.customData is SpriteEffects spriteEffects ? spriteEffects : SpriteEffects.None;

            Main.spriteBatch.Draw(Texture2D.Value, position, dust.frame, color, dust.velocity.ToRotation(), new Vector2(8), scale, effect, 0);
            return false;
        }
    }

    [Autoload(Side = ModSide.Client)]
    public class AmazonEffectHandler : ILoadable
    {
        [Autoload(false)]
        private class AmazonRenderTargetContent : RenderTargetContent
        {
            private readonly string name;
            private readonly Action drawToTargetAction;

            public AmazonRenderTargetContent(string name, Action drawToTargetAction)
            {
                this.name = name;
                this.drawToTargetAction = drawToTargetAction;
            }

            public override string Name { get => $"Amazon{name}RenderTargetContent"; }
            public override Point Size => Main.ScreenSize;

            public override bool PreRender()
            {
                return ModContent.GetInstance<AmazonEffectHandler>().IsActive;
            }

            public override void DrawToTarget()
            {
                drawToTargetAction();
            }
        }

        public const int RadiusFromProjCenter = 7;

        private AmazonRenderTargetContent wallRTContent;
        private AmazonRenderTargetContent tileRTContent;
        private AmazonRenderTargetContent maskRTContent;
        private ProjectileObserver projectileObserver;

        public bool IsActive => projectileObserver.AnyEntity;

        public void AddProjectile(Projectile proj)
        {
            projectileObserver.Add(proj);
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            mod.AddContent(wallRTContent = new AmazonRenderTargetContent("Wall", DrawWallTarget));
            mod.AddContent(tileRTContent = new AmazonRenderTargetContent("Tile", DrawTileTarget));
            mod.AddContent(maskRTContent = new AmazonRenderTargetContent("Mask", DrawMaskTarget));

            projectileObserver = new(p => p.type != ProjectileID.JungleYoyo);

            ModEvents.OnPostUpdateEverything += projectileObserver.Update;
            ModEvents.OnWorldUnload += projectileObserver.Clear;

            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);
                DrawBehindTiles();
            };

            ModEvents.OnPostDrawTiles += DrawOverTiles;
        }

        void ILoadable.Unload() { }

        private IReadOnlyList<Point> GetTilePoints()
        {
            var tilesInAreasHashSet = new HashSet<Point>();

            foreach (var proj in projectileObserver.GetEntityInstances())
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

        private void DrawBehindTiles()
        {
            Main.spriteBatch.End(out var spriteBatchSnapshot);
            DrawGrass(wallRTContent);
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }

        private void DrawOverTiles()
        {
            DrawGrass(tileRTContent);
        }

        private void DrawGrass(AmazonRenderTargetContent grassRTContent)
        {
            if (!grassRTContent.WasRenderedInThisFrame || !grassRTContent.TryGetRenderTarget(out var grassTarget)) return;
            if (!maskRTContent.WasRenderedInThisFrame || !maskRTContent.TryGetRenderTarget(out var maskTarget)) return;

            var effect = ModAssets.RequestEffect("AmazonEffect").Prepare(parameters =>
            {
                parameters["Texture1"].SetValue(maskTarget);
                parameters["ScreenSize"].SetValue(maskTarget.Size());
                parameters["Zoom"].SetValue(new Vector2(Main.GameZoomTarget));
            });

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Matrix.Identity);
            Main.spriteBatch.Draw(grassTarget, Vector2.Zero, Color.White);
            Main.spriteBatch.End();
        }

        private void DrawWallTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_GrassWall", AssetRequestMode.ImmediateLoad);

            foreach (var tilePos in GetTilePoints())
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

        private void DrawTileTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Amazon_GrassTile", AssetRequestMode.ImmediateLoad);

            foreach (var tilePos in GetTilePoints())
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

        private void DrawMaskTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var proj in projectileObserver.GetEntityInstances())
            {
                AmazonProjectile.DrawMask(proj);
            }

            Main.spriteBatch.End();
        }
    }
}