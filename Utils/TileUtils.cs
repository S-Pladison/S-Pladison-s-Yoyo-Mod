using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace SPYoyoMod.Utils
{
    public static class TileUtils
    {
        /// <summary>
        /// Размер одной плитки в пикселях.
        /// </summary>
        public const int TileSizeInPixels = 16;

        /// <summary>
        /// Производит поиск плитки по спирали изнутри наружу. Начальный путь с центра - вниз и налево и так по часовой.
        /// </summary>
        /// <param name="centerCoord">Координата плитки, откуда начинается поиск.</param>
        /// <param name="tilesFromCenter">Расстояние проверки от центра.</param>
        /// <param name="predicate">Условие поиска плитки.</param>
        /// <param name="tileCoord">Резулат поиска.</param>
        public static bool TryFindTileSpiralTraverse(Point centerCoord, int tilesFromCenter, Predicate<Point> predicate, out Point tileCoord)
        {
            tileCoord = default;
            tilesFromCenter = Math.Max(tilesFromCenter, 0);

            int tileCheckCount = 0;
            int width = tilesFromCenter * 2 + 1;

            // Направления движения
            int[] dirX = [0, 1, 0, -1];
            int[] dirY = [1, 0, -1, 0];

            // Начальная позиция
            int tileX = centerCoord.X;
            int tileY = centerCoord.Y;

            // Проверяем текущую плитку на позиции [tileX, tileY]
            bool CheckTile()
            {
                tileCheckCount++;
                return WorldGen.InWorld(tileX, tileY) && predicate(new(tileX, tileY));
            }

            // Проверка центральной плитки
            if (CheckTile())
            {
                tileCoord = new Point(tileX, tileY);
                return true;
            }

            int direction = 0;
            int steps = 1;
            int stepsTaken = 0;
            int stepsInCurrentDirection = 0;

            while (tileCheckCount < width * width)
            {
                tileX += dirX[direction];
                tileY += dirY[direction];

                stepsTaken++;
                stepsInCurrentDirection++;

                if (CheckTile())
                {
                    tileCoord = new Point(tileX, tileY);
                    return true;
                }

                if (stepsInCurrentDirection == steps)
                {
                    direction = (direction + 1) % 4;
                    stepsInCurrentDirection = 0;

                    if (direction == 0 || direction == 2)
                    {
                        steps++;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Производит поиск ближайшей плитки в определенном радиусе от центра.
        /// </summary>
        /// <param name="centerCoord">Координата плитки, откуда начинается поиск.</param>
        /// <param name="tilesFromCenter">Радиус проверки.</param>
        /// <param name="predicate">Условие поиска плитки.</param>
        /// <param name="tileCoord">Резулат поиска.</param>
        public static bool TryFindClosestTile(Point centerCoord, int tilesFromCenter, Predicate<Point> predicate, out Point tileCoord)
        {
            tileCoord = default;
            tilesFromCenter = Math.Max(tilesFromCenter, 0);

            var found = false;
            var closestDistSq = int.MaxValue;
            var radiusSq = tilesFromCenter * tilesFromCenter;

            for (var y = -tilesFromCenter; y <= tilesFromCenter; y++)
            {
                for (var x = -tilesFromCenter; x <= tilesFromCenter; x++)
                {
                    var distSq = x * x + y * y;

                    if (distSq > radiusSq || distSq >= closestDistSq)
                        continue;

                    var point = new Point(centerCoord.X + x, centerCoord.Y + y);

                    if (!WorldGen.InWorld(point.X, point.Y) || !predicate(point))
                        continue;

                    closestDistSq = distSq;
                    tileCoord = point;
                    found = true;

                    if (distSq == 0)
                        return true;
                }
            }

            return found;
        }
    }
}