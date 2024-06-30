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
        public bool AnyEntity { get => entities.Count > 0; }

        protected readonly List<EntityData> entities;
        protected readonly T[] sourseArray;
        protected readonly Predicate<T> entityShouldBeRemovedPredicate;

        public EntityObserver(T[] sourse) : this(sourse, null) { }

        public EntityObserver(T[] sourse, Predicate<T> entityShouldBeRemovedPredicate)
        {
            this.entities = new List<EntityData>();
            this.sourseArray = sourse;
            this.entityShouldBeRemovedPredicate = entityShouldBeRemovedPredicate;
        }

        /// <summary>
        /// Добавить для наблюдение новую сущность.
        /// </summary>
        public void Add(T entity)
        {
            entities.Add(new EntityData(entity.whoAmI, GetEntityType(entity)));
        }

        /// <summary>
        /// Удаляет не соответствующие условиям наблюдения сущности.
        /// </summary>
        public void Update()
        {
            for (var i = 0; i < entities.Count; i++)
            {
                ref var entity = ref sourseArray[entities[i].WhoAmI];

                if (!entity.active || GetEntityType(entity) != entities[i].Type || (entityShouldBeRemovedPredicate?.Invoke(entity) ?? false))
                {
                    entities.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Получить список наблюдаемых сущностей.
        /// </summary>
        public IList<T> GetEntityInstances()
        {
            var result = new List<T>(entities.Count);

            foreach (var entityData in entities)
            {
                result.Add(sourseArray[entityData.WhoAmI]);
            }

            return result;
        }

        // Очистить список наблюдаемых сущностей.
        public void Clear()
        {
            entities.Clear();
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