using SPYoyoMod.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;

namespace SPYoyoMod.Common
{
    /// <summary>
    /// Информация о предмете в слоте экипировок игрока.
    /// </summary>
    public readonly struct EquipmentInfo
    {
        public static readonly EquipmentInfo None = default;

        /// <summary>
        /// Предмет в функциональном слоте (т.е. тот, что выдает эффекты игроку).
        /// </summary>
        public bool Functional { get; init; }

        /// <summary>
        /// Предмет в визуальном слоте (т.е. тот, что приоритетнее отображается).
        /// </summary>
        public bool Visual { get; init; }

        /// <summary>
        /// Отображается ли сейчас предмет.
        /// </summary>
        public bool Visible { get; init; }

        /// <summary>
        /// Идентификатор красителя для отображаемого на игроке предмета.
        /// </summary>
        public int Dye { get; init; }

        /// <summary>
        /// Сам предмет в слоте.
        /// </summary>
        public Item Item { get; init; }

        /// <summary>
        /// Надет ли предмет.
        /// </summary>
        public bool Equipped => Functional || Visual;

        /// <summary>
        /// Установлен ли какой-либо краситель.
        /// </summary>
        public bool HasDye => Dye > 0;
    }

    [LoadBefore, LoadAfter(typeof(ModEvents))]
    public sealed class EquipmentPlayer : ModPlayer
    {
        private readonly Dictionary<int, EquipmentInfo> _infos = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public EquipmentInfo Get<T>() where T : ModItem
            => Get(ModContent.ItemType<T>());

        public EquipmentInfo Get(int itemType)
            => _infos.TryGetValue(itemType, out var info) ? info : EquipmentInfo.None;

        public override void ResetEffects()
        {
            _infos.Clear();

            FindActiveInVanillaSlots();
            FindActiveInModSlots();
        }

        private void FindActiveInVanillaSlots()
        {
            var armorSlots = Player.armor;
            var dyeSlots = Player.dye;
            var vanityOffset = armorSlots.Length / 2;
            var hidden = Player.hideVisibleAccessory;

            for (var i = 0; i < Math.Min(dyeSlots.Length, vanityOffset); i++)
            {
                if (!Player.IsItemSlotUnlockedAndUsable(i))
                    continue;

                ApplyRow(armorSlots[i], armorSlots[i + vanityOffset], dyeSlots[i], i < hidden.Length && hidden[i]);
            }
        }

        private void FindActiveInModSlots()
        {
            var loader = LoaderManager.Get<AccessorySlotLoader>();
            var modSlotPlayer = Player.GetModPlayer<ModAccessorySlotPlayer>();
            var items = ModSlots.Items(modSlotPlayer);
            var hidden = ModSlots.Hidden(modSlotPlayer);
            var dyes = ModSlots.Dyes(modSlotPlayer);

            if (items is null || dyes is null || hidden is null)
                return;

            for (var i = 0; i < modSlotPlayer.SlotCount; i++)
            {
                var functionalEnabled = loader.ModdedIsSpecificItemSlotUnlockedAndUsable(i, Player, false);
                var vanityEnabled = loader.ModdedIsSpecificItemSlotUnlockedAndUsable(i, Player, true);

                if (!functionalEnabled && !vanityEnabled)
                    continue;

                ApplyRow(functionalEnabled ? items[i] : null, vanityEnabled ? items[i + modSlotPlayer.SlotCount] : null, dyes[i], functionalEnabled && hidden[i]);
            }
        }

        private void ApplyRow(Item functional, Item vanity, Item dyeItem, bool hidden)
        {
            var hasFunctional = functional is { IsAir: false };
            var hasVanity = vanity is { IsAir: false };
            var dye = dyeItem is { IsAir: false } ? dyeItem.dye : 0;

            if (hasFunctional)
                ApplyItem(functional, true, false, !hasVanity && !hidden, dye);

            if (hasVanity)
                ApplyItem(vanity, false, true, true, dye);
        }

        private void ApplyItem(Item item, bool functional, bool visual, bool visible, int dye)
        {
            var prevInfo = Get(item.type);

            _infos[item.type] = new EquipmentInfo
            {
                Functional = prevInfo.Functional || functional,
                Visual = prevInfo.Visual || visual,
                Visible = prevInfo.Visible || visible,
                Dye = visible || !prevInfo.Visible ? dye : prevInfo.Dye,
                Item = visible || !prevInfo.Visible ? item : prevInfo.Item
            };
        }

        private static class ModSlots
        {
            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "exAccessorySlot")]
            public static extern ref Item[] Items(ModAccessorySlotPlayer player);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "exDyesAccessory")]
            public static extern ref Item[] Dyes(ModAccessorySlotPlayer player);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "exHideAccessory")]
            public static extern ref bool[] Hidden(ModAccessorySlotPlayer player);
        }
    }
}
