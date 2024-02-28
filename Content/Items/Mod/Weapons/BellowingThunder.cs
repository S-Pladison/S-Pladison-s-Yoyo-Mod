using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.PixelatedLayers;
using SPYoyoMod.Common.Renderers;
using SPYoyoMod.Common.RenderTargets;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.Rendering;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.CameraModifiers;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class BellowingThunderItem : YoyoItem
    {
        public const int StormCritBonus = 6;

        public override string Texture => ModAssets.ItemsPath + "BellowingThunder";
        public override int GamepadExtraRange => 15;

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;
            Item.crit = 6;

            Item.shoot = ModContent.ProjectileType<BellowingThunderProjectile>();

            Item.rare = ItemRarityID.Orange;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var critLine = tooltips.FirstOrDefault(x => x.Mod == "Terraria" && x.Name == "CritChance");

            if (critLine is null) return;

            var splitCritLine = critLine.Text.Split(' ');

            if (splitCritLine.Length == 0) return;

            if (int.TryParse(splitCritLine[0], out int crit))
            {
                crit += GetBonusValue();
                splitCritLine[0] = $"{crit}";
                critLine.Text = string.Join(' ', splitCritLine);
            }
            else if (splitCritLine[0].EndsWith("%") && int.TryParse(splitCritLine[0].Replace("%", ""), out crit))
            {
                crit += GetBonusValue();
                splitCritLine[0] = $"{crit}%";
                critLine.Text = string.Join(' ', splitCritLine);
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.CorruptYoyo)
                .AddIngredient(ItemID.Valor)
                .AddIngredient(ItemID.JungleYoyo)
                .AddIngredient(ItemID.Cascade)
                .AddTile(TileID.DemonAltar)
                .Register();

            CreateRecipe()
                .AddIngredient(ItemID.CrimsonYoyo)
                .AddIngredient(ItemID.Valor)
                .AddIngredient(ItemID.JungleYoyo)
                .AddIngredient(ItemID.Cascade)
                .AddTile(TileID.DemonAltar)
                .Register();
        }

        public static int GetBonusValue()
        {
            return Main.IsItStorming ? StormCritBonus : 0;
        }
    }

    public class BellowingThunderProjectile : YoyoProjectile
    {
        public const int HitsToActivateEffect = 5;

        public override string Texture => ModAssets.ProjectilesPath + "BellowingThunder";
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;

        private bool initialized;
        private int initCritChance;
        private TrailRenderer trailRenderer;
        private TrailRenderer shadowTrailRenderer;
        private int ringProjIndex;

        public override void OnKill(int timeLeft)
        {
            trailRenderer?.Dispose();
            shadowTrailRenderer?.Dispose();
        }

        public override void AI()
        {
            if (!initialized)
            {
                if (!Main.dedServ)
                {
                    trailRenderer = new TrailRenderer(10).SetWidth(f => MathHelper.Lerp(8f, 0f, f));
                    shadowTrailRenderer = new TrailRenderer(13).SetWidth(f => MathHelper.Lerp(10f, 0f, f));
                }

                initCritChance = Projectile.CritChance;
                ringProjIndex = -1;
                initialized = true;
            }

            if (ringProjIndex >= 0)
            {
                var ringProj = Main.projectile[ringProjIndex];

                if (ringProj is null || ringProj.type != ModContent.ProjectileType<BellowingThunderRingProjectile>() || !ringProj.active)
                {
                    ringProjIndex = -1;
                }
            }

            trailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);
            shadowTrailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity);

            Projectile.CritChance = initCritChance + BellowingThunderItem.GetBonusValue();

            Lighting.AddLight(Projectile.Center, new Color(208, 99, 219).ToVector3() * 0.25f);
        }

        public override void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!hit.Crit || ringProjIndex >= 0 || IsReturning) return;

            ringProjIndex = Projectile.NewProjectile(Projectile.GetSource_OnHit(target), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<BellowingThunderRingProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.identity);
        }

        public override void PostDrawYoyoString(Vector2 mountedCenter)
        {
            DrawUtils.DrawGradientYoyoStringWithShadow(Projectile, mountedCenter, (Color.Transparent, true), (new Color(208, 99, 219), true));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.UnderProjectiles, () =>
            {
                if (trailRenderer is null || shadowTrailRenderer is null) return;

                var effect = ModAssets.RequestEffect("DefaultStrip").Prepare(parameters =>
                {
                    parameters["Texture0"].SetValue(ModContent.Request<Texture2D>(ModAssets.MiscPath + "StripGradient_BlackToAlpha_PremultipliedAlpha", AssetRequestMode.ImmediateLoad).Value);
                    parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.TransformWithScreenOffset);
                });

                shadowTrailRenderer.Draw(effect.Prepare(parameters =>
                {
                    var colorVec4 = (Color.Black * 0.15f).ToVector4();

                    parameters["ColorTL"].SetValue(colorVec4);
                    parameters["ColorTR"].SetValue(colorVec4);
                    parameters["ColorBL"].SetValue(colorVec4);
                    parameters["ColorBR"].SetValue(colorVec4);
                }));

                trailRenderer.Draw(effect.Prepare(parameters =>
                {
                    var colorVec4 = new Color(208, 99, 219).ToVector4();

                    parameters["ColorTL"].SetValue(colorVec4);
                    parameters["ColorTR"].SetValue(colorVec4);
                    parameters["ColorBL"].SetValue(colorVec4);
                    parameters["ColorBR"].SetValue(colorVec4);
                }));
            });

            ModContent.GetInstance<PixelatedDrawLayers>().QueueDrawAction(PixelatedLayer.OverProjectiles, () =>
            {
                var timeForVisualEffects = (float)Main.timeForVisualEffects + Projectile.whoAmI * 111f;

                var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "BellowingThunder_Lightning", AssetRequestMode.ImmediateLoad);

                var frameIndex = (int)((timeForVisualEffects * 0.2f) % 16);
                var frame = new Rectangle(96 * (frameIndex / 4), 96 * (frameIndex % 4), 96, 96);
                var rotation = ((int)(timeForVisualEffects * 0.2f) / 16) * MathHelper.PiOver2;

                Main.spriteBatch.Draw(texture.Value, position, frame, Color.White with { A = 0 }, rotation, new Vector2(48, 48), 0.4f, SpriteEffects.None, 0f);
            });

            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "Yoyo_GlowWithShadow", AssetRequestMode.ImmediateLoad);

            Main.spriteBatch.Draw(texture.Value, position, null, new Color(208, 99, 219), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.2f, SpriteEffects.None, 0f);

            return true;
        }
    }

    public class BellowingThunderRingProjectile : ModProjectile
    {
        public const int MaxRadius = 16 * 3 + 8;
        public const int InitTimeLeft = 60 * 3;

        private static readonly EasingBuilder lineWidthEasing = new(
            (EasingFunctions.InOutCubic, 0.05f, 0f, 1f),
            (EasingFunctions.Linear, 0.05f, 1f, 1f),
            (EasingFunctions.InOutCubic, 0.05f, 1f, 0f),
            (EasingFunctions.Linear, 0.15f, 0f, 0f),
            (EasingFunctions.InOutCubic, 0.1f, 0f, 1f),
            (EasingFunctions.Linear, 0.5f, 1f, 1f),
            (EasingFunctions.InOutCubic, 0.1f, 1f, 0f)
        );

        private static readonly EasingBuilder starEasing = new(
            (EasingFunctions.InOutCubic, 0.2f, 0f, 1f),
            (EasingFunctions.Linear, 0.6f, 1f, 1f),
            (EasingFunctions.InOutCubic, 0.2f, 1f, 0f)
        );

        private static readonly EasingBuilder ringRadiusEasing = new(
            (EasingFunctions.OutBack, 0.05f, 0f, 1f),
            (EasingFunctions.Linear, 0.90f, 1f, 1f),
            (EasingFunctions.InOutCubic, 0.05f, 1f, 0f)
        );

        public override string Texture { get => ModAssets.MiscPath + "Invisible"; }
        public float TimeLeftProgress { get => 1f - Projectile.timeLeft / (float)InitTimeLeft; }

        private bool initialized;
        private int initCritChance;
        private LineRenderer lineRenderer;
        private RingRenderer ringRenderer;
        private int yoyoProjIndex;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = MaxRadius * 2;
            Projectile.height = MaxRadius * 2;

            Projectile.timeLeft = InitTimeLeft;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;

            Projectile.netImportant = true;
        }

        public override void OnKill(int timeLeft)
        {
            lineRenderer?.Dispose();
            ringRenderer?.Dispose();
        }

        public override void AI()
        {
            if (!initialized)
            {
                if (!Main.dedServ)
                {
                    ModContent.GetInstance<BellowingThunderRenderTargetContent>()?.AddProjectile(Projectile);

                    lineRenderer = new LineRenderer(0f, false);
                    ringRenderer = new RingRenderer(25, 16f * 5f, 0f);
                }

                initCritChance = Projectile.CritChance;
                yoyoProjIndex = Main.projectile.FirstOrDefault(p => p.identity == Projectile.ai[0] && p.type == ModContent.ProjectileType<BellowingThunderProjectile>())?.whoAmI ?? -1;
                initialized = true;
            }

            if (yoyoProjIndex < 0 || Main.projectile[yoyoProjIndex].type != ModContent.ProjectileType<BellowingThunderProjectile>() || !Main.projectile[yoyoProjIndex].active)
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.timeLeft == InitTimeLeft || Projectile.timeLeft == (InitTimeLeft - 55))
            {
                Main.instance.CameraModifiers.Add(new PunchCameraModifier(Projectile.Center, Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi)), 7f, 6f, 15, 16f * 25f));
                SoundEngine.PlaySound(new SoundStyle(ModAssets.SoundsPath + "Thunder", SoundType.Sound) { PitchVariance = 0.2f }, Projectile.Center);
            }

            Projectile.Center = Main.projectile[yoyoProjIndex].Center;
            Projectile.CritChance = initCritChance + BellowingThunderItem.GetBonusValue();

            Lighting.AddLight(Projectile.Center, new Color(208, 99, 219).ToVector3() * 0.4f * ringRadiusEasing.Evaluate(TimeLeftProgress));
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            var radius = MaxRadius * ringRadiusEasing.Evaluate(TimeLeftProgress);

            return CollisionUtils.CheckRectanglevCircle(targetHitbox, projHitbox.Center.ToVector2(), radius);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].Counterweight(target.Center, Projectile.damage, Projectile.knockBack);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "BellowingThunderRing_Circle", AssetRequestMode.ImmediateLoad);
            var color = new Color(145, 60, 195) with { A = 0 } * EasingFunctions.InOutCubic(TimeLeftProgress) * 0.2f;
            var scale = MathHelper.Lerp(2f, 0f, EasingFunctions.InCubic(TimeLeftProgress));

            Main.spriteBatch.Draw(texture.Value, position, null, color, 0f, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            return true;
        }

        public void DrawLightnings()
        {
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var effect = ModAssets.RequestEffect("BellowingThunderRingStrip").Prepare(parameters =>
            {
                var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "BellowingThunderRing_Lightning", AssetRequestMode.ImmediateLoad);

                parameters["Texture0"].SetValue(texture.Value);
                parameters["TransformMatrix"].SetValue(PrimitiveMatrices.PixelatedPrimitiveMatrices.Transform);
                parameters["Time"].SetValue(-(float)Main.timeForVisualEffects * 0.05f);

                parameters["UvRepeat"].SetValue(3f);
                parameters["Fade"].SetValue(false);
            });

            ringRenderer
                .SetPosition(position)
                .SetRadius(MaxRadius * 0.95f * ringRadiusEasing.Evaluate(TimeLeftProgress))
                .Draw(effect);

            var lineStartPosition = position - Vector2.UnitY * Main.screenHeight;
            var lineEndPosition = position;

            effect.Prepare(parameters =>
            {
                parameters["UvRepeat"].SetValue(2f);
                parameters["Fade"].SetValue(true);
            });

            lineRenderer
                .SetWidth(16f * 16f * lineWidthEasing.Evaluate(TimeLeftProgress))
                .SetPoints(new[] { lineStartPosition, lineEndPosition })
                .Draw(effect);

            var texture = ModContent.Request<Texture2D>(ModAssets.MiscPath + "BellowingThunderRing_Star", AssetRequestMode.ImmediateLoad);
            var rotation = EasingFunctions.InOutSine(TimeLeftProgress) * MathHelper.PiOver2;
            var scale = starEasing.Evaluate(TimeLeftProgress);

            Main.spriteBatch.Draw(texture.Value, position, null, Color.White, EasingFunctions.InOutSine(rotation) * MathHelper.PiOver2, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }

    public class BellowingThunderRenderTargetContent : RenderTargetContent
    {
        public override Point Size => new(Main.screenWidth / 2, Main.screenHeight / 2);

        private ProjectileObserver projectileObserver;

        public override void Load()
        {
            projectileObserver = new(p => p.ModProjectile is not BellowingThunderRingProjectile);

            ModEvents.OnPostUpdateEverything += projectileObserver.Update;
            ModEvents.OnWorldUnload += projectileObserver.Clear;

            On_Main.DrawPlayers_AfterProjectiles += (orig, main) =>
            {
                orig(main);
                DrawToScreen();
            };
        }

        public void AddProjectile(Projectile proj)
        {
            projectileObserver.Add(proj);
        }

        public override bool PreRender()
        {
            return projectileObserver.AnyEntity;
        }

        public override void DrawToTarget()
        {
            var spriteBatchSpanshot = new SpriteBatchSnapshot
            {
                SortMode = SpriteSortMode.Deferred,
                BlendState = BlendState.Additive,
                SamplerState = Main.DefaultSamplerState,
                DepthStencilState = DepthStencilState.None,
                RasterizerState = Main.Rasterizer,
                Effect = null,
                Matrix = Matrix.CreateScale(0.5f) * Main.GameViewMatrix.EffectMatrix
            };

            Main.graphics.GraphicsDevice.PrepRenderState(spriteBatchSpanshot);
            Main.spriteBatch.Begin(spriteBatchSpanshot);

            foreach (var proj in projectileObserver.GetEntityInstances())
            {
                (proj.ModProjectile as BellowingThunderRingProjectile).DrawLightnings();
            }

            Main.spriteBatch.End();
        }

        public void DrawToScreen()
        {
            if (!IsRenderedInThisFrame || !TryGetRenderTarget(out var target)) return;

            var effect = ModAssets.RequestEffect("BellowingThunderEffect").Prepare(parameters =>
            {
                parameters["ScreenSize"].SetValue(target.Size());
                parameters["Color"].SetValue(new Color(145, 60, 195).ToVector4());
            });

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, RasterizerState.CullNone, effect.Value, Main.GameViewMatrix.ZoomMatrix);
            Main.spriteBatch.Draw(target, Vector2.Zero, null, Color.White, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
        }
    }
}