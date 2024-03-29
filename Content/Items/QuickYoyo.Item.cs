﻿using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class YoyoItem : ModItem
    {
        /// <summary>
        /// Determines the extra range (in tile coordinates) that an item of the given item
        /// type can be used in when using a controller.
        /// </summary>
        public abstract int GamepadExtraRange { get; }

        public sealed override void SetStaticDefaults()
        {
            ItemID.Sets.Yoyo[Type] = true;
            ItemID.Sets.GamepadExtraRange[Type] = GamepadExtraRange;
            ItemID.Sets.GamepadSmartQuickReach[Type] = true;

            YoyoSetStaticDefaults();
        }

        public sealed override void SetDefaults()
        {
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.width = 30;
            Item.height = 26;
            Item.shootSpeed = 16f;

            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 25;
            Item.useTime = 25;

            Item.channel = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            YoyoSetDefaults();
        }

        /// <inheritdoc cref="SetStaticDefaults" />
        public virtual void YoyoSetStaticDefaults() { }

        /// <inheritdoc cref="SetDefaults" />
        public virtual void YoyoSetDefaults() { }
    }
}