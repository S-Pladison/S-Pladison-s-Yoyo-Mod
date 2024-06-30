using System;
using System.Collections.Generic;
using Terraria;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Класс-наблюдателя за сущностями.
    /// </summary>
    public abstract class EntityObserver<T> where T : Entity
    {
        protected readonly struct EntityData(int whoAmI, int type)
        {
            public int WhoAmI { get; init; } = whoAmI;
            public int Type { get; init; } = type;
        }

        /// <summary>
        /// Ведется ли наблюдение хотя бы за 1 сущностью.
        /// </summary>
        public bool AnyEntity { get => _entities.Count > 0; }

        protected readonly List<EntityData> _entities;
        protected readonly T[] _sourseArray;
        protected readonly Predicate<T> _entityShouldBeRemovedPredicate;

        public EntityObserver(T[] sourse) : this(sourse, null) { }

        public EntityObserver(T[] sourse, Predicate<T> entityShouldBeRemovedPredicate)
        {
            _entities = new List<EntityData>();
            _sourseArray = sourse;
            _entityShouldBeRemovedPredicate = entityShouldBeRemovedPredicate;
        }

        /// <summary>
        /// Добавить для наблюдение новую сущность.
        /// </summary>
        public void Add(T entity)
        {
            _entities.Add(new EntityData(entity.whoAmI, GetEntityType(entity)));
        }

        /// <summary>
        /// Удаляет не соответствующие условиям наблюдения сущности.
        /// </summary>
        public void Update()
        {
            for (var i = 0; i < _entities.Count; i++)
            {
                ref var entity = ref _sourseArray[_entities[i].WhoAmI];

                if (!entity.active || GetEntityType(entity) != _entities[i].Type || (_entityShouldBeRemovedPredicate?.Invoke(entity) ?? false))
                {
                    _entities.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Получить список наблюдаемых сущностей.
        /// </summary>
        public IList<T> GetEntityInstances()
        {
            var result = new List<T>(_entities.Count);

            foreach (var entityData in _entities)
            {
                result.Add(_sourseArray[entityData.WhoAmI]);
            }

            return result;
        }

        // Очистить список наблюдаемых сущностей.
        public void Clear()
        {
            _entities.Clear();
        }

        protected abstract int GetEntityType(T entity);
    }

    /// <summary>
    /// Класс-наблюдателя за снарядами.
    /// </summary>
    public class ProjectileObserver : EntityObserver<Projectile>
    {
        public ProjectileObserver() : this(null) { }

        public ProjectileObserver(Predicate<Projectile> entityShouldBeRemovedPredicate) : base(Main.projectile, entityShouldBeRemovedPredicate) { }

        protected override int GetEntityType(Projectile proj) => proj.type;
    }

    /// <summary>
    /// Класс-наблюдателя за НПС.
    /// </summary>
    public class NPCObserver : EntityObserver<NPC>
    {
        public NPCObserver() : this(null) { }

        public NPCObserver(Predicate<NPC> entityShouldBeRemovedPredicate) : base(Main.npc, entityShouldBeRemovedPredicate) { }

        protected override int GetEntityType(NPC npc) => npc.type;
    }
}