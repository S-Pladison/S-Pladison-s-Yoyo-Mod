using System;
using System.Collections.Generic;
using Terraria;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Класс-наблюдателя за сущностями.
    /// </summary>
    public abstract class EntityObserver<T>(T[] sourse, Predicate<T> entityShouldBeRemovedPredicate) where T : Entity
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

        protected readonly List<EntityData> _entities = [];
        protected readonly T[] _sourseArray = sourse;
        protected readonly Predicate<T> _entityShouldBeRemovedPredicate = entityShouldBeRemovedPredicate;

        public EntityObserver(T[] sourse) : this(sourse, null) { }

        /// <summary>
        /// Добавить для наблюдение новую сущность.
        /// </summary>
        /// <param name="entity">Объект сущности для последующего наблюдения.</param>
        public void Add(T entity)
        {
            _entities.Add(new EntityData(entity.whoAmI, GetEntityType(entity)));
        }

        /// <summary>
        /// Удаляет сущность из наблюдения. Если сущность ранее не была под наблюдением, ничего не произойдет.
        /// </summary>
        /// <param name="entity">Объект сущности, за которым нужно прекратить наблюдение.</param>
        public void Remove(T entity)
        {
            _entities.Remove(new EntityData(entity.whoAmI, GetEntityType(entity)));
        }

        /// <summary>
        /// Наблюдает за добавленными сущностями и удаляет всех, кто не соответствует условиям наблюдения.
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
        /// Получить данные обо всех наблюдаемых сущностей.
        /// </summary>
        public IEnumerable<T> GetEntityInstances()
        {
            foreach (var entityData in _entities)
                yield return _sourseArray[entityData.WhoAmI];
        }

        /// <summary>
        /// Очистить список наблюдаемых сущностей.
        /// </summary>
        public void Clear()
        {
            _entities.Clear();
        }

        protected abstract int GetEntityType(T entity);
    }

    /// <summary>
    /// Класс-наблюдателя за снарядами.
    /// </summary>
    public sealed class ProjectileObserver(Predicate<Projectile> entityShouldBeRemovedPredicate) : EntityObserver<Projectile>(Main.projectile, entityShouldBeRemovedPredicate)
    {
        public ProjectileObserver() : this(null) { }

        protected override int GetEntityType(Projectile proj) => proj.type;
    }

    /// <summary>
    /// Класс-наблюдателя за НПС.
    /// </summary>
    public sealed class NPCObserver(Predicate<NPC> entityShouldBeRemovedPredicate) : EntityObserver<NPC>(Main.npc, entityShouldBeRemovedPredicate)
    {
        public NPCObserver() : this(null) { }

        protected override int GetEntityType(NPC npc) => npc.type;
    }
}