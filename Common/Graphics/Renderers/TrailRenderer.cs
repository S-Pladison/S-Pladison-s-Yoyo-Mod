using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using SPYoyoMod.Utils.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace SPYoyoMod.Common.Graphics.Renderers
{
    public class TrailRenderer
    {
        public int MaxPoints
        {
            get => innerMaxPoints;
            set => SetMaxPoints(value);
        }

        public StripWidthDelegate Width
        {
            get;
            set;
        }

        public IReadOnlyList<Vector2> Points
        {
            get => innerPoints;
        }

        private int innerMaxPoints;
        private List<Vector2> innerPoints;

        public TrailRenderer(int maxPoints, StripWidthDelegate width)
        {
            innerPoints = new List<Vector2>(maxPoints);
            innerMaxPoints = maxPoints;

            Width = width;
        }

        public TrailRenderer SetMaxPoints(int maxPoints)
        {
            if (innerMaxPoints.Equals(maxPoints))
                return this;

            innerMaxPoints = maxPoints;
            innerPoints = innerPoints.Take(maxPoints).ToList();
            return this;
        }

        public TrailRenderer SetWidth(StripWidthDelegate width)
        {
            Width = width;
            return this;
        }

        public TrailRenderer SetNextPoint(Vector2 point)
        {
            innerPoints.Insert(0, point);

            if (innerPoints.Count >= innerMaxPoints)
                innerPoints.RemoveAt(innerMaxPoints - 1);

            return this;
        }

        public void Draw(Asset<Effect> effect)
        {
            Draw(effect.Value);
        }

        public void Draw(Effect effect)
        {
            DrawUtils.DrawPrimitiveStrip(effect, innerPoints, Width, false);
        }
    }
}