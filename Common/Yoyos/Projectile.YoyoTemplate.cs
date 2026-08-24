using Microsoft.Xna.Framework;
using SPYoyoMod.Core;
using SPYoyoMod.Core.Hooks;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Yoyos
{
    public abstract class YoyoProjectile : GlobalProjectile, IPostDrawYoyoStringProjectile
    {
        private static readonly Dictionary<Type, YoyoProjectile> _definitions = [];
        private static readonly Dictionary<int, YoyoProjectile> _byProjectileType = [];
        private static readonly Dictionary<Type, YoyoProjectile> _byItemClass = [];

        public virtual int OverrideType => 0;
        public int Type { get; private set; }
        public bool IsReturning => Projectile.ai[0] < 0f;
        public bool IsOverride => OverrideType > 0;
        public bool IsVanilla => IsOverride && ProjectileUtils.IsVanilla(OverrideType);
        public virtual string Texture => null; // TODO: Сделать замену спрайта при переопределении у ванильных йо-йо?
        public virtual float? LifeTime => null;
        public virtual float? MaxRange => null;
        public virtual float? TopSpeed => null;
        public Projectile Projectile { get; private set; }
        public Item Item
        {
            get
            {
                if (Projectile is null || !Projectile.TryGetOwner(out var owner))
                    return null;

                if (!YoyoItem.TryGetByProjectile(GetType(), out var definition))
                    return null;

                var held = owner.HeldItem;

                if (held is null || held.IsAir || held.type != definition.Type)
                    return null;

                return held;
            }
        }

        internal abstract Type ItemClass { get; }

        public sealed override bool InstancePerEntity => true;

        public sealed override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
        {
            if (!lateInstantiation)
                return false;

            return proj.type == Type;
        }

        public static T Get<T>() where T : YoyoProjectile
        {
            if (_definitions.TryGetValue(typeof(T), out var proj))
                return (T)proj;

            throw new InvalidOperationException($"YoyoProjectile '{typeof(T).Name}' is not loaded.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(Projectile proj) where T : YoyoProjectile
            => proj.type == Get<T>().Type;

        public static bool TryGet<T>(Projectile proj, out T yoyo) where T : YoyoProjectile
            => proj.TryGetGlobalProjectile(out yoyo);

        internal static bool TryGet(Type type, out YoyoProjectile yoyo)
            => _definitions.TryGetValue(type, out yoyo);

        private static bool TryGet(int projectileType, out YoyoProjectile yoyo)
            => _byProjectileType.TryGetValue(projectileType, out yoyo);

        protected static bool TryGetByItemClass(Type itemClass, out YoyoProjectile yoyo)
            => _byItemClass.TryGetValue(itemClass, out yoyo);

        public sealed override void Load()
        {
            _definitions[GetType()] = this;

            var typeName = GetType().FullName;

            if (ItemClass is null || !typeof(YoyoItem).IsAssignableFrom(ItemClass) || ItemClass.IsAbstract)
                throw new Exception($"'{typeName}.{nameof(ItemClass)}' must be a concrete {nameof(YoyoItem)} type");

            if (IsOverride)
            {
                if (!IsVanilla && ProjectileLoader.GetProjectile(OverrideType) is null)
                    throw new Exception($"'{typeName}.{nameof(OverrideType)}' ({OverrideType}) is not a loaded projectile");

                Type = OverrideType;
            }
            else
            {
                if (LifeTime is null || MaxRange is null || TopSpeed is null)
                    throw new Exception($"'{typeName}' must specify {nameof(LifeTime)}, {nameof(MaxRange)} and {nameof(TopSpeed)}");

                if (string.IsNullOrEmpty(Texture))
                    throw new Exception($"'{typeName}' must specify {nameof(Texture)}");

                var stub = (ModProjectile)Activator.CreateInstance(typeof(ModProjectileStub<>).MakeGenericType(GetType()), nonPublic: true);
                Mod.AddContent(stub);
                Type = stub.Type;
            }

            if (_byProjectileType.TryGetValue(Type, out var existing))
                throw new Exception($"'{typeName}' cannot use projectile type {Type}; already used by '{existing.GetType().FullName}'");

            if (_byItemClass.TryGetValue(ItemClass, out var existingByItem))
                throw new Exception($"'{typeName}' cannot use {nameof(YoyoItem)} '{ItemClass.Name}'; already used by '{existingByItem.GetType().FullName}'");

            if (YoyoItem.TryGetByProjectile(GetType(), out var item) && item.GetType() != ItemClass)
                throw new Exception($"'{typeName}.{nameof(ItemClass)}' must be '{item.GetType().FullName}'");

            _byProjectileType[Type] = this;
            _byItemClass[ItemClass] = this;

            OnLoad();
        }

        public sealed override void Unload()
        {
            OnUnload();

            _definitions.Remove(GetType());
            _byProjectileType.Remove(Type);
            _byItemClass.Remove(ItemClass);

            if (_definitions.Count == 0)
            {
                _byProjectileType.Clear();
                _byItemClass.Clear();
            }
        }

        protected virtual void OnLoad() { }

        protected virtual void OnUnload() { }

        /// <inheritdoc cref="IPostDrawYoyoStringProjectile.PostDrawYoyoString"/>
        public virtual void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter) { }

        public sealed override GlobalProjectile NewInstance(Projectile target)
        {
            var inst = (YoyoProjectile)base.NewInstance(target);
            inst.Type = Type;
            inst.Projectile = target;
            return inst;
        }

        public sealed override GlobalProjectile Clone(Projectile from, Projectile to)
        {
            var inst = (YoyoProjectile)base.Clone(from, to);
            inst.Type = Type;
            inst.Projectile = to;
            return inst;
        }

        [LoadBefore(typeof(YoyoProjectile))]
        private sealed class OverrideGlobalProjectile : GlobalProjectile
        {
            public override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
            {
                if (!lateInstantiation)
                    return false;

                return TryGet(proj.type, out var definition) && definition.IsOverride;
            }

            public override void SetStaticDefaults()
            {
                foreach (var definition in ModContent.GetContent<YoyoProjectile>())
                {
                    if (!definition.IsOverride)
                        continue;

                    if (definition.LifeTime.HasValue)
                        ProjectileID.Sets.YoyosLifeTimeMultiplier[definition.Type] = definition.LifeTime.Value;

                    if (definition.MaxRange.HasValue)
                        ProjectileID.Sets.YoyosMaximumRange[definition.Type] = definition.MaxRange.Value;

                    if (definition.TopSpeed.HasValue)
                        ProjectileID.Sets.YoyosTopSpeed[definition.Type] = definition.TopSpeed.Value;
                }
            }
        }

        [Autoload(false)]
        private sealed class ModProjectileStub<T> : ModProjectile where T : YoyoProjectile
        {
            private static T Definition => Get<T>();

            public override string Name => typeof(T).Name;
            public override string Texture => Definition.Texture;

            public override void SetStaticDefaults()
            {
                if (Definition.LifeTime.HasValue)
                    ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = Definition.LifeTime.Value;

                if (Definition.MaxRange.HasValue)
                    ProjectileID.Sets.YoyosMaximumRange[Type] = Definition.MaxRange.Value;

                if (Definition.TopSpeed.HasValue)
                    ProjectileID.Sets.YoyosTopSpeed[Type] = Definition.TopSpeed.Value;
            }

            public override void SetDefaults()
            {
                Projectile.DamageType = DamageClass.MeleeNoSpeed;
                Projectile.width = 16;
                Projectile.height = 16;
                Projectile.aiStyle = ProjAIStyleID.Yoyo;
                Projectile.friendly = true;
                Projectile.penetrate = -1;
            }
        }
    }

    public abstract class YoyoProjectile<TItem> : YoyoProjectile where TItem : YoyoItem
    {
        public static new int Type
        {
            get
            {
                if (!TryGetByItemClass(typeof(TItem), out var proj))
                    throw new InvalidOperationException($"YoyoProjectile '{typeof(TItem).Name}' is not loaded.");

                return proj.Type;
            }
        }

        internal sealed override Type ItemClass => typeof(TItem);
    }

    public static class YoyoProjectileExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(this Projectile proj) where T : YoyoProjectile
            => YoyoProjectile.Is<T>(proj);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetYoyo<T>(this Projectile proj, out T yoyo) where T : YoyoProjectile
            => YoyoProjectile.TryGet(proj, out yoyo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T AsYoyo<T>(this Projectile proj) where T : YoyoProjectile
            => YoyoProjectile.TryGet<T>(proj, out var yoyo) ? yoyo : null;
    }
}
