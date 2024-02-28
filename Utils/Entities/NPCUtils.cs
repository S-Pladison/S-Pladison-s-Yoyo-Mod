using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;

namespace SPYoyoMod.Utils.Entities
{
    public static class NPCUtils
    {
        public static (NPC npc, float distance) NearestNPC(Vector2 center, float? radius = null, Predicate<NPC> predicate = null)
        {
            var targets = NearestNPCs(center, radius, predicate);
            return targets.Any() ? targets.First() : (null, 0);
        }

        public static List<(NPC npc, float distance)> NearestNPCs(Vector2 center, float? radius = null, Predicate<NPC> predicate = null)
        {
            List<(NPC npc, float distance)> result = new();

            foreach (var npc in Main.npc)
            {
                if (!predicate?.Invoke(npc) ?? false) continue;

                var distance = Vector2.Distance(center, npc.Center);

                if (!radius.HasValue || distance <= radius)
                {
                    result.Add((npc, distance));
                }
            }

            result.Sort((x, y) => x.distance.CompareTo(y.distance));

            return result;
        }
    }

    public static class NPCExtensions
    {
        public static bool IsBossOrRelated(this NPC npc)
        {
            return npc.IsBoss() || npc.IsBossLimb() || npc.IsMiniBoss();
        }

        public static bool IsBoss(this NPC npc)
        {
            var type = npc.type;

            switch (type)
            {
                case NPCID.EaterofWorldsHead:
                case NPCID.EaterofWorldsBody:
                case NPCID.EaterofWorldsTail:

                case NPCID.DungeonGuardian:
                    return true;
            }

            if (npc.IsChild(out NPC parent) && parent.whoAmI != npc.whoAmI && parent.IsBoss())
            {
                return true;
            }

            return npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[type];
        }

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
                case NPCID.MoonLordHand:
                case NPCID.MoonLordHead:
                    return true;
                default:
                    return false;
            }
        }

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
            }

            if (npc.aiStyle == NPCAIStyleID.BiomeMimic) return true;

            return npc.IsChild(out NPC parent) && parent.whoAmI != npc.whoAmI && parent.IsMiniBoss();
        }

        public static bool IsChild(this NPC npc, out NPC parent)
        {
            var child = npc.realLife >= 0 && npc.realLife <= Main.maxNPCs && npc.realLife != npc.whoAmI;
            parent = (child ? Main.npc[npc.realLife] : null);
            return child;
        }
    }
}