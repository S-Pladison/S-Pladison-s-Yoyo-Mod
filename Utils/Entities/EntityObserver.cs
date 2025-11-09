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

        /// <summary>
        /// Событие, вызываемое в момент добавления сущности в список наблюдения.
        /// </summary>
        public event Action<T> OnAddEntity;

        /// <summary>
        /// Событие, вызываемое в момент удаления сущности из наблюдения.
        /// </summary>
        public event Action<T> OnRemoveEntity;

        protected readonly List<EntityData> _entities = [];
        protected readonly T[] _sourseArray = sourse;
        protected readonly Predicate<T> _entityShouldBeRemovedPredicate = entityShouldBeRemovedPredicate;

        /// <summary>
        /// Добавить для наблюдение новую сущность.
        /// </summary>
        /// <param name="entity">Объект сущности для последующего наблюдения.</param>
        public void Add(T entity)
        {
            if (!_entityShouldBeRemovedPredicate?.Invoke(entity) ?? true)
            {
                _entities.Add(new EntityData(entity.whoAmI, GetEntityType(entity)));
                OnAddEntity?.Invoke(entity);
            }
        }

        /// <summary>
        /// Удаляет сущность из наблюдения. Если сущность ранее не была под наблюдением, ничего не произойдет.
        /// </summary>
        /// <param name="entity">Объект сущности, за которым нужно прекратить наблюдение.</param>
        public void Remove(T entity)
        {
            if (_entities.Remove(new EntityData(entity.whoAmI, GetEntityType(entity))))
                OnRemoveEntity?.Invoke(entity);
        }

        /// <summary>
        /// Получить данные обо всех наблюдаемых сущностей.
        /// </summary>
        public IEnumerable<T> GetEntityInstances()
        {
            for (var i = 0; i < _entities.Count; i++)
            {
                var entity = _sourseArray[_entities[i].WhoAmI];

                if (!entity.active || GetEntityType(entity) != _entities[i].Type || (_entityShouldBeRemovedPredicate?.Invoke(entity) ?? false))
                {
                    OnRemoveEntity?.Invoke(entity);
                    _entities.RemoveAt(i--);
                    continue;
                }

                yield return entity;
            }
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
    public sealed class ProjectileObserver : EntityObserver<Projectile>
    {
        private ProjectileObserver(Predicate<Projectile> entityShouldBeRemovedPredicate) : base(Main.projectile, entityShouldBeRemovedPredicate) { }

        protected override int GetEntityType(Projectile proj) => proj.type;

        /// <summary>
        /// Создать объект класса-наблюдателя за снарядами.
        /// </summary>
        public static ProjectileObserver Create(Predicate<Projectile> entityShouldBeRemovedPredicate)
        {
            return new(
                entityShouldBeRemovedPredicate
            );
        }
    }

    /// <summary>
    /// Класс-наблюдателя за NPC.
    /// </summary>
    public sealed class NPCObserver : EntityObserver<NPC>
    {
        private NPCObserver(Predicate<NPC> entityShouldBeRemovedPredicate) : base(Main.npc, entityShouldBeRemovedPredicate) { }

        protected override int GetEntityType(NPC npc) => npc.type;

        /// <summary>
        /// Создать объект класса-наблюдателя за NPC.
        /// </summary>
        public static NPCObserver Create(Predicate<NPC> entityShouldBeRemovedPredicate)
        {
            return new(
                entityShouldBeRemovedPredicate
            );
        }
    }
}