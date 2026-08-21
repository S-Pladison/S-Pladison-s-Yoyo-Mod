using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Verlet-цепь из узлов с фиксированной длиной звена.
    /// Один вызов <see cref="Simulate"/> — один физический тик.
    /// </summary>
    public sealed class PhysicalChain
    {
        private const float MinDistanceBetweenNodes = 0.1f;
        private const float MinConstraintLength = 0.0001f;

        public struct Node(Vector2 position, bool locked = false)
        {
            public Vector2 Position = position;
            public Vector2 OldPosition = position;
            public bool Locked = locked;
        }

        private Node[] _nodes = [];
        private int _nodeCount;
        private float _distanceBetweenNodes = MinDistanceBetweenNodes;
        private float _damping = 0.99f;

        /// <summary>
        /// Целевая длина звена между соседними узлами.
        /// </summary>
        public float DistanceBetweenNodes
        {
            get => _distanceBetweenNodes;
            set => _distanceBetweenNodes = MathHelper.Max(value, MinDistanceBetweenNodes);
        }

        /// <summary>
        /// Смещение, добавляемое свободным узлам за один тик.
        /// </summary>
        public Vector2 Gravity { get; set; }

        /// <summary>
        /// Коэффициент сохранения скорости (0 — сразу останавливается, 1 — без затухания).
        /// </summary>
        public float Damping
        {
            get => _damping;
            set => _damping = MathHelper.Clamp(value, 0f, 1f);
        }

        /// <summary>
        /// Кол-во узлов в цепи.
        /// </summary>
        public int NodeCount => _nodeCount;

        public ref Node this[int index]
        {
            get
            {
                if ((uint)index >= (uint)_nodeCount)
                    throw new ArgumentOutOfRangeException(nameof(index));

                return ref _nodes[index];
            }
        }

        /// <summary>
        /// Первый узел цепи.
        /// </summary>
        public ref Node First => ref this[0];

        /// <summary>
        /// Последний узел цепи.
        /// </summary>
        public ref Node Last => ref this[_nodeCount - 1];

        public PhysicalChain(IReadOnlyList<Node> nodes = null)
        {
            Setup(nodes);
        }

        /// <summary>
        /// Создать цепь между двумя точками с заданной длиной звена.
        /// </summary>
        public static PhysicalChain CreateBetween(Vector2 start, Vector2 end, float segmentLength)
        {
            segmentLength = MathHelper.Max(segmentLength, MinDistanceBetweenNodes);

            var length = Vector2.Distance(start, end);
            var nodeCount = Math.Max((int)MathF.Round(length / segmentLength) + 1, 2);
            var nodes = new Node[nodeCount];

            for (var i = 0; i < nodeCount; i++)
            {
                var t = i / (float)(nodeCount - 1);
                nodes[i] = new Node(Vector2.Lerp(start, end, t));
            }

            return new PhysicalChain(nodes)
            {
                DistanceBetweenNodes = segmentLength
            };
        }

        /// <summary>
        /// Задать узлы цепи. Переданные значения копируются.
        /// </summary>
        public void Setup(IReadOnlyList<Node> nodes)
        {
            if (nodes is null || nodes.Count == 0)
            {
                _nodeCount = 0;
                return;
            }

            if (_nodes.Length < nodes.Count)
                _nodes = new Node[nodes.Count];

            for (var i = 0; i < nodes.Count; i++)
                _nodes[i] = nodes[i];

            _nodeCount = nodes.Count;
        }

        /// <summary>
        /// Просимулировать один тик без принудительного пина концов.
        /// </summary>
        public void Simulate(int iterations)
            => Simulate(null, null, iterations);

        /// <summary>
        /// Просимулировать один тик. Переданные позиции удерживают соответствующие концы только на время этого тика.
        /// </summary>
        public void Simulate(Vector2? startPosition, Vector2? endPosition, int iterations)
        {
            if (_nodeCount == 0)
                return;

            var startWasLocked = false;
            var endWasLocked = false;

            if (startPosition is Vector2 start)
            {
                ref var node = ref _nodes[0];
                startWasLocked = node.Locked;
                node.Position = start;
                node.OldPosition = start;
                node.Locked = true;
            }

            if (endPosition is Vector2 end)
            {
                ref var node = ref _nodes[_nodeCount - 1];
                endWasLocked = node.Locked;
                node.Position = end;
                node.OldPosition = end;
                node.Locked = true;
            }

            Integrate();
            SolveConstraints(iterations);

            if (startPosition is not null)
                _nodes[0].Locked = startWasLocked;

            if (endPosition is not null)
                _nodes[_nodeCount - 1].Locked = endWasLocked;
        }

        public Enumerator GetEnumerator()
            => new(_nodes, _nodeCount);

        private void Integrate()
        {
            for (var i = 0; i < _nodeCount; i++)
            {
                ref var node = ref _nodes[i];

                if (node.Locked)
                    continue;

                var previous = node.Position;
                node.Position += (node.Position - node.OldPosition) * _damping + Gravity;
                node.OldPosition = previous;
            }
        }

        private void SolveConstraints(int iterations)
        {
            if (_nodeCount < 2)
                return;

            var restLength = _distanceBetweenNodes;

            for (var i = 0; i < iterations; i++)
            {
                if ((i & 1) == 0)
                {
                    for (var j = 0; j < _nodeCount - 1; j++)
                        Constrain(j, j + 1, restLength);
                }
                else
                {
                    for (var j = _nodeCount - 2; j >= 0; j--)
                        Constrain(j, j + 1, restLength);
                }
            }
        }

        private void Constrain(int firstIndex, int secondIndex, float restLength)
        {
            ref var first = ref _nodes[firstIndex];
            ref var second = ref _nodes[secondIndex];

            if (first.Locked && second.Locked)
                return;

            var delta = second.Position - first.Position;
            var distanceSquared = delta.LengthSquared();

            if (distanceSquared < MinConstraintLength * MinConstraintLength)
                return;

            var distance = MathF.Sqrt(distanceSquared);
            var error = (distance - restLength) / distance;

            if (first.Locked)
            {
                second.Position -= delta * error;
            }
            else if (second.Locked)
            {
                first.Position += delta * error;
            }
            else
            {
                var half = delta * (error * 0.5f);
                first.Position += half;
                second.Position -= half;
            }
        }

        public struct Enumerator
        {
            private readonly Node[] _nodes;
            private readonly int _count;
            private int _index;

            internal Enumerator(Node[] nodes, int count)
            {
                _nodes = nodes;
                _count = count;
                _index = -1;
            }

            public readonly Vector2 Current => _nodes[_index].Position;

            public bool MoveNext() => ++_index < _count;
        }
    }
}
