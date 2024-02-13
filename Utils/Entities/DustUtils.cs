using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace SPYoyoMod.Utils.Entities
{
    public static class DustUtils
    {
        public static void SpawnDustCircle(Vector2 center, float radius, int count, int type, Action<Dust> onSpawn = null)
        {
            for (var i = 0; i < count; i++)
            {
                var position = center + new Vector2(radius, 0).RotatedBy(i / (float)count * MathHelper.TwoPi);
                var dust = Dust.NewDustPerfect(position, type);
                onSpawn?.Invoke(dust);
            }
        }

        public static void SpawnDustCircle(Vector2 center, float radius, int count, Func<int, int> type, Action<Dust, int, float> onSpawn = null)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)count * MathHelper.TwoPi;
                var position = center + new Vector2(radius, 0).RotatedBy(angle);
                var dustType = type?.Invoke(i) ?? -1;

                if (dustType != -1)
                {
                    var dust = Dust.NewDustPerfect(position, dustType);
                    onSpawn?.Invoke(dust, i, angle);
                }
            }
        }
    }
}