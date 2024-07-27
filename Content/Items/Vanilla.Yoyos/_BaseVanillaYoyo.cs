using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Hooks;
using SPYoyoMod.Utils;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Yoyos
{
    /// <summary>
    /// Базовый класс ванильного предмета йо-йо.
    /// </summary>
    public abstract class VanillaYoyoBaseItem : GlobalItem, ILocalizedModType
    {
        /// <summary>
        /// Уникальный ID предмета йо-йо, который будет реворкнут.
        /// </summary>
        public abstract int ItemType { get; }

        /// <summary>
        /// Определяет диапазон (в координатах плитки), в котором может использоваться
        /// данный предмет при использовании контроллера.
        /// </summary>
        public virtual int? GamepadExtraRange { get; }

        /// <summary>
        /// Категория локализации для данного предмета... Ну, лучше не трогать лишний раз :p
        /// <br/>Пусть вся локализация ванильных предметов будет в одном файлике :/
        /// </summary>
        public virtual string LocalizationCategory { get => "VanillaItems"; }

        /// <summary>
        /// Текст всплывающей подсказки данного предмета. Не заменяет ванильное описание, а лишь добавлять новые строки ниже.
        /// </summary>
        public virtual LocalizedText Tooltip { get => this.GetLocalization("Tooltip", () => ""); }

        public sealed override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            if (!lateInstantiation)
                return false;

            if (ItemType >= ItemID.Count)
                throw new Exception($"'{nameof(VanillaYoyoBaseItem)}.{nameof(ItemType)}' value is not a vanilla type");

            return item.type == ItemType;
        }

        public override void SetStaticDefaults()
        {
            _ = Tooltip;

            if (GamepadExtraRange.HasValue)
                ItemID.Sets.GamepadExtraRange[ItemType] = GamepadExtraRange.Value;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (Tooltip.Value is null || Tooltip.Value == "")
                return;

            var tooltipLine = new TooltipLine(Mod, "ModTooltip", Tooltip.Value);
            var tooltipLines = tooltipLine.Split('\n');

            tooltips.InsertDescription(tooltipLines);
        }
    }

    /// <summary>
    /// Базовый класс ванильного снаряда йо-йо.
    /// </summary>
    public abstract class VanillaYoyoBaseProjectile : GlobalProjectile, IPostDrawYoyoStringProjectile
    {
        /// <summary>
        /// Уникальный ID снаряда йо-йо, который будет реворкнут.
        /// </summary>
        public abstract int ProjType { get; }

        /// <summary>
        /// Как долго в секундах йо-йо будет 'оставаться в бою', прежде чем автоматически вернуться к игроку.<br/>
        /// Если установить значение в -1, время станет бесконечным.
        /// </summary>
        public virtual float? LifeTime { get; }

        /// <summary>
        /// Максимальное расстояние, на котором йо-йо может находиться от своего владельца в пикселях.
        /// </summary>
        public virtual float? MaxRange { get; }

        /// <summary>
        /// Максимальная скорость, с которой может двигаться йо-йо, в пикселях за тик.
        /// </summary>
        public virtual float? TopSpeed { get; }

        public sealed override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
        {
            if (!lateInstantiation)
                return false;

            if (ProjType >= ProjectileID.Count)
                throw new Exception($"'{nameof(VanillaYoyoBaseProjectile)}.{nameof(ProjType)}' value is not a vanilla type");

            return proj.type == ProjType;
        }

        public override void SetStaticDefaults()
        {
            if (LifeTime.HasValue)
                ProjectileID.Sets.YoyosLifeTimeMultiplier[ProjType] = LifeTime.Value;

            if (MaxRange.HasValue)
                 ProjectileID.Sets.YoyosMaximumRange[ProjType] = MaxRange.Value;

            if (TopSpeed.HasValue)
                ProjectileID.Sets.YoyosTopSpeed[ProjType] = TopSpeed.Value;
        }

        public override void SetDefaults(Projectile proj)
        {
            // - Почему?
            // Ну, по неизвестной мне причине, все снаряды йо-йо отрисовываются 2 раза...
            // 1 - При отрисовке самого снаряда
            // 2 - При отрисовки игрока (heldProj или тип того)
            // proj.hide = true в свою очередь убирает 1-ую отрисовку.
            // - Почему не на всех снарядах йо-йо?
            // Не хочу портить внешний вид йо-йо из других модов...
            // Они явно отрисованы так как им нужно.
            // Да, сама нить йо-йо менее прозрачна, но я не думаю что это критично.
            proj.hide = true;
        }

        /// <inheritdoc cref="IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile, Vector2)" />
        public virtual void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter) { }
    }
}