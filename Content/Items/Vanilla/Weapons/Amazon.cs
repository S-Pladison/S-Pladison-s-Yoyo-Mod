using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using static Humanizer.In;
using static tModPorter.ProgressUpdate;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class AmazonItem : VanillaYoyoItem
    {
        public AmazonItem() : base(ItemID.JungleYoyo) { }
    }

    public class AmazonProjectile : VanillaYoyoProjectile
    {
        private Vector2? startToReturnPosition;

        public AmazonProjectile() : base(ProjectileID.JungleYoyo) { }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            ModContent.GetInstance<AmazonEffectHandler>().AddProjectile(proj);

            proj.localAI[1] = 0f;
        }

        public override void AI(Projectile proj)
        {
            var isReturning = proj.ai[0] == -1;

            if (isReturning)
            {
                if (!startToReturnPosition.HasValue)
                {
                    startToReturnPosition = proj.Center;
                }

                var owner = Main.player[proj.owner];

                proj.localAI[1] = Vector2.DistanceSquared(owner.Center, proj.Center) / Vector2.DistanceSquared(owner.Center, startToReturnPosition.Value);
            }
            else
            {
                proj.localAI[1] += 0.2f;
            }

            proj.localAI[1] = MathHelper.Clamp(proj.localAI[1], 0f, 1f);
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 60 * 5);

            var vector = Vector2.Normalize(proj.Center - target.Center);
            var scaleFactor = -6f;
            proj.velocity += vector * scaleFactor;

            // Vanilla: 16f
            // New: 10f

            proj.netUpdate = true;
        }

        public override void PostDraw(Projectile proj, Color lightColor)
        {
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amazon_Ring", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var segmentCount = (int)MathF.Ceiling(15 * proj.localAI[1]);

            for (int i = 0; i < segmentCount; i++)
            {
                var origin = new Vector2(10, 15);
                var frame = new Rectangle(((i * 255) ^ proj.whoAmI) % 3 * 20, 0, 20, 30);
                var angle = i * MathHelper.TwoPi / segmentCount + (float)Main.timeForVisualEffects * 0.025f;
                var pos = position + Vector2.UnitX.RotatedBy(angle) * 63f * proj.localAI[1];
                var color = Lighting.GetColor((pos + Main.screenPosition).ToTileCoordinates(), Color.White);

                Main.spriteBatch.Draw(texture.Value, pos, frame, color, angle, origin, 1f, SpriteEffects.None, 0f);
            }
        }

        public static void DrawMask(Projectile proj)
        {
            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amazon_Mask", AssetRequestMode.ImmediateLoad);
            var position = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;

            Main.spriteBatch.Draw(texture.Value, position, null, Color.White, 0f, texture.Size() * 0.5f, proj.localAI[1], SpriteEffects.None, 0f);
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

        public int ProjCount { get => projectiles.Count; }
        public IReadOnlyList<int> Projectiles { get => projectiles; }

        private List<int> projectiles;

        public void AddProjectile(Projectile proj)
        {
            if (proj.type != ProjectileID.JungleYoyo) return;

            projectiles.Add(proj.whoAmI);
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            projectiles = new();

            On_Main.DoDraw_Tiles_NonSolid += (orig, main) =>
            {
                orig(main);
                DrawEffect_BehindTiles();
            };

            ModEvents.OnPostUpdateEverything += ClearProjs;
            ModEvents.OnPostDrawTiles += DrawEffect_OverTiles;
        }

        void ILoadable.Unload() { }

        public void ClearProjs()
        {
            for (int i = 0; i < projectiles.Count; i++)
            {
                ref var proj = ref Main.projectile[projectiles[i]];

                if (proj is null || !proj.active || proj.type != ProjectileID.JungleYoyo)
                {
                    projectiles.RemoveAt(i);
                    i--;
                }
            }
        }

        public IReadOnlyList<Point> GetTilePoints()
        {
            var tilesInAreasHashSet = new HashSet<Point>();

            foreach (var projIndex in projectiles)
            {
                ref var proj = ref Main.projectile[projIndex];
                var projCenter = proj.Center.ToTileCoordinates();

                for (int x = projCenter.X - RadiusFromProjCenter; x <= projCenter.X + RadiusFromProjCenter; x++)
                {
                    for (int y = projCenter.Y - RadiusFromProjCenter; y <= projCenter.Y + RadiusFromProjCenter; y++)
                    {
                        int distance = (int)MathF.Ceiling((float)Math.Sqrt(Math.Pow(x - projCenter.X, 2) + Math.Pow(y - projCenter.Y, 2)));

                        if (distance > RadiusFromProjCenter || !WorldGen.InWorld(x, y)) continue;

                        tilesInAreasHashSet.Add(new Point(x, y));
                    }
                }
            }

            return tilesInAreasHashSet.ToList();
        }

        public void DrawEffect_DrawTargets<T>() where T : ScreenRenderTargetContent
        {
            var grassRTContent = ModContent.GetInstance<T>();

            if (!grassRTContent.IsRenderedInThisFrame || !grassRTContent.TryGetRenderTarget(out RenderTarget2D grassTarget)) return;

            var maskRTContent = ModContent.GetInstance<AmazonEffectMaskRenderTargetContent>();

            if (!maskRTContent.IsRenderedInThisFrame || !maskRTContent.TryGetRenderTarget(out RenderTarget2D maskTarget)) return;

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

        public void DrawEffect_BehindTiles()
        {
            Main.spriteBatch.End(out SpriteBatchSnapshot spriteBatchSnapshot);
            DrawEffect_DrawTargets<AmazonEffectGrassWallsRenderTargetContent>();
            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }

        public void DrawEffect_OverTiles()
        {
            DrawEffect_DrawTargets<AmazonEffectGrassTilesRenderTargetContent>();
        }
    }

    public class AmazonEffectGrassWallsRenderTargetContent : ScreenRenderTargetContent
    {
        public override bool PreRender() { return (ModContent.GetInstance<AmazonEffectHandler>()?.ProjCount ?? 0) > 0; }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amazon_GrassWall", AssetRequestMode.ImmediateLoad);

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

    public class AmazonEffectGrassTilesRenderTargetContent : ScreenRenderTargetContent
    {
        public override bool PreRender() { return (ModContent.GetInstance<AmazonEffectHandler>()?.ProjCount ?? 0) > 0; }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            var texture = ModContent.Request<Texture2D>(ModAssets.TexturesPath + "Effects/Amazon_Grass", AssetRequestMode.ImmediateLoad);

            foreach (var tilePos in ModContent.GetInstance<AmazonEffectHandler>().GetTilePoints())
            {
                var tile = Main.tile[tilePos.X, tilePos.Y];

                if (!tile.HasTile) continue;
                if (!WorldGen.SolidOrSlopedTile(tilePos.X, tilePos.Y) && !TileID.Sets.Platforms[tile.TileType]) continue;
                if (WorldGen.SolidOrSlopedTile(tilePos.X, tilePos.Y - 1)) continue;

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

    public class AmazonEffectMaskRenderTargetContent : ScreenRenderTargetContent
    {
        public override bool PreRender() { return (ModContent.GetInstance<AmazonEffectHandler>()?.ProjCount ?? 0) > 0; }

        public override void DrawToTarget()
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (var projIndex in ModContent.GetInstance<AmazonEffectHandler>().Projectiles)
            {
                ref var proj = ref Main.projectile[projIndex];

                AmazonProjectile.DrawMask(proj);
            }

            Main.spriteBatch.End();
        }
    }
}