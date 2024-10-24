using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SPYoyoMod.Utils
{
    public sealed class PhysicalChain
    {
        public class Node(Vector2 position, bool locked)
        {
            public Vector2 Position = position;
            public Vector2 OldPosition = position;
            public bool Locked = locked;
        }

        private class Segment(Node first, Node second)
        {
            public readonly Node First = first;
            public readonly Node Second = second;
        }

        private float _innerDistanceBetweenNodes;

        private readonly List<Segment> _segments = [];

        public float DistanceBetweenNodes
        {
            get => _innerDistanceBetweenNodes;
            set => _innerDistanceBetweenNodes = MathHelper.Max(value, 0.1f);
        }

        public Vector2 Gravity
        {
            get;
            set;
        }

        public PhysicalChain(IList<Node> nodes = null)
        {
            Setup(nodes);
        }

        public void Setup(IList<Node> nodes)
        {
            _segments.Clear();

            if (nodes is null || nodes.Count <= 1)
                return;

            _segments.EnsureCapacity(nodes.Count);

            for (int i = 0; i < nodes.Count - 1; i++)
            {
                _segments.Add(new Segment(nodes[i], nodes[i + 1]));
            }
        }

        public IEnumerable<Vector2> GetPositions()
        {
            if (_segments.Count == 0)
                yield break;

            foreach (var segment in _segments)
                yield return segment.First.Position;

            yield return _segments[^1].Second.Position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Simulate(uint iterations)
            => Simulate(null, null, iterations);

        public void Simulate(Vector2? firstNodePosition, Vector2? secondNodePosition, uint iterations)
        {
            if (firstNodePosition is not null)
            {
                _segments[0].First.Position = firstNodePosition.Value;
                _segments[0].First.Locked = true;
            }

            if (secondNodePosition is not null)
            {
                _segments[^1].Second.Position = secondNodePosition.Value;
                _segments[^1].Second.Locked = true;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void SimulateNode(Node node)
            {
                if (node.Locked)
                    return;

                var positionBeforeUpdate = node.Position;

                node.Position += node.Position - node.OldPosition;
                node.Position += Gravity;

                node.OldPosition = positionBeforeUpdate;
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                SimulateNode(_segments[i].First);
            }

            SimulateNode(_segments[^1].Second);

            for (uint i = 0; i < iterations; i++)
            {
                for (int j = 0; j < _segments.Count; j++)
                {
                    var segment = _segments[j];
                    var center = (segment.First.Position + segment.Second.Position) / 2.0f;
                    var direction = Vector2.Normalize(segment.First.Position - segment.Second.Position);

                    if (!segment.First.Locked)
                        segment.First.Position = center + direction * DistanceBetweenNodes / 2.0f;

                    if (!segment.Second.Locked)
                        segment.Second.Position = center - direction * DistanceBetweenNodes / 2.0f;
                }
            }
        }
    }
}
