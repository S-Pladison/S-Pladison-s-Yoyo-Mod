using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.DataStructures;
using SPYoyoMod.Utils.Extensions;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class YoyoProjectile : ModProjectile, IModifyYoyoStatsProjectile, IPostDrawYoyoStringProjectile
    {
        public bool IsMainYoyo { get; private set; }
        public bool IsReturning { get => Projectile.ai[0] == -1; }
        public float ReturnToPlayerProgress { get; private set; }

        private readonly float lifeTime;
        private readonly float maxRange;
        private readonly float topSpeed;

        private Vector2? startToReturnPosition;

        public YoyoProjectile(float lifeTime, float maxRange, float topSpeed)
        {
            this.lifeTime = lifeTime;
            this.maxRange = maxRange;
            this.topSpeed = topSpeed;
        }

        // ...

        public sealed override void SetStaticDefaults()
        {
            ProjectileID.Sets.YoyosLifeTimeMultiplier[Type] = lifeTime;
            ProjectileID.Sets.YoyosMaximumRange[Type] = maxRange;
            ProjectileID.Sets.YoyosTopSpeed[Type] = topSpeed;

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

            IsMainYoyo = GetMainYoyoFlag(owner);

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

        private bool GetMainYoyoFlag(Player owner)
        {
            if (owner.OwnedProjectileCounts(Type) > 0)
                return false;

            // Fact that owned proj count was 0 does not guarantee that it is main yoyo
            // (In case of spawning 2+ yoyos at once)
            // Therefore, let's check other projs

            for (int i = 0; i < Projectile.whoAmI; i++)
            {
                ref var otherProjectile = ref Main.projectile[i];

                if (otherProjectile.active
                    && otherProjectile.owner == Projectile.owner
                    && otherProjectile.type == Type
                    && otherProjectile.ModProjectile is YoyoProjectile otherModProj
                    && otherModProj.IsMainYoyo)
                    return false;
            }

            return true;
        }

        void IModifyYoyoStatsProjectile.ModifyYoyoStats(Projectile proj, ref YoyoStatModifiers statModifiers)
        {
            ModifyYoyoStats(ref statModifiers);
        }

        void IPostDrawYoyoStringProjectile.PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            PostDrawYoyoString(mountedCenter);
        }

        // ...

        public virtual bool YoyoPreAI(Player owner) => true;
        public virtual void YoyoSetStaticDefaults() { }
        public virtual void YoyoSetDefaults() { }
        public virtual void YoyoOnSpawn(Player owner, IEntitySource source) { }
        public virtual void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
        public virtual void YoyoOnHitPlayer(Player owner, Player target, Player.HurtInfo info) { }
        public virtual void YoyoSendExtraAI(BinaryWriter writer) { }
        public virtual void YoyoReceiveExtraAI(BinaryReader reader) { }

        public virtual void OnActivateYoyoGlove() { }
        public virtual void ModifyYoyoStats(ref YoyoStatModifiers statModifiers) { }
        public virtual void PostDrawYoyoString(Vector2 mountedCenter) { }
    }
}