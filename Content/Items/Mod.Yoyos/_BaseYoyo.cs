using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Hooks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Yoyos
{
    /// <summary>
    /// Базовый класс предмета йо-йо. Пример использования предоставлен в Items.Mod.Yoyos.<see cref="ExampleYoyoItem"/>.
    /// </summary>
    public abstract class YoyoBaseItem : ModItem
    {
        /// <summary>
        /// Определяет диапазон (в координатах плитки), в котором может использоваться
        /// данный предмет при использовании контроллера.
        /// </summary>
        public abstract int GamepadExtraRange { get; }

        public override void SetStaticDefaults()
        {
            ItemID.Sets.Yoyo[Type] = true;
            ItemID.Sets.GamepadExtraRange[Type] = GamepadExtraRange;
            ItemID.Sets.GamepadSmartQuickReach[Type] = true;
        }

        public override void SetDefaults()
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
        }
    }

    /// <summary>
    /// Базовый класс снаряда йо-йо. Пример использования предоставлен в Items.Yoyos.<see cref="ExampleYoyoProjectile"/>.
    /// </summary>
    public abstract class YoyoBaseProjectile : ModProjectile, IModifyYoyoStatsProjectile, IPostDrawYoyoStringProjectile
    {
        /// <summary>
        /// Как долго в секундах йо-йо будет 'оставаться в бою', прежде чем автоматически вернуться к игроку.<br/>
        /// Если установить значение в -1, время станет бесконечным.
        /// </summary>
        public abstract float LifeTime { get; }

        /// <summary>
        /// Максимальное расстояние, на котором йо-йо может находиться от своего владельца в пикселях.
        /// </summary>
        public abstract float MaxRange { get; }

        /// <summary>
        /// Максимальная скорость, с которой может двигаться йо-йо, в пикселях за тик.
        /// </summary>
        public abstract float TopSpeed { get; }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = LifeTime;
            ProjectileID.Sets.YoyosMaximumRange[Type] = MaxRange;
            ProjectileID.Sets.YoyosTopSpeed[Type] = TopSpeed;
        }

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.aiStyle = ProjAIStyleID.Yoyo;
            Projectile.friendly = true;
            Projectile.penetrate = -1;

            Projectile.hide = true;
        }

        /// <inheritdoc cref="IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile, ref YoyoStatModifiers)" />
        public virtual void ModifyYoyoStats(ref YoyoStatModifiers statModifiers) { }

        void IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            ModifyYoyoStats(ref statModifiers);
        }

        /// <inheritdoc cref="IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile, Vector2)" />
        public virtual void PostDrawYoyoString(Vector2 mountedCenter) { }

        void IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            PostDrawYoyoString(mountedCenter);
        }
    }
}