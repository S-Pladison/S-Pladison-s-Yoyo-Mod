using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Core.Graphics;
using SPYoyoMod.Utils;
using System;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Particles
{
    /// <summary>
    /// Базовый класс примитивной частицы.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public abstract class BaseParticle : IWorldParticle, ILoadable
    {
        private static readonly int _defaultLifeTime = ModUtils.SecondsToTicks(1);

        /// <summary>
        /// Мировая позиция частицы.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Скорость перемещения частицы.
        /// </summary>
        public Vector2 Velocity;

        private int _innerLifeTime = _defaultLifeTime;

        /// <summary>
        /// Время жизни, по истечении которого частица будет удалена.
        /// </summary>
        public int LifeTime { get => _innerLifeTime; set => _innerLifeTime = Math.Max(value, 1); }

        /// <summary>
        /// Прошедшее время с момента появления частицы в мире.
        /// </summary>
        public int ElapsedTime { get; private set; }

        /// <summary>
        /// Интерполированное значение (от 0 до 1) того, насколько продвинулась частица в своем жизненном цикле.
        /// </summary>
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

        /// <summary>
        /// Метод, вызываемый при обновлении частицы. Вызывается перед обновлением основной логики.
        /// </summary>
        protected virtual void OnUpdate() { }

        /// <summary>
        /// Уничтожить частицу.
        /// </summary>
        public void Despawn()
        {
            if (ShouldBeRemoved)
                return;

            ShouldBeRemoved = true;
        }
    }
}