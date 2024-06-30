using System;
using System.Collections.Generic;
using System.Linq;
using Terraria.ModLoader;

namespace SPYoyoMod.Common.Graphics.DrawLayers
{
    /// <summary>
    /// Класс предоставляет возможность рисовать вещи на разных этапах отрисовки игры.<br/>
    /// Все доступные ванильные слои доступны в классе <see cref="VanillaDrawLayers"/>.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public abstract class GameDrawLayer : ModType
    {
        private readonly List<GameDrawLayer> _childrenBefore = new();
        private readonly List<GameDrawLayer> _childrenAfter = new();

        public bool Visible { get; private set; } = true;
        public IReadOnlyList<GameDrawLayer> ChildrenBefore => _childrenBefore;
        public IReadOnlyList<GameDrawLayer> ChildrenAfter => _childrenAfter;

        public void DrawWithChildren()
        {
            if (!Visible)
                return;

            foreach (var child in ChildrenBefore)
                child.DrawWithChildren();

            Draw();

            foreach (var child in ChildrenAfter)
                child.DrawWithChildren();
        }

        public abstract Position GetDefaultPosition();

        public virtual bool GetDefaultVisibility()
            => true;
        
        public sealed override void SetupContent()
            => SetStaticDefaults();
        
        public override string ToString()
            => Name;

        protected abstract void Draw();

        protected sealed override void Register()
        {
            ModTypeLookup<GameDrawLayer>.Register(this);
            GameDrawLayerLoader.Add(this);
        }

        private void AddChildBefore(GameDrawLayer child)
        => _childrenBefore.Add(child);

        private void AddChildAfter(GameDrawLayer child)
        => _childrenAfter.Add(child);

        private void ResetVisibility()
        {
            foreach (var child in ChildrenBefore)
                child.ResetVisibility();

            Visible = GetDefaultVisibility();

            foreach (var child in ChildrenAfter)
                child.ResetVisibility();
        }

        public abstract class Position { }

        public sealed class BeforeParent : Position
        {
            public GameDrawLayer Parent { get; }

            public BeforeParent(GameDrawLayer parent)
            {
                Parent = parent;
            }
        }

        public sealed class AfterParent : Position
        {
            public GameDrawLayer Parent { get; }

            public AfterParent(GameDrawLayer parent)
            {
                Parent = parent;
            }
        }

        [Autoload(Side = ModSide.Client)]
        private class GameDrawLayerLoader : ILoadable
        {
            private static readonly List<GameDrawLayer> _layers = new();
            private static GameDrawLayer[] _rootLayers = Array.Empty<GameDrawLayer>();

            public static void Add(GameDrawLayer layer)
            {
                _layers.Add(layer);
            }

            private static void DefineLayerPositions()
            {
                Dictionary<GameDrawLayer, Position> positions = _layers.ToDictionary(l => l, l => l.GetDefaultPosition());

                foreach (var (layer, position) in positions.ToArray())
                {
                    if (position is null)
                        continue;

                    if (position is BeforeParent before)
                    {
                        before.Parent.AddChildBefore(layer);
                        positions.Remove(layer);
                        continue;
                    }

                    if (position is AfterParent after)
                    {
                        after.Parent.AddChildAfter(layer);
                        positions.Remove(layer);
                        continue;
                    }

                    throw new ArgumentException($"GameDrawLayer {layer} has unknown Position type {position}");
                }

                _rootLayers = positions.Select(l => l.Key).ToArray();
            }

            private static void ResetVisibility()
            {
                foreach (var layer in _rootLayers)
                    layer.ResetVisibility();
            }

            void ILoadable.Load(Mod mod)
            {
                ModEvents.OnPostSetupContent += DefineLayerPositions;
                ModEvents.OnPreDraw += ResetVisibility;
            }

            void ILoadable.Unload()
            {
                ModEvents.OnPreDraw -= ResetVisibility;
                ModEvents.OnPostSetupContent -= DefineLayerPositions;

                _rootLayers = null;
                _layers.Clear();
            }
        }
    }
}