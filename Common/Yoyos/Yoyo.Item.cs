using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    /// <summary>
    /// Инициализация нового и модификация существующего предмета йо-йо.<br/>
    /// Если задан <see cref="OverrideType"/>, накладывается на существующий предмет; иначе создаётся новый модовый предмет.<br/>
    /// </summary>
    public abstract partial class YoyoItem : GlobalItem, ILocalizedModType
    {
        private static readonly Dictionary<Type, YoyoItem> _definitions = [];
        private static readonly Dictionary<int, YoyoItem> _byItemType = [];
        private static readonly Dictionary<Type, YoyoItem> _byProjectileClass = [];

        /// <summary>
        /// Тип предмета.
        /// </summary>
        public int Type { get; private set; }

        /// <summary>
        /// Накладывается ли этот класс на уже существующий предмет.
        /// </summary>
        public bool IsOverride => OverrideType > 0;

        /// <summary>
        /// Накладывается ли этот класс на ванильный предмет.
        /// </summary>
        public bool IsVanilla => ItemUtils.IsVanilla(OverrideType);

        /// <summary>
        /// Тип предмета, который нужно модифицировать.<br/>
        /// Оставьте 0, чтобы создать новый модовый предмет.
        /// </summary>
        public virtual int OverrideType => 0;

        // TODO: Сделать замену спрайта при переопределении у ванильных йо-йо?
        /// <summary>
        /// Текстура предмета. Обязательна для нового йо-йо.
        /// </summary>
        public virtual string Texture => null;

        /// <summary>
        /// Текст всплывающей подсказки предмета.
        /// </summary>
        public virtual LocalizedText Tooltip => this.GetLocalization(nameof(Tooltip), () => "");

        /// <summary>
        /// Диапазон использования с геймпада в плитках.<br/>
        /// Обязателен для нового йо-йо.
        /// </summary>
        public virtual int? GamepadExtraRange => null;

        /// <summary>
        /// Предмет, к которому привязан этот экземпляр.
        /// </summary>
        public Item Item { get; private set; }

        internal abstract Type ProjectileClass { get; }

        string ILocalizedModType.LocalizationCategory => "Items";

        public sealed override bool InstancePerEntity => true;

        public sealed override bool AppliesToEntity(Item item, bool lateInstantiation)
        {
            if (!lateInstantiation)
                return false;

            return item.type == Type;
        }

        /// <summary>
        /// Возвращает экземпляр типа <typeparamref name="T"/>.
        /// Данный экземпляр не существует в мире. Он лишь служит примером того, каким должен быть предмет.
        /// </summary>
        public static T Get<T>() where T : YoyoItem
        {
            if (_definitions.TryGetValue(typeof(T), out var item))
                return (T)item;

            throw new InvalidOperationException($"YoyoItem '{typeof(T).Name}' is not loaded.");
        }

        /// <summary>
        /// Является ли предмет йо-йом типа <typeparamref name="T"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(Item item) where T : YoyoItem
            => item.type == Get<T>().Type;

        private static bool TryGet(int itemType, out YoyoItem yoyo)
            => _byItemType.TryGetValue(itemType, out yoyo);

        internal static bool TryGetByProjectile(Type projectileClass, out YoyoItem yoyo)
            => _byProjectileClass.TryGetValue(projectileClass, out yoyo);

        public sealed override void Load()
        {
            _definitions[GetType()] = this;

            var typeName = GetType().FullName;

            if (ProjectileClass is null || !typeof(YoyoProjectile).IsAssignableFrom(ProjectileClass) || ProjectileClass.IsAbstract)
                throw new Exception($"'{typeName}.{nameof(ProjectileClass)}' must be a concrete {nameof(YoyoProjectile)} type");

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

                var stub = (ModItem)Activator.CreateInstance(typeof(ModItemStub<>).MakeGenericType(GetType()), nonPublic: true);
                Mod.AddContent(stub);
                Type = stub.Type;
            }

            if (_byProjectileClass.TryGetValue(ProjectileClass, out var existingByProjectile))
                throw new Exception($"'{typeName}' cannot use {nameof(YoyoProjectile)} '{ProjectileClass.Name}'; already used by '{existingByProjectile.GetType().FullName}'");

            if (_byItemType.TryGetValue(Type, out var existingByType))
                throw new Exception($"'{typeName}' cannot use item type {Type}; already used by '{existingByType.GetType().FullName}'");

            if (YoyoProjectile.TryGet(ProjectileClass, out var proj) && proj.ItemClass != GetType())
                throw new Exception($"'{proj.GetType().FullName}.{nameof(YoyoProjectile.ItemClass)}' must be '{typeName}'");

            _byProjectileClass[ProjectileClass] = this;
            _byItemType[Type] = this;

            OnLoad();
        }

        public sealed override void Unload()
        {
            OnUnload();

            _definitions.Remove(GetType());
            _byItemType.Remove(Type);
            _byProjectileClass.Remove(ProjectileClass);

            if (_definitions.Count == 0)
            {
                _byItemType.Clear();
                _byProjectileClass.Clear();
            }
        }

        /// <summary>
        /// Вызывается при загрузке, после регистрации типа.
        /// </summary>
        protected virtual void OnLoad() { }

        /// <summary>
        /// Вызывается при выгрузке.
        /// </summary>
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
    }

    /// <summary>
    /// Предмет йо-йо, связанный со снарядом <typeparamref name="TProjectile"/>.
    /// </summary>
    public abstract class YoyoItem<TProjectile> : YoyoItem where TProjectile : YoyoProjectile
    {
        /// <summary>
        /// Тип предмета.
        /// </summary>
        public static new int Type
        {
            get
            {
                if (!TryGetByProjectile(typeof(TProjectile), out var item))
                    throw new InvalidOperationException($"YoyoItem '{typeof(TProjectile).Name}' is not loaded.");

                return item.Type;
            }
        }

        internal sealed override Type ProjectileClass => typeof(TProjectile);
    }

    public static class YoyoItemExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this Item item) where T : YoyoItem
            => YoyoItem.Is<T>(item);
    }
}
