using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Terraria;

namespace SPYoyoMod.Utils
{
    public abstract class EntityObserver<T> where T : Entity
    {
        protected struct EntityData
        {
            public int WhoAmI { get; init; }
            public int Type { get; init; }

            public EntityData(int whoAmI, int type)
            {
                WhoAmI = whoAmI;
                Type = type;
            }
        }

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

        public void Add(T entity)
        {
            entities.Add(new EntityData(entity.whoAmI, GetEntityType(entity)));
        }

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

        public IList<T> GetEntityInstances()
        {
            var result = new List<T>(entities.Count);

            foreach (var entityData in entities)
            {
                result.Add(sourseArray[entityData.WhoAmI]);
            }

            return result;
        }

        public void Clear()
        {
            entities.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        protected abstract int GetEntityType(T entity);
    }

    public class ProjectileObserver : EntityObserver<Projectile>
    {
        public ProjectileObserver() : this(null) { }

        public ProjectileObserver(Predicate<Projectile> entityShouldBeRemovedPredicate) : base(Main.projectile, entityShouldBeRemovedPredicate) { }

        protected override int GetEntityType(Projectile proj) => proj.type;
    }

    public class NPCObserver : EntityObserver<NPC>
    {
        public NPCObserver() : this(null) { }

        public NPCObserver(Predicate<NPC> entityShouldBeRemovedPredicate) : base(Main.npc, entityShouldBeRemovedPredicate) { }

        protected override int GetEntityType(NPC npc) => npc.type;
    }
}