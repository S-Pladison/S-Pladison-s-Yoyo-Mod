using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public abstract partial class YoyoItem : GlobalItem, ILocalizedModType
    {
        private static readonly Dictionary<Type, YoyoItem> _definitions = [];
        private static readonly Dictionary<int, YoyoItem> _byItemType = [];
        private static readonly Dictionary<Type, YoyoItem> _byProjectileClass = [];

        public int Type { get; private set; }

        public bool IsOverride => OverrideType > 0;

        public bool IsVanilla => ItemUtils.IsVanilla(OverrideType);

        public virtual int OverrideType => 0;

        // TODO: Сделать замену спрайта при переопределении у ванильных йо-йо?
        public virtual string Texture => null;

        public virtual LocalizedText Tooltip => this.GetLocalization(nameof(Tooltip), () => "");

        public virtual int? GamepadExtraRange => null;

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

        public static T Get<T>() where T : YoyoItem
        {
            if (_definitions.TryGetValue(typeof(T), out var item))
                return (T)item;

            throw new InvalidOperationException($"YoyoItem '{typeof(T).Name}' is not loaded.");
        }

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
    }

    public abstract class YoyoItem<TProjectile> : YoyoItem where TProjectile : YoyoProjectile
    {
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
