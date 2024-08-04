using System.Runtime.CompilerServices;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Utils
{
    // [
    //   IsBossOrRelated, IsBoss, IsBossLimb, IsMiniBoss and IsChild taken from:
    //   https://github.com/SamsonAllen13/ClickerClass/blob/master/Utilities/NPCHelper.cs
    //   https://github.com/SamsonAllen13/ClickerClass/blob/master/Utilities/NPCHelper_Bosses.cs
    // ]

    public static class NPCUtils
    {
        /// <summary>
        /// Связан ли этот НПС как то с боссом или мини-боссом. Этом может быть и сам босс, в случае Скелетрона - рука и т.д.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBossOrRelated(this NPC npc)
            => npc.IsBoss() || npc.IsBossLimb() || npc.IsMiniBoss();

        /// <summary>
        /// Является ли этот NPC боссом. В большинстве случаем, на это влияет значение поля npc.boss или NPCID.Sets.ShouldBeCountedAsBoss[npc.type],
        /// но есть исключения.
        /// </summary>
        public static bool IsBoss(this NPC npc)
        {
            var type = npc.type;

            if (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[type])
                return true;

            switch (type)
            {
                // Eater of Worlds
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                // Misc
                case NPCID.DungeonGuardian:
                    return true;
                default:
                    break;
            }

            return npc.IsChild(out NPC parent) && parent.whoAmI != npc.whoAmI && parent.IsBoss();
        }

        /// <summary>
        /// Является ли этот NPC частью/конечностью босса.
        /// </summary>
        public static bool IsBossLimb(this NPC npc)
        {
            switch (npc.type)
            {
                // Eater of Worlds
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:
                // Skeletron
                case NPCID.SkeletronHand:
                // Skeletron Prime
                case NPCID.PrimeCannon:
                case NPCID.PrimeLaser:
                case NPCID.PrimeSaw:
                case NPCID.PrimeVice:
                // Golem
                case NPCID.GolemHead:
                case NPCID.GolemHeadFree:
                case NPCID.GolemFistLeft:
                case NPCID.GolemFistRight:
                // Pirate Ship
                case NPCID.PirateShipCannon:
                // Martian Saucer
                case NPCID.MartianSaucerCannon:
                case NPCID.MartianSaucerTurret:
                case NPCID.MartianSaucer:
                // Moon Lord
                case NPCID.MoonLordHead:
                case NPCID.MoonLordHand:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Является ли этот NPC мини-боссом.
        /// </summary>
        public static bool IsMiniBoss(this NPC npc)
        {
            switch (npc.type)
            {
                // Biomes
                case NPCID.SandElemental:
                case NPCID.IceGolem:
                case NPCID.Paladin:
                case NPCID.Mothron:
                case NPCID.MartianSaucerCore:
                // Events
                case NPCID.PirateShip:
                case NPCID.IceQueen:
                case NPCID.SantaNK1:
                case NPCID.Everscream:
                case NPCID.Pumpking:
                case NPCID.MourningWood:
                case NPCID.DD2Betsy:
                case NPCID.DD2DarkMageT1:
                case NPCID.DD2DarkMageT3:
                case NPCID.DD2OgreT2:
                case NPCID.DD2OgreT3:
                // Misc
                case NPCID.WyvernHead:
                case NPCID.GoblinSummoner:
                case NPCID.PirateCaptain:
                case NPCID.HeadlessHorseman:
                case NPCID.Nailhead:
                    return true;
                default:
                    break;
            }

            switch (npc.aiStyle)
            {
                case NPCAIStyleID.BiomeMimic:
                    return true;
                default:
                    break;
            }

            return npc.IsChild(out NPC parent) && parent.whoAmI != npc.whoAmI && parent.IsMiniBoss();
        }

        // Проверяет, привязан ли этот NPC к пулу здоровья другого NPC.
        public static bool IsChild(this NPC npc, out NPC parent)
        {
            var child = npc.realLife >= 0 && npc.realLife <= Main.maxNPCs && npc.realLife != npc.whoAmI;
            parent = (child ? Main.npc[npc.realLife] : null);
            return child;
        }
    }
}