using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SPYoyoMod.Core
{
    /// <summary>
    /// Указывает, что тип должен быть загружен перед заданными типами.
    /// Без аргументов — перед всеми остальными (кроме других типов с тем же ограничением).
    /// Учитываются сами типы, их наследники и реализации.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class LoadBeforeAttribute(params Type[] types) : Attribute
    {
        public readonly Type[] Types = types ?? [];
    }

    /// <summary>
    /// Указывает, что тип должен быть загружен после заданных типов.
    /// Без аргументов — после всех остальных (кроме других типов с тем же ограничением).
    /// Учитываются сами типы, их наследники и реализации.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class LoadAfterAttribute(params Type[] types) : Attribute
    {
        public readonly Type[] Types = types ?? [];
    }

    /// <summary>
    /// Указывает, что тип должен быть загружен между двумя другими:
    /// после <paramref name="after"/> и перед <paramref name="before"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class LoadBetweenAttribute(Type after, Type before) : Attribute
    {
        public readonly Type After = after;
        public readonly Type Before = before;
    }

    internal static class LoadOrder
    {
        public static List<Type> Sort(IEnumerable<Type> types)
        {
            var list = types as IList<Type> ?? types.ToList();

            if (list.Count <= 1)
                return [.. list];

            var successors = new Dictionary<Type, HashSet<Type>>(list.Count);
            var indegree = new Dictionary<Type, int>(list.Count);
            var beforeAll = new HashSet<Type>();
            var afterAll = new HashSet<Type>();

            foreach (var type in list)
            {
                successors[type] = [];
                indegree[type] = 0;
            }

            void AddEdge(Type from, Type to)
            {
                if (from is null || to is null || from == to)
                    return;

                if (!successors.ContainsKey(from) || !successors.ContainsKey(to))
                    return;

                if (successors[from].Add(to))
                    indegree[to]++;
            }

            foreach (var type in list)
            {
                foreach (var attr in GetAttributes<LoadBeforeAttribute>(type))
                {
                    if (attr.Types.Length == 0)
                    {
                        beforeAll.Add(type);
                        continue;
                    }

                    foreach (var target in attr.Types)
                    {
                        foreach (var other in Resolve(target, list, type))
                            AddEdge(type, other);
                    }
                }

                foreach (var attr in GetAttributes<LoadAfterAttribute>(type))
                {
                    if (attr.Types.Length == 0)
                    {
                        afterAll.Add(type);
                        continue;
                    }

                    foreach (var target in attr.Types)
                    {
                        foreach (var other in Resolve(target, list, type))
                            AddEdge(other, type);
                    }
                }

                foreach (var attr in GetAttributes<LoadBetweenAttribute>(type))
                {
                    foreach (var other in Resolve(attr.After, list, type))
                        AddEdge(other, type);

                    foreach (var other in Resolve(attr.Before, list, type))
                        AddEdge(type, other);
                }

                if (beforeAll.Contains(type) && afterAll.Contains(type))
                {
                    throw new InvalidOperationException(
                        $"Type '{type.FullName}' cannot have both parameterless {nameof(LoadBeforeAttribute)} and {nameof(LoadAfterAttribute)}."
                    );
                }
            }

            foreach (var type in beforeAll)
            {
                foreach (var other in list)
                {
                    if (!beforeAll.Contains(other))
                        AddEdge(type, other);
                }
            }

            foreach (var type in afterAll)
            {
                foreach (var other in list)
                {
                    if (!afterAll.Contains(other))
                        AddEdge(other, type);
                }
            }

            var ready = new PriorityQueue<Type, string>(StringComparer.InvariantCulture);

            foreach (var type in list)
            {
                if (indegree[type] == 0)
                    ready.Enqueue(type, type.FullName ?? type.Name);
            }

            var result = new List<Type>(list.Count);

            while (ready.TryDequeue(out var type, out _))
            {
                result.Add(type);

                foreach (var next in successors[type])
                {
                    indegree[next]--;

                    if (indegree[next] == 0)
                        ready.Enqueue(next, next.FullName ?? next.Name);
                }
            }

            if (result.Count != list.Count)
            {
                throw new InvalidOperationException(
                    "Cyclic load order dependency detected: " + FormatCycle(successors, list, result)
                );
            }

            return result;
        }

        private static IEnumerable<T> GetAttributes<T>(Type type) where T : Attribute
        {
            foreach (var attr in type.GetCustomAttributes<T>(inherit: true))
                yield return attr;

            foreach (var iface in type.GetInterfaces())
            {
                foreach (var attr in iface.GetCustomAttributes<T>(inherit: false))
                    yield return attr;
            }
        }

        private static IEnumerable<Type> Resolve(Type specified, IList<Type> all, Type self)
        {
            if (specified is null)
                yield break;

            foreach (var type in all)
            {
                if (type != self && Matches(type, specified))
                    yield return type;
            }
        }

        private static bool Matches(Type candidate, Type specified)
        {
            if (specified.IsAssignableFrom(candidate))
                return true;

            if (!specified.IsGenericTypeDefinition)
                return false;

            for (var type = candidate; type is not null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == specified)
                    return true;
            }

            foreach (var iface in candidate.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == specified)
                    return true;
            }

            return false;
        }

        private static string FormatCycle(Dictionary<Type, HashSet<Type>> successors, IList<Type> all, List<Type> sorted)
        {
            var remaining = new HashSet<Type>(all);
            remaining.ExceptWith(sorted);

            if (TryFindCycle(successors, remaining, out var cycle))
                return string.Join(" -> ", cycle.Select(type => type.FullName ?? type.Name));

            return string.Join(", ", remaining.Select(type => type.FullName ?? type.Name));
        }

        private static bool TryFindCycle(Dictionary<Type, HashSet<Type>> successors, HashSet<Type> remaining, out List<Type> cycle)
        {
            const byte white = 0;
            const byte gray = 1;
            const byte black = 2;

            var color = remaining.ToDictionary(type => type, _ => white);
            var parent = new Dictionary<Type, Type>();
            List<Type> found = null;

            bool Dfs(Type current)
            {
                color[current] = gray;

                foreach (var next in successors[current])
                {
                    if (!remaining.Contains(next))
                        continue;

                    if (color[next] == gray)
                    {
                        found = [next];

                        for (var type = current; type != next; type = parent[type])
                            found.Add(type);

                        found.Add(next);
                        found.Reverse();
                        return true;
                    }

                    if (color[next] == white)
                    {
                        parent[next] = current;

                        if (Dfs(next))
                            return true;
                    }
                }

                color[current] = black;
                return false;
            }

            foreach (var type in remaining)
            {
                if (color[type] == white && Dfs(type))
                {
                    cycle = found;
                    return true;
                }
            }

            cycle = null;
            return false;
        }
    }
}
