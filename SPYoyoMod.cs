using SPYoyoMod.Core;
using SPYoyoMod.Core.Netcode;
using System;
using System.Collections.Generic;
using System.IO;
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
            var typesToLoad = LoadOrder.Sort(
                from t in AssemblyManager.GetLoadableTypes(Code)
                where !t.IsAbstract && !t.ContainsGenericParameters
                where t.IsAssignableTo(typeof(ILoadable))
                where t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null
                where AutoloadAttribute.GetValue(t).NeedsAutoloading
                select t
            );

            var loadedInstances = new List<ILoadable>(typesToLoad.Count);

            LoaderUtils.ForEachAndAggregateExceptions(typesToLoad, t =>
            {
                loadedInstances.Add(Activator.CreateInstance(t, true) as ILoadable);
            });

            foreach (var instance in loadedInstances)
                AddContent(instance);
        }

        public override void HandlePacket(BinaryReader reader, int sender)
        {
            NetHandler.Receive(reader, sender);
        }
    }
}