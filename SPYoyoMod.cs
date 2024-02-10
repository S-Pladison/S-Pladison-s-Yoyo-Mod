using SPYoyoMod.Common.Configs;
using SPYoyoMod.Common.ModCompatibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;

namespace SPYoyoMod
{
    public class SPYoyoMod : Mod
    {
        public SPYoyoMod()
        {
            ContentAutoloadingEnabled = false;
        }

        public override void Load()
        {
            var modCompatibilityInstances = new List<ILoadable>();
            var otherInstances = new List<ILoadable>();

            // Load config

            AddConfig("ClientSideConfig", new ClientSideConfig());
            AddConfig("ServerSideConfig", new ServerSideConfig());

            // Load content
            // Code from Mod.Autoload()

            LoaderUtils.ForEachAndAggregateExceptions((
                from t in AssemblyManager.GetLoadableTypes(Code)
                where !t.IsAbstract && !t.ContainsGenericParameters
                where t.IsAssignableTo(typeof(ILoadable))
                where t.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes) != null
                where AutoloadAttribute.GetValue(t).NeedsAutoloading
                select t).OrderBy((Type type) => type.FullName, StringComparer.InvariantCulture), delegate (Type t)
                {
                    var instance = (ILoadable)Activator.CreateInstance(t, true);

                    if (instance is ModCompatibility) modCompatibilityInstances.Add(instance);
                    else otherInstances.Add(instance);
                }
            );

            foreach (var instance in modCompatibilityInstances) AddContent(instance);
            foreach (var instance in otherInstances) AddContent(instance);
        }
    }
}