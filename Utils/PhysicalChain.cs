using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;

namespace SPYoyoMod.Utils
{
    /// <summary>
    /// Класс физической цепи...
    /// </summary>
    public sealed class PhysicalChain
    {
        /// <summary>
        /// Узел цепи.
        /// </summary>
        public sealed class Node(Vector2 position, bool locked = false)
        {
            public Vector2 Position = position;
            public bool Locked = locked;

            internal Vector2 OldPosition = position;
        }

        /// <summary>
        /// Сегмент цепи между двумя узлами.
        /// </summary>
        private sealed class Segment(PhysicalChain.Node a, PhysicalChain.Node b)
        {
            public readonly Node A = a;
            public readonly Node B = b;
        }

        private readonly List<Segment> _segments = [];

        private float _restLength = 16f;
        private float _compliance = 0f;
        private float _damping = 1f;
        private int _solverIterations = 3;

        /// <summary>
        /// Длина сегмента в состоянии покоя.
        /// </summary>
        public float RestLength
        {
            get => _restLength;
            set => _restLength = MathF.Max(value, 0.001f);
        }

        /// <summary>
        /// Гравитация, применяемая к каждому узлу.
        /// </summary>
        public Vector2 Gravity { get; set; }

        /// <summary>
        /// XPBD Compliance — параметр мягкости.
        /// 0 = абсолютно жёсткая цепь.
        /// Рекомендуемый диапазон: 0..0.1
        /// </summary>
        public float Compliance
        {
            get => _compliance;
            set => _compliance = MathF.Max(value, 0f);
        }

        /// <summary>
        /// Количество итераций солвера.
        /// Отвечает за стабильность, а не жёсткость.
        /// Диапазон: 1..32
        /// </summary>
        public int SolverIterations
        {
            get => _solverIterations;
            set => _solverIterations = Math.Clamp(value, 1, 32);
        }

        /// <summary>
        /// Демпфирование скорости (затухание).
        /// 1 = без затухания.
        /// Диапазон: 0.01..1
        /// </summary>
        public float Damping
        {
            get => _damping;
            set => _damping = Math.Clamp(value, 0.01f, 1f);
        }

        /// <summary>
        /// Доступ к сегментам (readonly, для отладки / рендера).
        /// </summary>
        public IReadOnlyList<Node> Nodes
        {
            get
            {
                if (_segments.Count == 0)
                    return [];

                var list = new List<Node>(_segments.Count + 1);
                foreach (var s in _segments)
                    list.Add(s.A);

                list.Add(_segments[^1].B);
                return list;
            }
        }

        /// <summary>
        /// Получает позиции всех узлов цепи.
        /// </summary>
        public IEnumerable<Vector2> GetPositions()
        {
            if (_segments.Count == 0)
                yield break;

            foreach (var segment in _segments)
                yield return segment.A.Position;

            yield return _segments[^1].B.Position;
        }

        public PhysicalChain(IList<Node> nodes = null)
        {
            Setup(nodes);
        }

        /// <summary>
        /// Инициализация цепи из списка узлов.
        /// </summary>
        public void Setup(IList<Node> nodes)
        {
            _segments.Clear();

            if (nodes == null || nodes.Count < 2)
                return;

            _segments.EnsureCapacity(nodes.Count - 1);

            for (int i = 0; i < nodes.Count - 1; i++)
                _segments.Add(new Segment(nodes[i], nodes[i + 1]));
        }

        /// <summary>
        /// Устанавливает позицию первого узла цепи (начала) и фиксирует его.
        /// </summary>
        public void SetStart(Vector2 position)
        {
            if (_segments.Count == 0)
                return;

            var first = _segments[0].A;
            first.Position = position;
            first.OldPosition = position;
            first.Locked = true;
        }

        /// <summary>
        /// Устанавливает позицию последнего узла цепи (конца) и фиксирует его.
        /// </summary>
        public void SetEnd(Vector2 position)
        {
            if (_segments.Count == 0)
                return;

            var last = _segments[^1].B;
            last.Position = position;
            last.OldPosition = position;
            last.Locked = true;
        }

        /// <summary>
        /// Выполнение симуляции физики цепи.
        /// </summary>
        public void Simulate()
        {
            if (_segments.Count == 0)
                return;

            Integrate();
            SolveConstraints();
        }

        private void Integrate()
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void IntegrateNode(Node node)
            {
                if (node.Locked)
                    return;

                var current = node.Position;
                var velocity = (node.Position - node.OldPosition) * Damping;

                node.Position += velocity + Gravity;
                node.OldPosition = current;
            }

            for (int i = 0; i < _segments.Count; i++)
                IntegrateNode(_segments[i].A);

            IntegrateNode(_segments[^1].B);
        }

        private void SolveConstraints()
        {
            for (var i = 0; i < SolverIterations; i++)
            {
                foreach (var segment in _segments)
                {
                    var a = segment.A;
                    var b = segment.B;

                    var delta = b.Position - a.Position;
                    var dist = delta.Length();

                    if (dist <= 1e-6f)
                        continue;

                    var constraint = dist - RestLength;

                    var wA = a.Locked ? 0f : 1f;
                    var wB = b.Locked ? 0f : 1f;

                    var denom = wA + wB + Compliance;

                    if (denom == 0f)
                        continue;

                    var lambda = -constraint / denom;
                    var correction = lambda * delta / dist;

                    if (!a.Locked)
                        a.Position -= wA * correction;

                    if (!b.Locked)
                        b.Position += wB * correction;
                }
            }
        }
    }

    /// <summary>
    /// Утилиты для работы с физической цепью.
    /// </summary>
    public static class PhysicalChainUtils
    {
        /// <summary>
        /// Создаёт объект физической цепи между двумя точками, с заданной длиной сегмента.
        /// Начальный и конечный узлы автоматически фиксируются.
        /// </summary>
        /// <param name="start">Начальная точка цепи</param>
        /// <param name="end">Конечная точка цепи</param>
        /// <param name="segmentLength">Ожидаемая длина сегмента</param>
        public static PhysicalChain CreateBetweenTwoPoints(Vector2 start, Vector2 end, float segmentLength)
        {
            if (segmentLength <= 0f)
                throw new ArgumentException("Segment length must be greater than zero.", nameof(segmentLength));

            var delta = end - start;
            var totalLength = delta.Length();

            var segments = Math.Max(1, (int)MathF.Ceiling(totalLength / segmentLength));
            var direction = Terraria.Utils.SafeNormalize(delta, Vector2.Zero);

            var nodes = new List<PhysicalChain.Node>(segments);

            for (int i = 0; i < segments; i++)
            {
                var pos = start + direction * segmentLength * i;
                var locked = (i == 0 || i == segments);

                nodes.Add(new PhysicalChain.Node(pos, locked));
            }

            var chain = new PhysicalChain(nodes)
            {
                RestLength = segmentLength
            };

            return chain;
        }
    }
}