using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Core.Graphics.Renderers;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    [Autoload(Side = ModSide.Client)]
    public sealed class Code1Assets : ILoadable
    {
        public const string StringPath = $"{_assetPath}FishingLine_WithShadow";

        public static Asset<Texture2D> GlowTexture { get; private set; } = ModContent.Request<Texture2D>($"{_assetPath}YoyoGlow_WithShadow");

        private const string _assetPath = $"{nameof(SPYoyoMod)}/Assets/";
        private const string _yoyoPath = $"{_assetPath}Items/Vanilla.Yoyos/Code1/";

        void ILoadable.Unload()
        {
            GlowTexture = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class Code1Item : VanillaYoyoBaseItem
    {
        public const int CritBonus = 16;

        public override int ItemType => ItemID.Code1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritBonus);

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            base.ModifyTooltips(item, tooltips); //< Не удалять!

            var critLine = tooltips.Find(VanillaTooltipLine.CritChance);

            if (critLine is null)
                return;

            ItemUtils.ModifyFirstIntegerInLine(critLine, static (crit) =>
            {
                return crit + (Main.LocalPlayer.GetModPlayer<Code1Player>().CheckBuffState() ? CritBonus : 0);
            });
        }
    }

    public sealed class Code1Projectile : VanillaYoyoBaseProjectile, IInitializableProjectile, IEmitLightEntity
    {
        public sealed class StringSegmentDrawer : IDrawYoyoStringSegments
        {
            public Texture2D Texture { get; init; } = TextureAssets.FishingLine.Value;

            public void Draw(SpriteBatch spriteBatch, in YoyoStringRendererSettings settings, IReadOnlyList<YoyoStringSegment> segments)
            {
                ref float glowMult = ref settings.Projectile.localAI[1];

                var origin = new Vector2(Texture.Width * 0.5f, 0f);
                var glowColor = GlowColor * EasingFunctions.InOutQuint(glowMult);

                foreach (var segment in segments)
                {
                    var rectangle = new Rectangle(0, 0, Texture.Width, (int)segment.Length);
                    var color = Color.Lerp(Color.Transparent, glowColor, segment.Index / (float)segments.Count);

                    spriteBatch.Draw(Texture, segment.Position + settings.Offset, rectangle, color, segment.Rotation, origin, 1f, SpriteEffects.None, 0f);
                }
            }
        }

        public static readonly Color GlowColor = new(65, 185, 255);
        public static readonly float SearchNPCsRadius = TileUtils.TileSizeInPixels * 15f;

        private int _initCritChance;
        private YoyoStringRenderer _stringRenderer;

        public override int ProjType { get => ProjectileID.Code1; }
        public override bool InstancePerEntity { get => true; }

        void IInitializableProjectile.Initialize(Projectile proj)
        {
            _initCritChance = proj.CritChance;

            ref float glowMult = ref proj.localAI[1];
            glowMult = 0;

            if (Main.netMode == NetmodeID.Server)
                return;

            _stringRenderer = new YoyoStringRenderer(new StringSegmentDrawer());
        }

        public override void AI(Projectile proj)
        {
            var isBuffActive = proj.GetOwner().GetModPlayer<Code1Player>().IsBuffActive;

            // Визуал
            ref float glowMult = ref (proj as Projectile).localAI[1];
            glowMult = MathHelper.Clamp(glowMult + (isBuffActive ? 0.05f : -0.05f), 0f, 1f);

            // Свойства
            proj.CritChance = _initCritChance + (isBuffActive ? Code1Item.CritBonus : 0);
        }

        void IEmitLightEntity.EmitLight(Entity proj)
        {
            ref float glowMult = ref (proj as Projectile).localAI[1];

            Lighting.AddLight(proj.Center, GlowColor.ToVector3() * 0.15f * glowMult);
        }

        public override bool PreDraw(Projectile proj, ref Color lightColor)
        {
            ref float glowMult = ref proj.localAI[1];

            var glowPosition = proj.Center + proj.gfxOffY * Vector2.UnitY - Main.screenPosition;
            var glowTexture = Code1Assets.GlowTexture.Value;
            var glowColor = GlowColor * EasingFunctions.InOutQuint(glowMult);
            var glowOrigin = glowTexture.Size() * 0.5f;
            var glowScale = proj.scale * 1.2f;

            Main.spriteBatch.Draw(glowTexture, glowPosition, null, glowColor, proj.rotation, glowOrigin, glowScale, SpriteEffects.None, 0f);

            return true;
        }

        public override void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            if (_stringRenderer is null)
                return;

            var settings = new YoyoStringRendererSettings(
                proj: proj,
                start: mountedCenter + proj.GetOwner()?.gfxOffY * Vector2.UnitY ?? Vector2.Zero,
                offset: -Main.screenPosition
            );

            _stringRenderer.Render(Main.spriteBatch, settings);
        }
    }

    public sealed class Code1Player : ModPlayer
    {
        public static readonly float ScanNPCsRadius = TileUtils.TileSizeInPixels * 23f;

        public bool IsBuffActive { get; private set; }

        public override void PreUpdate()
        {
            if (Player.ownedProjectileCounts[ProjectileID.Code1] == 0)
                return;

            IsBuffActive = CheckBuffState();
        }

        public bool CheckBuffState()
        {
            var nearbyNPCs = new List<NPC>();

            foreach (var npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Player, false))
                    continue;

                if (Vector2.Distance(npc.Center, Player.Center) > ScanNPCsRadius)
                    continue;

                nearbyNPCs.Add(npc);
            }

            return nearbyNPCs.Count > 2;
        }
    }
}