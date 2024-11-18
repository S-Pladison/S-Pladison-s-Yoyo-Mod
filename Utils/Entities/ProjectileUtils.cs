using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Yoyos;
using System;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class ProjectileUtils
    {
        /// <summary>
        /// Настраивает указанный снаряд для использования исключительно как визуальный эффект, отключая взаимодействие с окружающей средой и игровыми сущностями.
        /// </summary>
        public static void DefaultToVisualEffect(this Projectile proj)
        {
            proj.width = 16;
            proj.height = 16;
            proj.timeLeft = 60;
            proj.friendly = true;
            proj.penetrate = -1;
            proj.ignoreWater = true;
            proj.tileCollide = false;
            proj.damage = 0;

            proj.DamageType = DamageClass.Generic;
            proj.CritChance = 0;
        }

        /// <summary>
        /// Является ли этот снаряд йо-йом.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsYoyo(this Projectile proj)
            => proj.aiStyle.Equals(ProjAIStyleID.Yoyo) && !proj.counterweight;

        /// <summary>
        /// Является ли этот снаряд противовесом.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCounterweight(this Projectile proj)
            => proj.counterweight;

        /// <summary>
        /// Связан ли этот снаряд хоть как-то с йо-йо. Проще говоря, является ли он йо-йо, его противовесом или вовсе поражден от другого снаряда, порожденного другим снарядом, порожденным йо-йо.
        /// Не советую использовать данную функцию при загрузке мода, т.к. флаг, указывающий на то, связан ли снаряд с йо-йо, устанавливается лишь при спавне самого снаряда.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsYoyoOrRelated(this Projectile proj)
        {
            return proj.IsYoyo()
                || proj.IsCounterweight()
                || proj.TryGetGlobalProjectile(out RelatedToYoyoGlobalProjectile globalProj) && globalProj.RelatedToYoyo;
        }

        /// <summary>
        /// Является ли этот снаряд основным снарядом йо-йо.
        /// Основным является тот, которым управляет игрок, а не тот, который летает возле.
        /// Учитывайте, что основной йо-йо не обязательно будет тем, что заспавнился первым.
        /// </summary>
        public static bool IsPrimaryYoyo(this Projectile proj)
        {
            if (!proj.IsYoyo() || proj.IsCounterweight())
                return false;

            for (int i = 0; i < proj.whoAmI; i++)
            {
                ref var otherProj = ref Main.projectile[i];

                if (otherProj.type == proj.type && otherProj.owner == proj.owner)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Является ли этот снаряд снарядом ванильным.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVanilla(this Projectile proj)
            => proj.type < ProjectileID.Count;

        /// <summary>
        /// Является ли локальный игрок владельцем данного снаряда.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLocalPlayerAsOwner(this Projectile proj)
            => proj.owner == Main.myPlayer;

        /// <summary>
        /// Получить владельца (игрока) снаряда.
        /// </summary>
        public static Player GetOwner(this Projectile proj)
        {
            if (!Main.player.IndexInRange(proj.owner))
                return null;

            var player = Main.player[proj.owner];

            if (player == null || !player.active)
                return null;

            return player;
        }

        /// <summary>
        /// Преобразует текущий объект типа <see cref="Projectile"/> в указанный тип <typeparamref name="T"/>, 
        /// если он является моддовым снарядом типа <typeparamref name="T"/>. Возвращает <c>null</c>, если преобразование невозможно.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(this Projectile proj) where T : ModProjectile
            => proj.ModProjectile as T;

        /// <summary>
        /// Возвращает первый снаряд, удовлетворяющий заданному условию, или null, если снаряд не найден.
        /// </summary>
        public static Projectile FirstOrDefault(this ActiveEntityIterator<Projectile> projectiles, Predicate<Projectile> predicate)
        {
            foreach (var proj in projectiles)
            {
                if (predicate(proj))
                    return proj;
            }

            return null;
        }

        /// <summary>
        /// Находит снаряд по указанному идентификатору <paramref name="identity"/>.
        /// </summary>
        public static Projectile FindByIdentityOrDefault(this ActiveEntityIterator<Projectile> projectiles, int identity)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return Main.projectile[identity];

            foreach (var proj in projectiles)
            {
                if (proj.identity != identity)
                    continue;

                return proj;
            }

            return null;
        }

        /// <summary>
        /// Содержит ли коллекция снарядов хотя бы один снаряд.
        /// </summary>
        public static bool Any(this ActiveEntityIterator<Projectile> projectiles)
        {
            foreach (var _ in projectiles)
                return true;

            return false;
        }

        /// <summary>
        /// Содержит ли коллекция хотя бы один снаряд, удовлетворяющий заданному условию.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Any(this ActiveEntityIterator<Projectile> projectiles, Predicate<Projectile> predicate)
            => FirstOrDefault(projectiles, predicate) != null;

        /// <summary>
        /// Вычисляет направление, в котором нужно выстрелить, чтобы попасть в движущуюся цель с учётом её скорости.
        /// </summary>
        public static Vector2 PredictiveAimToTarget(Vector2 startPosition, Vector2 targetPosition, Vector2 targetVelocity, float speed)
        {
            var toTarget = targetPosition - startPosition;

            // Квадратные значения для решения уравнения
            var distanceSquared = toTarget.LengthSquared();
            var speedSquared = speed * speed;
            var targetSpeedSquared = targetVelocity.LengthSquared();
            var targetSpeedAlongToTarget = Vector2.Dot(toTarget, targetVelocity);

            // Дискриминант квадратного уравнения
            var a = speedSquared - targetSpeedSquared;
            var b = -2f * targetSpeedAlongToTarget;
            var c = -distanceSquared;
            var discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
                return Vector2.Normalize(toTarget);

            // Вычисление времени до пересечения
            var sqrtDiscriminant = (float)Math.Sqrt(discriminant);
            var t1 = (-b + sqrtDiscriminant) / (2f * a);
            var t2 = (-b - sqrtDiscriminant) / (2f * a);
            var t = Math.Max(t1, t2);

            if (t < 0)
                return Vector2.Normalize(toTarget);

            // Вычисление направления к будущей позиции цели
            var futurePosition = targetPosition + targetVelocity * t;
            var toFutureTarget = futurePosition - startPosition;
            return Vector2.Normalize(toFutureTarget);
        }
    }
}