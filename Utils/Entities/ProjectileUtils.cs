using System.IO;
using System.Runtime.CompilerServices;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader.IO;
using Terraria.ModLoader;

namespace SPYoyoMod.Utils
{
    public static class ProjectileUtils
    {
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
        /// Является ли этот снаряд основным снарядом от йо-йо.
        /// Основным является тот, которым управляет игрок, а не тот, который летает возле.
        /// Учитывайте, что основной йо-йо не обязательно будет тем, что заспавнился первым.
        /// </summary>
        public static bool IsMainYoyo(this Projectile proj)
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
        public static Player? GetOwner(this Projectile proj)
        {
            if (!Main.player.IndexInRange(proj.owner))
                return null;

            var player = Main.player[proj.owner];

            if (player == null || !player.active)
                return null;

            return player;
        }

        private sealed class RelatedToYoyoGlobalProjectile : GlobalProjectile
        {
            public bool RelatedToYoyo { get; private set; }
            public override bool InstancePerEntity { get => true; }

            public override void OnSpawn(Projectile proj, IEntitySource source)
            {
                if (source is not EntitySource_Parent parentSource || parentSource.Entity is not Projectile parentProj)
                    return;

                if (parentProj.IsYoyo() || parentProj.IsCounterweight())
                {
                    RelatedToYoyo = true;
                    return;
                }

                if (!parentProj.TryGetGlobalProjectile(out RelatedToYoyoGlobalProjectile parentGlobal))
                    return;

                RelatedToYoyo = parentGlobal.RelatedToYoyo;
            }

            public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
            {
                bitWriter.WriteBit(RelatedToYoyo);
            }

            public override void ReceiveExtraAI(Projectile proj, BitReader bitReader, BinaryReader binaryReader)
            {
                RelatedToYoyo = bitReader.ReadBit();
            }
        }
    }
}