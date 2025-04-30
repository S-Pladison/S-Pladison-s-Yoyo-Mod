using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    public sealed class BlackholeAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Mod.Yoyos/Blackhole/Blackhole";

        public const string ItemPath = $"{YoyoPath}_Item";
        public const string ProjPath = $"{YoyoPath}_Proj";

        public static readonly LazyAsset<Effect> BackgroundEffect = LazyAsset<Effect>.From($"{YoyoPath}Effect_Background", ReLogic.Content.AssetRequestMode.ImmediateLoad);
    }

    public sealed class BlackholeItem : YoyoBaseItem
    {
        public override string Texture => BlackholeAssets.ItemPath;
        public override int GamepadExtraRange => 15;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            ModSets.Items.InventoryScaleMultiplier[Type] = 1.3f;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();

            Item.width = 42;
            Item.height = 26;

            Item.damage = 90;
            Item.knockBack = 2f;
            Item.crit = 6;

            Item.shoot = ModContent.ProjectileType<BlackholeProjectile>();

            Item.rare = ItemRarityID.Yellow;
            Item.value = ItemUtils.SellPrice(platinum: 0, gold: 20, silver: 0, copper: 0);
        }
    }

    public sealed class BlackholeProjectile : YoyoBaseProjectile, IInitializableProjectile
    {
        public override string Texture => BlackholeAssets.ProjPath;
        public override float LifeTime => -1f;
        public override float MaxRange => 300f;
        public override float TopSpeed => 13f;

        void IInitializableProjectile.Initialize(Projectile _)
        {
            ModContent.GetInstance<BlackholeBackgroundHandler>()?.Add(Projectile);
        }

        public override void OnKill(int timeLeft)
        {
            ModContent.GetInstance<BlackholeBackgroundHandler>()?.Remove(Projectile);
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class BlackholeBackgroundHandler : ILoadable
    {
        private static readonly short[] QuadTriangles = { 0, 2, 3, 0, 1, 2 };

        /// <summary>
        /// Общая сила эффекта от 0 до 1, где промежуток от 0 до 0.5 - затемнение заднего фона/удаление глобального освещения, а 0.5 до 1 - яркость/прозрачность космоса
        /// </summary>
        private static float _effectStrength;

        /// <summary>
        /// Значение затемнения глобального освещения на поверхности.
        /// </summary>
        private static float _surfaceDarkStrength;

        /// <summary>
        /// Наблюдатель за снарядами черной дыры.
        /// </summary>
        private readonly ProjectileObserver _projObserver = ProjectileObserver.Create(p => p.ModProjectile is not BlackholeProjectile);

        public void Add(Projectile proj)
        {
            _projObserver.Add(proj);
        }

        public void Remove(Projectile proj)
        {
            _projObserver.Remove(proj);
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            // Просто определяем силу эффекта;
            // Если есть хоть один йо-йо, то эффект должен плавно включаться, а если нет - выключаться
            ModEvents.OnPostUpdateEverything += () =>
            {
                if (_projObserver.AnyEntity)
                {
                    _effectStrength = MathHelper.Min(_effectStrength + 0.0125f, 1.0f);

                    if (_effectStrength >= 0.25f)
                        _surfaceDarkStrength = MathHelper.Min(_surfaceDarkStrength + 0.05f, 1.0f);
                }
                else if (_effectStrength > 0.0f)
                {
                    _effectStrength = MathHelper.Max(_effectStrength - 0.0125f, 0.0f);

                    if (_effectStrength <= 0.75f)
                        _surfaceDarkStrength = MathHelper.Max(_surfaceDarkStrength - 0.05f, 0.0f);
                }
            };

            // Шиммер (мерцание) отключает отрисовку этой фигни...
            On_Main.DrawBlack += (orig, self, force) =>
            {
                if (_effectStrength >= 0.5f)
                    return;

                orig(self, force);
            };

            // Отрисовка заднего фона эффекта (прям как шиммер...)
            IL_Main.DoDraw += (il) =>
            {
                var c = new ILCursor(il);

                // Overlays.Scene.Draw(spriteBatch, RenderLayers.InWorldUI);

                // IL_14d6: ldsfld class Terraria.Graphics.Effects.OverlayManager Terraria.Graphics.Effects.Overlays::Scene
                // IL_14db: ldsfld class [FNA]Microsoft.Xna.Framework.Graphics.SpriteBatch Terraria.Main::spriteBatch
                // IL_14e0: ldc.i4.3
                // IL_14e1: ldc.i4.0
                // IL_14e2: callvirt instance void Terraria.Graphics.Effects.OverlayManager::Draw(class [FNA]Microsoft.Xna.Framework.Graphics.SpriteBatch, valuetype Terraria.Graphics.Effects.RenderLayers, bool)

                if (!c.TryGotoNext(
                    MoveType.After,
                    i => i.MatchLdsfld(typeof(Overlays).GetField(nameof(Overlays.Scene))),
                    i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.spriteBatch))),
                    i => i.MatchLdcI4(3),
                    i => i.MatchLdcI4(0),
                    i => i.MatchCallvirt(typeof(OverlayManager).GetMethod(nameof(OverlayManager.Draw)))))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(BlackholeBackgroundHandler)}..{nameof(IL_Main.DoDraw)}\" failed...");
                    return;
                }

                ILLabel label = null;

                // if (shimmerAlpha > 0f)

                // IL_1453: ldsfld float32 Terraria.Main::shimmerAlpha
                // IL_1458: ldc.r4 0.0
                // IL_145d: ble.un.s IL_14d6

                if (!c.TryGotoPrev(
                    MoveType.Before,
                    i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.shimmerAlpha))),
                    i => i.MatchLdcR4(0.0f),
                    i => i.MatchBleUn(out label)))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(BlackholeBackgroundHandler)}..{nameof(IL_Main.DoDraw)}\" failed...");
                    return;
                }

                c.GotoLabel(label, MoveType.Before);
                c.MarkLabel(label);
                c.EmitDelegate(DrawBackground);
            };

            // Нужно сделать фон прозрачнее, прям как с шиммером...
            IL_Main.DrawBackground += (il) =>
            {
                var c = new ILCursor(il);

                // float num = shimmerAlpha;

                // IL_0000: ldsfld float32 Terraria.Main::shimmerAlpha
                // IL_0005: stloc.0

                var numIndex = -1;

                if (!c.TryGotoNext(
                    MoveType.After,
                    i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.shimmerAlpha))),
                    i => i.MatchStloc(out numIndex)))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(BlackholeBackgroundHandler)}..{nameof(IL_Main.DrawBackground)}\" failed...");
                    return;
                }

                c.Emit(OpCodes.Ldloca, numIndex);
                c.EmitDelegate(static (ref float num) =>
                {
                    if (_effectStrength <= 0.0f)
                        return;

                    num = MathHelper.Min(MathHelper.Min(_effectStrength * 2.0f, 1.0f) + num, 1.0f);
                });
            };

            // Шиммер (мерцание) отключает глобальное освещение
            IL_TileLightScanner.ApplySurfaceLight += (il) =>
            {
                var c = new ILCursor(il);

                // float num11 = 1f - Main.shimmerDarken;

                // IL_040d: ldc.r4 1
                // IL_0412: ldsfld float32 Terraria.Main::shimmerDarken
                // IL_0417: sub
                // IL_0418: stloc.s 7 // num11

                var num11Index = -1;

                if (!c.TryGotoNext(
                    MoveType.After,
                    i => i.MatchLdcR4(1),
                    i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.shimmerDarken))),
                    i => i.MatchSub(),
                    i => i.MatchStloc(out num11Index)))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(BlackholeBackgroundHandler)}..{nameof(IL_TileLightScanner.ApplySurfaceLight)}\" failed...");
                    return;
                }

                c.Emit(OpCodes.Ldloca, num11Index);
                c.EmitDelegate(static (ref float value) =>
                {
                    if (_surfaceDarkStrength <= 0.0f)
                        return;

                    value = MathHelper.Max(0.0f, value - _surfaceDarkStrength);
                });
            };
        }

        void ILoadable.Unload()
        {
            // ...
        }

        private static void DrawBackground()
        {
            if (_effectStrength <= 0)
                return;

            var backgroundTexture = TextureAssets.MagicPixel.Value;
            var backgroundColor = Color.Black * MathHelper.Min(_effectStrength * 2.0f, 1.0f);
            var backgroundScale = new Vector2(Main.Camera.UnscaledSize.X + Main.offScreenRange * 2, Main.Camera.UnscaledSize.Y + Main.offScreenRange * 2);

            Main.spriteBatch.Draw(backgroundTexture, Vector2.Zero, null, backgroundColor, 0f, Vector2.Zero, backgroundScale, SpriteEffects.None, 0f);

            var backgroundRectangle = new Rectangle((int)(Main.sceneTilePos.X - Main.screenPosition.X), (int)(Main.sceneTilePos.Y - Main.screenPosition.Y), (int)backgroundScale.X, (int)backgroundScale.Y);
            var backgroundEffect = BlackholeAssets.BackgroundEffect.Prepare(parameters =>
            {
                parameters["TransformMatrix"].SetValue(Main.GameViewMatrix.NormalizedTransformationmatrix);
                parameters["Texture0"].SetValue(Main.instance.tileTarget);
                parameters["Texture0Offset"].SetValue(Vector2.Zero);
                parameters["BlurRadius"].SetValue(Vector2.One * 16 / backgroundScale);
                parameters["Transparency"].SetValue(MathHelper.Max(_effectStrength - 0.5f, 0.0f) * 2.0f);
            });

            Main.spriteBatch.End(out var spriteBatchSnapshot);

            foreach (var pass in backgroundEffect.Value.CurrentTechnique.Passes)
            {
                pass.Apply();

                var vertices = new[] {
                    new VertexPositionTexture(new Vector3(backgroundRectangle.Left, backgroundRectangle.Top, 0f), new Vector2(0f, 0f)),
                    new VertexPositionTexture(new Vector3(backgroundRectangle.Right, backgroundRectangle.Top, 0f), new Vector2(1f, 0f)),
                    new VertexPositionTexture(new Vector3(backgroundRectangle.Right, backgroundRectangle.Bottom, 0f), new Vector2(1f, 1f)),
                    new VertexPositionTexture(new Vector3(backgroundRectangle.Left, backgroundRectangle.Bottom, 0f), new Vector2(0f, 1f))
                };

                Main.graphics.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length, QuadTriangles, 0, QuadTriangles.Length / 3);
            }

            Main.spriteBatch.Begin(spriteBatchSnapshot);
        }
    }
}
