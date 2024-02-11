using System;
using System.Collections.Generic;
using Terraria;

namespace SPYoyoMod.Utils
{
    public abstract class EntityObserver<T> where T : Entity
    {
        public IReadOnlyList<int> Entities { get => entities; }
        public bool AnyEntity { get => entities.Count > 0; }

        protected readonly List<int> entities;
        protected readonly T[] sourseArray;
        protected readonly Predicate<T> entityShouldBeRemovedPredicate;

        public EntityObserver(T[] sourse, Predicate<T> entityShouldBeRemovedPredicate)
        {
            this.entities = new List<int>();
            this.sourseArray = sourse;
            this.entityShouldBeRemovedPredicate = entityShouldBeRemovedPredicate;
        }

        public void Add(T entity)
        {
            entities.Add(entity.whoAmI);
        }

        public void Update()
        {
            for (int i = 0; i < entities.Count; i++)
            {
                ref var entity = ref sourseArray[entities[i]];

                if (entityShouldBeRemovedPredicate(entity))
                {
                    entities.RemoveAt(i);
                    i--;
                }
            }
        }

        public void Clear()
        {
            entities.Clear();
        }
    }

    public class ProjectileObserver : EntityObserver<Projectile>
    {
        public ProjectileObserver(Predicate<Projectile> entityShouldBeRemovedPredicate) : base(Main.projectile, entityShouldBeRemovedPredicate) { }

        public IList<Projectile> GetProjectileInstances()
        {
            var result = new List<Projectile>(Entities.Count);

            foreach (var entityIndex in Entities)
            {
                result.Add(sourseArray[entityIndex]);
            }

            return result;
        }
    }

    public class NPCObserver : EntityObserver<NPC>
    {
        public NPCObserver(Predicate<NPC> entityShouldBeRemovedPredicate) : base(Main.npc, entityShouldBeRemovedPredicate) { }

        public IList<NPC> GetNPCInstances()
        {
            var result = new List<NPC>(Entities.Count);

            foreach (var entityIndex in Entities)
            {
                result.Add(sourseArray[entityIndex]);
            }

            return result;
        }
    }
}