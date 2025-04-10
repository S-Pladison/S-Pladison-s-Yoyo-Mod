using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
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

    public sealed class BlackholeBackgroundHandler : ILoadable
    {
        private readonly ProjectileObserver _projObserver = ProjectileObserver.Create(p => p.ModProjectile is not BlackholeProjectile);

        private static float _effectStrength;

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
                _effectStrength = MathHelper.Clamp(_projObserver.AnyEntity ? (_effectStrength + 0.025f) : (_effectStrength - 0.025f), 0.0f, 1.0f);
            };

            // Шиммер (мерцание) отключает отрисовку этой фигни...
            On_Main.DrawBlack += (orig, self, force) =>
            {
                if (_effectStrength >= 1.0f)
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
                    MoveType.Before,
                    i => i.MatchLdsfld(typeof(Overlays).GetField(nameof(Overlays.Scene))),
                    i => i.MatchLdsfld(typeof(Main).GetField(nameof(Main.spriteBatch))),
                    i => i.MatchLdcI4(3),
                    i => i.MatchLdcI4(0)))
                {
                    ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(BlackholeBackgroundHandler)}..{nameof(IL_Main.DoDraw)}\" failed...");
                    return;
                }

                ILLabel label = null;

                // if (shimmerAlpha > 0f)

                // IL_1453: ldsfld float32 Terraria.Main::shimmerAlpha
                // IL_1458: ldc.r4 0.0
                // IL_145d: ble.un.s IL_14d6

                if (!c.TryGotoNext(
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
                c.EmitDelegate(static () =>
                {
                    if (_effectStrength <= 0)
                        return;

                    Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, Vector2.Zero, null, Color.Black * _effectStrength, 0f, Vector2.Zero, new Vector2(Main.Camera.UnscaledSize.X + Main.offScreenRange * 2, Main.Camera.UnscaledSize.Y + Main.offScreenRange * 2), SpriteEffects.None, 0f);
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
                    value = MathHelper.Max(0.0f, value - _effectStrength);
                });
            };
        }

        void ILoadable.Unload()
        {

        }
    }
}
