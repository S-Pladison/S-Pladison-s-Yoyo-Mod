using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Common.Graphics;
using SPYoyoMod.Common.Graphics.Renderers;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Content.Particles;
using SPYoyoMod.Utils;
using System;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    public sealed class CascadeAssets : ILoadable
    {
        // [ Текстуры ]
        public const string InvisiblePath = $"{nameof(SPYoyoMod)}/Assets/Invisible";
        public const string StringPath = $"{nameof(SPYoyoMod)}/Assets/FishingLine_WithShadow";
        public static Asset<Texture2D> ExplosionRingTexture { get; private set; } = ModContent.Request<Texture2D>($"{_path}CascadeExplosion");

        // [ Эффекты ]
        public static Asset<Effect> ExplosionRingEffect { get; private set; } = ModContent.Request<Effect>($"{_path}CascadeExplosionShader");

        // [ Звуки ]
        public static readonly SoundStyle StartChargingSound = new($"{_path}CascadeSound_StartCharging");

        // [ Общее ]
        private const string _path = $"{nameof(SPYoyoMod)}/Assets/Items/Vanilla.Yoyos/Cascade/";

        void ILoadable.Unload()
        {
            ExplosionRingTexture = null;
            ExplosionRingEffect = null;
        }

        void ILoadable.Load(Terraria.ModLoader.Mod mod) { }
    }

    public sealed class CascadeItem : VanillaYoyoBaseItem
    {
        public override int ItemType => ItemID.Cascade;
    }

    public sealed class CascadeProjectile : VanillaYoyoBaseProjectile
    {
        public override int ProjType => ProjectileID.Cascade;
    }
}