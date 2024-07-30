using SPYoyoMod.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod
{
    public sealed class SPYoyoMod : Mod
    {
        public SPYoyoMod()
        {
            ContentAutoloadingEnabled = false;
        }

        public override void Load()
        {
            var loadedInstances = new PriorityQueue<ILoadable, sbyte>(
                Comparer<sbyte>.Create((a, b) =>
                {
                    return b.CompareTo(a);
                })
            );

            LoaderUtils.ForEachAndAggregateExceptions((
                from t in AssemblyManager.GetLoadableTypes(Code)
                where !t.IsAbstract && !t.ContainsGenericParameters
                where t.IsAssignableTo(typeof(ILoadable))
                where t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null
                where AutoloadAttribute.GetValue(t).NeedsAutoloading
                select t).OrderBy((Type type) => type.FullName, StringComparer.InvariantCulture), delegate (Type t)
                {
                    loadedInstances.Enqueue(Activator.CreateInstance(t, true) as ILoadable, LoadPriorityAttribute.GetValue(t).Value);
                }
            );

            while (loadedInstances.TryDequeue(out var name, out var priority))
            {
                AddContent(name);
            }
        }
    }
}