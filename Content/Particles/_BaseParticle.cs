using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Graphics;

namespace SPYoyoMod.Content.Particles
{
    public abstract class BaseParticle(int lifeTime, bool pixelated) : IWorldParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;

        public int LifeTime { get; init; } = lifeTime;
        public int TimeLeft { get; private set; } = lifeTime;
        public bool IsPixelated { get; private set; } = pixelated;
        public bool ShouldBeRemoved { get; private set; } = false;

        public void Update()
        {
            OnUpdate();

            Position += Velocity;
            TimeLeft--;

            if (TimeLeft <= 0)
                Despawn();
        }

        public abstract void Draw(Vector2 screenPosition);

        protected virtual void OnUpdate() { }

        public void Despawn()
        {
            if (ShouldBeRemoved)
                return;

            ShouldBeRemoved = true;
        }
    }
}
