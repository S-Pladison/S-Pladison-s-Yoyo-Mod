using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Utils;
using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Particles
{
    [Autoload(Side = ModSide.Client)]
    public abstract class BaseParticle : IWorldParticle, ILoadable
    {
        private static readonly int _defaultLifeTime = GeneralUtils.SecondsToTicks(1);

        public Vector2 Position;

        public Vector2 Velocity;

        private int _innerLifeTime = _defaultLifeTime;

        public int LifeTime { get => _innerLifeTime; set => _innerLifeTime = Math.Max(value, 1); }

        public int ElapsedTime { get; private set; }

        public float LifeTimeRatio { get => Math.Min(ElapsedTime / (float)LifeTime, 1.0f); }

        public bool ShouldBeRemoved { get; private set; } = false;

        public void Update()
        {
            OnUpdate();

            Position += Velocity;

            if (ElapsedTime++ >= LifeTime)
                Despawn();
        }

        public abstract void Draw(SpriteBatch spriteBatch, in Vector2 screenPosition);

        public virtual void Load(Mod mod) { }
        public virtual void Unload() { }

        protected virtual void OnUpdate() { }

        public void Despawn()
        {
            if (ShouldBeRemoved)
                return;

            ShouldBeRemoved = true;
        }
    }
}