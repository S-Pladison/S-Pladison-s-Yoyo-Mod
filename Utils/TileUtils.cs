using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;

namespace SPYoyoMod.Utils
{
    public static class TileUtils
    {
        public const int TileSizeInPixels = 16;

        /// <summary>
        /// Производит поиск плитки по спирали изнутри наружу. Начальный путь с центра - вниз и налево и так по часовой.
        /// </summary>
        /// <param name="centerCoord">Координата плитки, откуда начинается поиск.</param>
        /// <param name="tilesFromCenter">Расстояние проверки от центра.</param>
        /// <param name="predicate">Условие поиска плитки.</param>
        /// <param name="tileCoord">Резулат поиска. Является координатой плитки.</param>
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
        /// <param name="tilesFromCenter">Расстояние проверки от центра.</param>
        /// <param name="predicate">Условие поиска плитки.</param>
        /// <param name="tileCoord">Резулат поиска. Является координатой плитки.</param>
        public static bool TryFindClosestTile(Point centerCoord, int tilesFromCenter, Predicate<Point> predicate, out Point tileCoord)
        {
            var tileInCircleList = new List<Point>(tilesFromCenter * tilesFromCenter + 1);

            // Работаем лишь с 1/4 круга (верхняя левая)
            for (var x = -tilesFromCenter; x < 0; x++)
            {
                for (var y = -tilesFromCenter; y < 0; y++)
                {
                    var distance = Math.Sqrt(Math.Pow(x - centerCoord.X, 2) + Math.Pow(y - centerCoord.Y, 2));

                    if (distance <= tilesFromCenter)
                    {
                        for (var j = y; j < 0; j++)
                        {
                            tileInCircleList.Add(new(centerCoord.X + x, centerCoord.Y + j));
                            tileInCircleList.Add(new(centerCoord.X + x, centerCoord.Y - j));
                            tileInCircleList.Add(new(centerCoord.X - x, centerCoord.Y + j));
                            tileInCircleList.Add(new(centerCoord.X - x, centerCoord.Y - j));
                        }
                        break;
                    }
                }
            }

            // Нужно добавить отдельные тайлы из 'креста' круга, т.к. ранее мы их не добавляли
            for (var x = centerCoord.X - tilesFromCenter; x <= centerCoord.X + tilesFromCenter; x++)
            {
                for (var y = centerCoord.Y - tilesFromCenter; y <= centerCoord.Y + tilesFromCenter; y++)
                {
                    tileInCircleList.Add(new(x, y));
                }
            }

            var tileInfo = tileInCircleList
                .Select(p => new Tuple<Point, double>(p, Math.Sqrt(Math.Pow(p.X - centerCoord.X, 2) + Math.Pow(p.Y - centerCoord.Y, 2))))
                .Where(t => WorldGen.InWorld(t.Item1.X, t.Item1.Y) && predicate(t.Item1));

            if (!tileInfo.Any())
            {
                tileCoord = default;
                return false;
            }

            tileCoord = tileInfo.Aggregate((min, next) => min.Item2 < next.Item2 ? min : next).Item1;
            return true;
        }
    }
}