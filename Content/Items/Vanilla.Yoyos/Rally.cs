using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Yoyos;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using SPYoyoMod.Utils.DataStructures;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class RallyAssets
    {
        public const string AssetPath = $"{nameof(SPYoyoMod)}/Assets";
        public const string YoyoPath = $"{AssetPath}/Items/Vanilla.Yoyos/Rally/Rally";

        public const string StringPath = $"{AssetPath}/FishingLine_WithShadow";

        public static readonly LazyAsset<Texture2D> GlowTexture = LazyAsset<Texture2D>.From($"{AssetPath}/YoyoGlow_WithShadow");
    }

    public sealed class RallyItem : YoyoItem<RallyProjectile>
    {
        public override int OverrideType => ItemID.Rally;

        //=/-

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs((int)((RallyProjectile.SoloDamageMultiplier - 1f) * 100f));
    }

    public sealed class RallyProjectile : YoyoProjectile<RallyItem>, IInitializableProjectile, IEmitLightEntity
    {
        public override int OverrideType => ProjectileID.Rally;

        //=/-

        public static readonly float SoloDamageMultiplier = 1.5f;
        public static readonly int CombatMemoryTime = GeneralUtils.SecondsToTicks(5f);
        public static readonly Color GlowColor = new(243, 252, 255);

        private YoyoStringRenderer _stringRenderer;
        private float _fadeProgress;
        private bool _isBonusActive;

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            if (Main.dedServ)
                return;

            _stringRenderer = new YoyoStringRenderer(new RallyString(
                ModContent.Request<Texture2D>(RallyAssets.StringPath, AssetRequestMode.ImmediateLoad).Value,
                this
            ));
        }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            _isBonusActive = proj.TryGetOwner(out var owner) && owner.GetModPlayer<RallyPlayer>().IsActive;
            _fadeProgress = _isBonusActive ? 1f : 0f;
        }

        public override void AI(Projectile proj)
        {
            if (proj.IsLocalPlayerAsOwner())
            {
                _isBonusActive = proj.TryGetOwner(out var owner) && owner.GetModPlayer<RallyPlayer>().IsActive;
                proj.netUpdate = true;
            }

            _fadeProgress = MathHelper.Clamp(_fadeProgress + (_isBonusActive ? 0.05f : -0.05f), 0f, 1f);
        }

        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!_isBonusActive) {
                return;
            }

            modifiers.SourceDamage *= SoloDamageMultiplier;
        }

        public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!proj.TryGetOwner(out var owner))
                return;

            var rallyPlayer = owner.GetModPlayer<RallyPlayer>();
            rallyPlayer.OnHit(target);
        }

        public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(_isBonusActive);
        }

        public override void ReceiveExtraAI(Projectile proj, BitReader bitReader, BinaryReader binaryReader)
        {
            _isBonusActive = bitReader.ReadBit();
        }

        void IEmitLightEntity.EmitLight(Entity entity)
        {
            if (_fadeProgress <= 0f)
                return;

            Lighting.AddLight(entity.Center, GetGlowColor().ToVector3() * 0.05f);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            if (_fadeProgress <= 0f)
                return true;

            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = RallyAssets.GlowTexture.Value;
            var glowOrigin = glowTexture.Size() * 0.5f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, GetGlowColor(), proj.rotation, glowOrigin, proj.scale * 1.2f, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (_fadeProgress <= 0f)
                return;

            _stringRenderer.Render(Main.spriteBatch, YoyoStringRendererContext.FromProjectile(proj, mountedCenter));
        }

        private Color GetGlowColor()
            => GlowColor * _fadeProgress;

        private sealed class RallyString(Texture2D texture, RallyProjectile yoyo) : IDrawYoyoStringSegments
        {
            public Texture2D Texture { get; } = texture;

            public void Draw(SpriteBatch spriteBatch, in YoyoStringRendererContext context, IReadOnlyList<YoyoStringSegment> segments)
            {
                var origin = new Vector2(Texture.Width * 0.5f, 0f);
                var segmentCount = segments.Count;
                var glowColor = yoyo.GetGlowColor();

                foreach (var segment in segments)
                {
                    var color = ColorUtils.MultipleLerp(segment.Index / (float)segmentCount, Color.Transparent, Color.Transparent, glowColor);
                    segment.Draw(spriteBatch, Texture, origin, context.Offset, color);
                }
            }
        }
    }

    public sealed class RallyPlayer : ModPlayer
    {
        private int _lastTargetType = -1;
        private int _lastTargetWhoAmI = -1;
        private int _hitMemoryTimer = 0;

        public bool IsActive { get; private set; } = true;

        public override void PostUpdate()
        {
            if (_hitMemoryTimer-- <= 0)
                return;

            // Проверяем, жива ли цель
            if (_lastTargetType >= 0 && !TryGetLastTarget(out var _))
            {
                ResetTarget();
                return;
            }

            // Забываем, если проходит слишком много времени с последнего удара
            if (_hitMemoryTimer <= 0)
            {
                ResetTarget();
                IsActive = true;
                return;
            }
        }

        public void OnHit(NPC target)
        {
            if (!IsActive)
                return;

            _hitMemoryTimer = RallyProjectile.CombatMemoryTime;

            if (TryGetLastTarget(out var lastTarget) && lastTarget.whoAmI == target.whoAmI && lastTarget.type == target.type)
                return;

            if (_lastTargetWhoAmI >= 0 && _lastTargetWhoAmI != target.whoAmI)
                IsActive = false;

            _lastTargetType = target.type;
            _lastTargetWhoAmI = target.whoAmI;
        }

        private void ResetTarget()
        {
            _lastTargetType = -1;
            _lastTargetWhoAmI = -1;
        }

        private bool TryGetLastTarget(out NPC target)
        {
            if (!Main.npc.IndexInRange(_lastTargetWhoAmI))
            {
                target = null;
                return false;
            }

            target = Main.npc[_lastTargetWhoAmI];

            if (target == null || !target.active || target.type != _lastTargetType)
            {
                target = null;
                return false;
            }

            return true;
        }
    }
}
