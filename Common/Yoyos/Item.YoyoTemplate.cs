using SPYoyoMod.Core;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    /// <summary>
    /// Класс, представляющий собой обертку класса <see cref="GlobalItem"/>, позволяющий работать с йо-йо немного проще;<br/>
    /// Привязывается к определенному типу предмета (см. <see cref="YoyoItem.OverrideType"/>), если хотим модифицировать его;<br/>
    /// Но, в отличии от <see cref="GlobalItem"/>, если не указывать значение <see cref="YoyoItem.OverrideType"/>, может создать совершено новый предмет (модовый);<br/>
    /// Зачем это нужно? Да чтобы код был одинаковым как для модовых йо-йо, так и для переделки ванильных... По факту он и не нужен, но я так хочу...<br/>
    /// </summary>
    public abstract class YoyoItem : GlobalItem, ILocalizedModType
    {
        private static readonly Dictionary<Type, YoyoItem> _samples = [];
        private static readonly Dictionary<int, YoyoItem> _byItemType = [];

        public abstract Type ProjectileType { get; }

        /// <summary>
        /// Тип предмета йо-йо, который нужно переделать;<br/>
        /// Если значение равно 0, то создастся новый йо-йо, и класс будет работать именно с ним;<br/>
        /// Тип предмета будет хранится в переменной <see cref="YoyoItem.Type"/><br/>
        /// </summary>
        public virtual int OverrideType => 0;

        public virtual string Texture => null; //< TODO: Сделать замену спрайта при переопределении у ванильных йо-йо?
        public virtual LocalizedText Tooltip => this.GetLocalization(nameof(Tooltip), () => "");
        public virtual int? GamepadExtraRange => null;

        public int Type { get; private set; }
        public bool IsOverride => OverrideType > 0;
        public bool IsVanilla => ItemUtils.IsVanilla(OverrideType);
        public Item Item { get; private set; }

        string ILocalizedModType.LocalizationCategory => "Items";

        public sealed override bool InstancePerEntity => true;

        public sealed override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            if (!lateInstantiation)
                return false;

            return item.type == Type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T GetSample<T>() where T : YoyoItem
            => (T)GetSample(typeof(T));

        public static YoyoItem GetSample(Type type)
        {
            if (TryGetSample(type, out var item))
                return item;

            throw new InvalidOperationException($"YoyoItem '{type.Name}' is not loaded.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetSample(Type type, out YoyoItem yoyo)
            => _samples.TryGetValue(type, out yoyo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(Item item) where T : YoyoItem
            => item.type == GetSample<T>().Type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGet(int itemType, out YoyoItem yoyo)
            => _byItemType.TryGetValue(itemType, out yoyo);

        public sealed override void Load()
        {
            _samples[GetType()] = this;

            var typeName = GetType().FullName;

            if (ProjectileType is null || !typeof(YoyoProjectile).IsAssignableFrom(ProjectileType) || ProjectileType.IsAbstract)
                throw new Exception($"'{typeName}.{nameof(ProjectileType)}' must be a concrete {nameof(YoyoProjectile)} type");

            if (IsOverride)
            {
                if (!IsVanilla && ItemLoader.GetItem(OverrideType) is null)
                    throw new Exception($"'{typeName}.{nameof(OverrideType)}' ({OverrideType}) is not a loaded item");

                Type = OverrideType;
            }
            else
            {
                if (string.IsNullOrEmpty(Texture))
                    throw new Exception($"'{typeName}' must specify {nameof(Texture)}");

                if (!GamepadExtraRange.HasValue)
                    throw new Exception($"'{typeName}' must specify {nameof(GamepadExtraRange)}");

                var stub = (ModItem)Activator.CreateInstance(typeof(ModItemStub<,>).MakeGenericType(GetType(), ProjectileType), nonPublic: true);
                Mod.AddContent(stub);
                Type = stub.Type;
            }

            if (_byItemType.TryGetValue(Type, out var existingByType))
                throw new Exception($"'{typeName}' cannot use item type {Type}; already used by '{existingByType.GetType().FullName}'");

            if (YoyoProjectile.TryGetSample(ProjectileType, out var proj) && proj.ItemType != GetType())
                throw new Exception($"'{proj.GetType().FullName}.{nameof(YoyoProjectile.ItemType)}' must be '{typeName}'");

            _byItemType[Type] = this;

            OnLoad();
        }

        public sealed override void Unload()
        {
            OnUnload();

            _samples.Remove(GetType());
            _byItemType.Remove(Type);

            if (_samples.Count == 0)
                _byItemType.Clear();
        }

        protected virtual void OnLoad() { }

        protected virtual void OnUnload() { }

        public sealed override GlobalItem NewInstance(Item target)
        {
            var inst = (YoyoItem)base.NewInstance(target);
            inst.Type = Type;
            inst.Item = target;
            return inst;
        }

        public sealed override GlobalItem Clone(Item from, Item to)
        {
            var inst = (YoyoItem)base.Clone(from, to);
            inst.Type = Type;
            inst.Item = to;
            return inst;
        }

        /// <summary>
        /// Класс для внесения общих модификакий ванильных йо-йо;<br/>
        /// Нужен для того, чтобы тот же base.SetStaticDefaults() и base.ModifyTooltips() не прописывать каждый раз...<br/>
        /// А запечатывать метод и создавать новый виртуальный с другим наименованием не хочу;<br/>
        /// Поэтому, делает вот такой финт...<br/>
        /// </summary>
        [LoadBefore(typeof(YoyoItem))]
        private sealed class OverrideGlobalItem : GlobalItem
        {
            public override bool AppliesToEntity(Item item, bool lateInstantiation)
            {
                if (!lateInstantiation)
                    return false;

                return TryGet(item.type, out var definition) && definition.IsOverride;
            }

            public override void SetStaticDefaults()
            {
                foreach (var definition in ModContent.GetContent<YoyoItem>())
                {
                    if (!definition.IsOverride)
                        continue;

                    if (definition.GamepadExtraRange.HasValue)
                        ItemID.Sets.GamepadExtraRange[definition.Type] = definition.GamepadExtraRange.Value;

                    _ = definition.Tooltip;
                }
            }

            public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
            {
                if (!TryGet(item.type, out var definition))
                    return;

                var value = definition.Tooltip.Value;

                if (value is null || value == "")
                    return;

                var tooltipLine = new TooltipLine(definition.Mod, "ModTooltip", value);
                tooltips.InsertDescription(tooltipLine.Split('\n'));
            }
        }

        /// <summary>
        /// Заглушка... Чтобы класс мог создавать новые йо-йо, а не только переопределять существующие.
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <typeparam name="TProjectile"></typeparam>
        [Autoload(false)]
        private sealed class ModItemStub<TItem, TProjectile> : ModItem where TItem : YoyoItem where TProjectile : YoyoProjectile
        {
            private static TItem Sample => GetSample<TItem>();

            public override string Name => typeof(TItem).Name.Replace("Item", ""); //< Мы же не хотим, чтобы "выгруженные" предметы имели в наименовании приписку "Item"
            public override string Texture => Sample.Texture;
            public override LocalizedText DisplayName => Sample.GetLocalization(nameof(DisplayName), PrettyPrintName); //< TODO: Заменять ванильные наименования йо-йо?
            public override LocalizedText Tooltip => Sample.Tooltip;

            public override void SetStaticDefaults()
            {
                ItemID.Sets.Yoyo[Type] = true;
                ItemID.Sets.GamepadExtraRange[Type] = Sample.GamepadExtraRange.Value;
                ItemID.Sets.GamepadSmartQuickReach[Type] = true;
            }

            public override void SetDefaults()
            {
                Item.DamageType = DamageClass.MeleeNoSpeed;
                Item.damage = 1;
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
                Item.shoot = YoyoProjectile.GetSample<TProjectile>().Type;
            }
        }
    }

    /// <summary>
    /// Класс, представляющий собой обертку класса <see cref="GlobalItem"/>, позволяющий работать с йо-йо немного проще;<br/>
    /// Привязывается к определенному типу предмета (см. <see cref="YoyoItem.OverrideType"/>), если хотим модифицировать его;<br/>
    /// Но, в отличии от <see cref="GlobalItem"/>, если не указывать значение <see cref="YoyoItem.OverrideType"/>, может создать совершено новый предмет (модовый);<br/>
    /// Зачем это нужно? Да чтобы код был одинаковым как для модовых йо-йо, так и для переделки ванильных... По факту он и не нужен, но я так хочу...<br/>
    /// </summary>
    public abstract class YoyoItem<TProjectile> : YoyoItem where TProjectile : YoyoProjectile
    {
        /// <summary>
        /// Тип переделываемого или создаваемого йо-йо.
        /// </summary>
        public static new int Type => GetSample(YoyoProjectile.GetSample<TProjectile>().ItemType).Type;

        public sealed override Type ProjectileType => typeof(TProjectile);
    }

    public static class YoyoItemExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this Item item) where T : YoyoItem
            => YoyoItem.Is<T>(item);
    }
}
