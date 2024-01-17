namespace SPYoyoMod.Common.Renderers
{
    public class TrailRenderer
    {

        /*public class LineRenderer
        {
            private struct Point
            {
                public Vector2 Pos;
                public float Width;
                public Color Color;
                public float LengthFromPrevPoint;
            }

            private struct Segment
            {
                public LineRenderer.Point A;
                public LineRenderer.Point B;
            }

            public int PointCount { get; init; }

            private PrimitiveRenderer renderer;
            private LineRenderer.Point[] points;

            public LineRenderer(int pointCount)
            {
                PointCount = pointCount;

                renderer = new PrimitiveRenderer(2 * PointCount, 6 * PointCount - 6);
                points = new LineRenderer.Point[PointCount];
                vertices = new VertexPositionColorTexture[PointCount * 2];
                indices = new short[PointCount * 6 - 6];
            }

            public LineRenderer SetPoints(IList<Vector2> points)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    this.points[i].Pos = points[i];
                }

                RecalculateMesh();

                return this;
            }

            /*public LineRenderer SetPoints(Vector2[] points)
            {
                if (points.Length != PointCount) throw new ArgumentException($"Points length is not equal to PointCount...");

                this.points = points;

                RecreateMesh();

                return this;
            }

            public void Draw(Asset<Effect> effect)
            {
                Draw(effect.Value);
            }

            public void Draw(Effect effect)
            {
                renderer.Draw(effect);
            }

            private void RecalculateMesh()
            {
                var lineLength = 0f;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    ref var firstPoint = ref points[i];
                    ref var secondPoint = ref points[i + 1];

                    var distance = Vector2.Distance(firstPoint.Pos, secondPoint.Pos);
                    secondPoint.LengthFromPrevPoint = distance;
                    lineLength += distance;
                }

                // special case: single segment
                if (_points.length == 2)
                {

                }

                var distanceSoFar = 0f;

                for (int i = 0; i < points.Length - 1; i++)
                {
                    ref var firstPoint = ref points[i];
                    ref var secondPoint = ref points[i + 1];

                    distanceSoFar += secondPoint.LengthFromPrevPoint;

                    firstPoint.Color = Color.White;
                    secondPoint.Color = Color.White;

                    firstPoint.Width = 16f;
                    secondPoint.Width = 16f;

                    if (i == 0)
                    {

                    }
                    else
                    {

                    }
                }

                renderer.SetVertices(vertices);
                renderer.SetIndices(indices);
            }

            private void AddVertex(int index, Vector2 position, Vector2 texCoord, Color color)
            {
                vertices[index].Position = new Vector3(position, 0);
                vertices[index].Color = color;
                vertices[index].TextureCoordinate = texCoord;
            }
        }*/

        /*public class LineRenderer
        {
            private static readonly WidthDelegate DefaultWidthFunc;
            private static readonly ColorDelegate DefaultColorFunc;

            static LineRenderer()
            {
                DefaultWidthFunc = (_) => 16f;
                DefaultColorFunc = (_) => Color.White;
            }

            public int PointCount { get; init; }
            public bool Loop { get; init; }

            private PrimitiveRenderer renderer;
            private Vector2[] points;
            private VertexPositionColorTexture[] vertices;
            private short[] indices;

            private WidthDelegate widthFunc;
            private ColorDelegate colorFunc;

            public LineRenderer(int pointCount, bool loop)
            {
                PointCount = pointCount;
                Loop = loop;

                renderer = new PrimitiveRenderer(2 * PointCount, 6 * PointCount - 6);
                points = new Vector2[PointCount];
                vertices = new VertexPositionColorTexture[PointCount * 2];
                indices = new short[PointCount * 6 - 6];

                widthFunc = DefaultWidthFunc;
                colorFunc = DefaultColorFunc;
            }

            public LineRenderer SetWidth(WidthDelegate widthFunc)
            {
                this.widthFunc = widthFunc;
                return this;
            }

            public LineRenderer SetColor(ColorDelegate colorFunc)
            {
                this.colorFunc = colorFunc;
                return this;
            }

            public LineRenderer SetPoints(IList<Vector2> points)
            {
                return SetPoints(points.ToArray());
            }

            public LineRenderer SetPoints(Vector2[] points)
            {
                if (points.Length != PointCount) throw new ArgumentException($"Points length is not equal to PointCount...");

                this.points = points;

                RecreateMesh();

                return this;
            }

            public void Draw(Asset<Effect> effect)
            {
                Draw(effect.Value);
            }

            public void Draw(Effect effect)
            {
                renderer.Draw(effect);
            }

            private void RecreateMesh()
            {
                var vCount = 0;
                var iCount = 0;

                void AddVertex(Vector2 position, Color color, Vector2 textureCoords)
                {
                    vertices[vCount].Position = new Vector3(position, 0);
                    vertices[vCount].Color = color;
                    vertices[vCount].TextureCoordinate = textureCoords;
                    vCount++;
                }

                void AddIndex(int value)
                {
                    indices[iCount] = (short)value;
                    iCount++;
                }

                (Color, Vector2) GetProgressVariables(float progress, int index = 1)
                {
                    var width = widthFunc(progress);
                    var color = colorFunc(progress);
                    var normal = (points[index] - points[index - 1]).SafeNormalize(Vector2.Zero).RotatedBy(MathHelper.PiOver2);
                    var offset = normal * width / 2f;

                    return (color, offset);
                }

                var length = 0f;
                var distances = new float[points.Length - 1];

                for (int i = 1; i < points.Length; i++)
                {
                    var j = i - 1;
                    distances[j] = Vector2.DistanceSquared(points[j], points[i]);
                    length += distances[j];
                }

                var progress = 0f;
                (var color, var offset) = GetProgressVariables(0);

                AddVertex(points[0] - offset, color, new Vector2(progress, 0));
                AddVertex(points[0] + offset, color, new Vector2(progress, 1));

                var nextIndex = 0;

                for (int i = 1; i < points.Length; i++)
                {
                    progress += distances[i - 1] / length;
                    (color, offset) = GetProgressVariables(progress, i);

                    AddVertex(points[i] - offset, color, new Vector2(progress, 0));
                    AddVertex(points[i] + offset, color, new Vector2(progress, 1));

                    var i2 = nextIndex + i * 2 - 2;

                    AddIndex(i2);
                    AddIndex(i2 + 2);
                    AddIndex(i2 + 1);
                    AddIndex(i2 + 2);
                    AddIndex(i2 + 3);
                    AddIndex(i2 + 1);
                }

                renderer.SetVertices(vertices);
                renderer.SetIndices(indices);
            }

            public delegate float WidthDelegate(float factorFromStartToEnd);
            public delegate Color ColorDelegate(float factorFromStartToEnd);
        }*/
    }
}