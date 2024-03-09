using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class YoyoProjectile : ModProjectile, IModifyYoyoStatsProjectile, IPostDrawYoyoStringProjectile
    {
        /// <summary>
        /// How long in seconds the yoyo will stay out before automatically returning to the player.
        /// Leaving as -1 will make the time infinite.
        /// </summary>
        public abstract float LifeTime { get; }

        /// <summary>
        /// The maximum distance a yoyo projectile can be from its owner in pixels.
        /// </summary>
        public abstract float MaxRange { get; }

        /// <summary>
        /// The maximum speed a yoyo projectile can go in pixels per tick.
        /// </summary>
        public abstract float TopSpeed { get; }

        public bool IsReturning { get => Projectile.ai[0] == -1; }
        public float ReturnToPlayerProgress { get; private set; }

        private Vector2? startToReturnPosition;

        public sealed override void SetStaticDefaults()
        {
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = LifeTime;
            ProjectileID.Sets.YoyosMaximumRange[Type] = MaxRange;
            ProjectileID.Sets.YoyosTopSpeed[Type] = TopSpeed;

            YoyoSetStaticDefaults();
        }

        public sealed override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.aiStyle = 99;
            Projectile.friendly = true;
            Projectile.penetrate = -1;

            YoyoSetDefaults();
        }

        public sealed override void OnSpawn(IEntitySource source)
        {
            var owner = Main.player[Projectile.owner];

            // ...

            YoyoOnSpawn(owner, source);
        }

        public sealed override bool PreAI()
        {
            var owner = Main.player[Projectile.owner];

            if (IsReturning)
            {
                if (!startToReturnPosition.HasValue)
                {
                    startToReturnPosition = Projectile.Center;
                }

                var progress = 1f - Vector2.DistanceSquared(owner.Center, Projectile.Center) / Vector2.DistanceSquared(owner.Center, startToReturnPosition.Value);
                ReturnToPlayerProgress = MathHelper.Clamp(progress, 0f, 1f);
            }

            return YoyoPreAI(owner);
        }

        public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var owner = Main.player[Projectile.owner];

            // ...

            YoyoOnHitNPC(owner, target, hit, damageDone);
        }

        public sealed override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            var owner = Main.player[Projectile.owner];

            // ...

            YoyoOnHitPlayer(owner, target, info);
        }

        public sealed override void SendExtraAI(BinaryWriter writer)
        {
            // ...

            YoyoSendExtraAI(writer);
        }

        public sealed override void ReceiveExtraAI(BinaryReader reader)
        {
            // ...

            YoyoReceiveExtraAI(reader);
        }

        void IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            ModifyYoyoStats(ref statModifiers);
        }

        void IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            PostDrawYoyoString(mountedCenter);
        }

        /// <inheritdoc cref="PreAI" />
        public virtual bool YoyoPreAI(Player owner) => true;

        /// <inheritdoc cref="SetStaticDefaults" />
        public virtual void YoyoSetStaticDefaults() { }

        /// <inheritdoc cref="SetDefaults" />
        public virtual void YoyoSetDefaults() { }

        /// <inheritdoc cref="OnSpawn(IEntitySource)" />
        public virtual void YoyoOnSpawn(Player owner, IEntitySource source) { }

        /// <inheritdoc cref="OnHitNPC(NPC, NPC.HitInfo, int)" />
        public virtual void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }

        /// <inheritdoc cref="OnHitPlayer(Player, Player.HurtInfo)" />
        public virtual void YoyoOnHitPlayer(Player owner, Player target, Player.HurtInfo info) { }

        /// <inheritdoc cref="SendExtraAI(BinaryWriter)" />
        public virtual void YoyoSendExtraAI(BinaryWriter writer) { }

        /// <inheritdoc cref="ReceiveExtraAI(BinaryReader)" />
        public virtual void YoyoReceiveExtraAI(BinaryReader reader) { }

        /// <inheritdoc cref="IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile, ref YoyoStatModifiers)" />
        public virtual void ModifyYoyoStats(ref YoyoStatModifiers statModifiers) { }

        /// <inheritdoc cref="IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile, Vector2)" />
        public virtual void PostDrawYoyoString(Vector2 mountedCenter) { }
    }
}