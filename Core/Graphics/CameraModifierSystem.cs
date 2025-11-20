using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.CameraModifiers;
using Terraria.ModLoader;

namespace SPYoyoMod.Core.Graphics
{
    /// <summary>
    /// Система для управления модификаторами камеры. Является бесполезной обёрткой над <see cref="Main.instance.CameraModifiers"/>.
    /// </summary>
    public sealed class CameraModifierSystem : ILoadable
    {
        /// <summary>
        /// Коллекция стандартных модификаторов камеры.
        /// </summary>
        public static class Modifiers
        {
            /// <summary>
            /// Модификатор камеры, создающий эффект тряски.
            /// </summary>
            public sealed class Shake : ICameraModifier
            {
                private float _strength = 5.0f;
                private uint _frames = 30;
                private uint _timer = 30;

                /// <summary>
                /// Сила тряски.
                /// </summary>
                public float Strength { get => _strength; init => _strength = MathHelper.Clamp(value, 0f, 1f); }

                /// <summary>
                /// Количество кадров, в течение которых будет длиться тряска.
                /// </summary>
                public uint Frames
                {
                    get => _frames; init
                    {
                        _frames = value;
                        _timer = value;
                    }
                }

                /// <summary>
                /// Уникальный идентификатор модификатора. При наличии все остальные активные модификаторы камеры с тем же значением будут очищены.
                /// </summary>
                public string UniqueIdentity { get; init; }

                public bool Finished { get; private set; }

                public void Update(ref CameraInfo cameraInfo)
                {
                    if (Main.gamePaused)
                        return;

                    ref Vector2 cameraPosition = ref cameraInfo.CameraPosition;

                    cameraPosition += Terraria.Utils.NextVector2Circular(Main.rand, Strength, Strength);
                    _timer--;

                    if (_timer == 0)
                        Finished = true;
                }
            }
        }

        void ILoadable.Load(Mod mod)
        {
            // ...
        }

        void ILoadable.Unload()
        {
            // ...
        }

        /// <summary>
        /// Добавляет новый модификатор камеры типа <typeparamref name="T"/> в <see cref="Main.instance.CameraModifiers"/>.
        /// </summary>
        public static T Add<T>() where T : ICameraModifier, new()
        {
            var modifier = new T();

            Add(modifier);

            return modifier;
        }

        /// <summary>
        /// Добавляет указанный модификатор камеры в <see cref="Main.instance.CameraModifiers"/>.
        /// </summary>
        public static void Add(ICameraModifier modifier)
        {
            if (Main.dedServ)
                return;

            Main.instance.CameraModifiers.Add(modifier);
        }
    }
}
