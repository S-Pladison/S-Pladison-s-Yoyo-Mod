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
    /// <summary>
    /// Класс, представляющий собой обертку класса <see cref="GlobalProjectile"/>, позволяющий работать с йо-йо немного проще;<br/>
    /// Привязываемся к определенному типу снаряда (см. <see cref="YoyoProjectile.OverrideType"/>), если хотим модифицировать его;<br/>
    /// Но в отличии от <see cref="GlobalProjectile"/>, если не указать значение <see cref="YoyoProjectile.OverrideType"/>, может создать совершенно новый предмет (модовый);<br/>
    /// Зачем это нужно? Да чтобы код был одинаковым как для модовых йо-йо, так и для переделки ванильных... По факту он и не нужен, но я так хочу...<br/>
    /// </summary>
    public abstract class YoyoProjectile : GlobalProjectile, IPostDrawYoyoStringProjectile
    {
        private static readonly Dictionary<Type, YoyoProjectile> _samples = [];
        private static readonly Dictionary<int, YoyoProjectile> _byProjType = [];

        public abstract Type ItemType { get; }

        /// <summary>
        /// Тип снаряда йо-йо, который нужно переделать;<br/>
        /// Если значение равно 0, то создастся новый йо-йо, и класс будет работать именно с ним;<br/>
        /// Тип снаряда будет хранится в переменной <see cref="YoyoProjectile.Type"/><br/>
        /// </summary>
        public virtual int OverrideType => 0;

        public virtual string Texture => null; //< TODO: Сделать замену спрайта при переопределении у ванильных йо-йо?
        public virtual float? LifeTime => null;
        public virtual float? MaxRange => null;
        public virtual float? TopSpeed => null;

        public int Type { get; private set; }
        public bool IsReturning => Projectile.ai[0] < 0f;
        public bool IsOverride => OverrideType > 0;
        public bool IsVanilla => IsOverride && ProjectileUtils.IsVanilla(OverrideType);

        /// <summary>
        /// Снаряд, который сейчас находится в мире...
        /// </summary>
        public Projectile Projectile { get; private set; }

        /// <summary>
        /// Предмет, который сейчас держит игрок; Это должен быть йо-йо, тип которого мы указали;<br/>
        /// Иначе быть не должно, а если и будет, то чет я сделал не то...<br/>
        /// </summary>
        public Item Item
        {
            get
            {
                if (Projectile is null || !Projectile.TryGetOwner(out var owner))
                    return null;

                if (!YoyoItem.TryGetSample(ItemType, out var sample))
                    return null;

                var held = owner.HeldItem;

                if (held is null || held.IsAir || held.type != sample.Type)
                    return null;

                return held;
            }
        }

        public sealed override bool InstancePerEntity => true;

        public sealed override bool AppliesToEntity(Projectile proj, bool lateInstantiation)
        {
            if (!lateInstantiation)
                return false;

            return proj.type == Type;
        }

        public static T GetSample<T>() where T : YoyoProjectile
            => (T)GetSample(typeof(T));

        public static YoyoProjectile GetSample(Type type)
        {
            if (TryGetSample(type, out var proj))
                return proj;

            throw new InvalidOperationException($"YoyoProjectile '{type.Name}' is not loaded.");
        }

        public static bool TryGetSample(Type type, out YoyoProjectile yoyo)
            => _samples.TryGetValue(type, out yoyo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Is<T>(Projectile proj) where T : YoyoProjectile
            => proj.type == GetSample<T>().Type;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGet<T>(Projectile proj, out T yoyo) where T : YoyoProjectile
            => proj.TryGetGlobalProjectile(out yoyo);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGet(int projType, out YoyoProjectile yoyo)
            => _byProjType.TryGetValue(projType, out yoyo);

        public sealed override void Load()
        {
            _samples[GetType()] = this;

            var typeName = GetType().FullName;

            if (ItemType is null || !typeof(YoyoItem).IsAssignableFrom(ItemType) || ItemType.IsAbstract)
                throw new Exception($"'{typeName}.{nameof(ItemType)}' must be a concrete {nameof(YoyoItem)} type");

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

            if (_byProjType.TryGetValue(Type, out var existing))
                throw new Exception($"'{typeName}' cannot use projectile type {Type}; already used by '{existing.GetType().FullName}'");

            if (YoyoItem.TryGetSample(ItemType, out var item) && item.ProjectileType != GetType())
                throw new Exception($"'{item.GetType().FullName}.{nameof(YoyoItem.ProjectileType)}' must be '{typeName}'");

            _byProjType[Type] = this;

            OnLoad();
        }

        public sealed override void Unload()
        {
            OnUnload();

            _samples.Remove(GetType());
            _byProjType.Remove(Type);

            if (_samples.Count == 0)
                _byProjType.Clear();
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

        /// <summary>
        /// Класс для внесения общих модификакий ванильных йо-йо;<br/>
        /// Нужен для того, чтобы тот же base.SetStaticDefaults() не прописывать каждый раз...<br/>
        /// А запечатывать метод и создавать новый виртуальный с другим наименованием не хочу;<br/>
        /// Поэтому, делает вот такой финт...<br/>
        /// </summary>
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

        /// <summary>
        /// Заглушка... Чтобы класс мог создавать новые йо-йо, а не только переопределять существующие.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        [Autoload(false)]
        private sealed class ModProjectileStub<T> : ModProjectile where T : YoyoProjectile
        {
            private static T Sample => GetSample<T>();

            public override string Name => typeof(T).Name;
            public override string Texture => Sample.Texture;

            public override void SetStaticDefaults()
            {
                if (Sample.LifeTime.HasValue)
                    ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = Sample.LifeTime.Value;

                if (Sample.MaxRange.HasValue)
                    ProjectileID.Sets.YoyosMaximumRange[Type] = Sample.MaxRange.Value;

                if (Sample.TopSpeed.HasValue)
                    ProjectileID.Sets.YoyosTopSpeed[Type] = Sample.TopSpeed.Value;
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

    /// <summary>
    /// Класс, представляющий собой обертку класса <see cref="GlobalProjectile"/>, позволяющий работать с йо-йо немного проще;<br/>
    /// Привязывается к определенному типу снаряда (см. <see cref="YoyoProjectile.OverrideType"/>), если хотим модифицировать его;<br/>
    /// Но, в отличии от <see cref="GlobalProjectile"/>, если не указывать значение <see cref="YoyoProjectile.OverrideType"/>, может создать совершено новый предмет (модовый);<br/>
    /// Зачем это нужно? Да чтобы код был одинаковым как для модовых йо-йо, так и для переделки ванильных... По факту он и не нужен, но я так хочу...<br/>
    /// </summary>
    public abstract class YoyoProjectile<TItem> : YoyoProjectile where TItem : YoyoItem
    {
        /// <summary>
        /// Тип переделываемого или создаваемого йо-йо.
        /// </summary>
        public static new int Type => GetSample(YoyoItem.GetSample<TItem>().ProjectileType).Type;

        public sealed override Type ItemType => typeof(TItem);
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
