using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Interfaces;
using SPYoyoMod.Utils.DataStructures;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items
{
    public abstract class YoyoProjectile : ModProjectile, IModifyYoyoStatsProjectile, IPostDrawYoyoStringProjectile
    {
        public bool YoyoGloveActivated { get; private set; }
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
            Projectile.DamageType = DamageClass.Melee;

            Projectile.width = 16;
            Projectile.height = 16;

            Projectile.aiStyle = 99;
            Projectile.friendly = true;
            Projectile.penetrate = -1;

            YoyoSetDefaults();
        }

        public sealed override bool PreAI()
        {
            var owner = Main.player[Projectile.owner];

            if (!YoyoPreAI(owner)) return false;

            if (IsReturning)
            {
                if (!startToReturnPosition.HasValue)
                {
                    startToReturnPosition = Projectile.Center;
                }

                var progress = 1f - Vector2.DistanceSquared(owner.Center, Projectile.Center) / Vector2.DistanceSquared(owner.Center, startToReturnPosition.Value);
                ReturnToPlayerProgress = MathHelper.Clamp(progress, 0f, 1f);
            }

            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            var owner = Main.player[Projectile.owner];

            if (owner.yoyoGlove && !YoyoGloveActivated)
            {
                YoyoGloveActivated = true;
                OnActivateYoyoGlove();
            }

            YoyoOnHitNPC(owner, target, hit, damageDone);
        }

        public sealed override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(YoyoGloveActivated);
            YoyoSendExtraAI(writer);
        }

        public sealed override void ReceiveExtraAI(BinaryReader reader)
        {
            YoyoGloveActivated = reader.ReadBoolean();
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

        // ...

        public virtual bool YoyoPreAI(Player owner) => true;
        public virtual void YoyoSetStaticDefaults() { }
        public virtual void YoyoSetDefaults() { }
        public virtual void YoyoOnHitNPC(Player owner, NPC target, NPC.HitInfo hit, int damageDone) { }
        public virtual void YoyoSendExtraAI(BinaryWriter writer) { }
        public virtual void YoyoReceiveExtraAI(BinaryReader reader) { }

        public virtual void OnActivateYoyoGlove() { }
        public virtual void ModifyYoyoStats(ref YoyoStatModifiers statModifiers) { }
        public virtual void PostDrawYoyoString(Vector2 mountedCenter) { }
    }
}