using Microsoft.Xna.Framework;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class AmarokItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.Amarok;
    }

    public class AmarokProjectile : VanillaYoyoProjectile
    {
        public const int CrystalCount = 7;

        public override int YoyoType { get => ProjectileID.Amarok; }
        public int CrystalCounter { get; set; }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            for (int i = 0; i < CrystalCount; i++)
            {
                Projectile.NewProjectile(proj.GetSource_FromThis(), proj.Center, Vector2.Zero, ModContent.ProjectileType<AmarokCrystalProjectile>(), proj.damage, proj.knockBack, proj.owner, proj.identity, i);
            }
        }

        public override void AI(Projectile proj)
        {
            CrystalCounter++;

            /*var vector = Vector2.UnitX.RotatedBy(Main.rand.NextFloat(MathHelper.TwoPi));
            var position = proj.Center;
            var velocity = vector * Main.rand.NextFloat(1f);
            var dust = Dust.NewDustPerfect(position, ModContent.DustType<SmokeDust>(), velocity, Main.rand.Next(50, 100), Color.White, Main.rand.NextFloat(0.02f, 0.04f));
            dust.customData = new SmokeDust.CustomData(Color.White, false, new Color(165, 185, 200), false);*/
        }
    }

    public class AmarokCrystalProjectile : ModProjectile
    {
        public override string Texture { get => ModAssets.MiscPath + "Invisible"; }
        public int YoyoProjIdentity { get => (int)Projectile.ai[0]; }
        public int CrystalIndex { get => (int)Projectile.ai[1]; }

        private bool initialized;
        private int yoyoProjIndex;

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.MeleeNoSpeed;

            Projectile.width = 8;
            Projectile.height = 8;

            Projectile.timeLeft = 2;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 20;

            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (!initialized)
            {
                yoyoProjIndex = Main.projectile.FirstOrDefault(p => p.identity == YoyoProjIdentity && p.type == ProjectileID.Amarok)?.whoAmI ?? -1;
                initialized = true;
            }

            var yoyoProj = Main.projectile[yoyoProjIndex];

            if (yoyoProjIndex < 0
                || yoyoProj.type != ProjectileID.Amarok
                || !yoyoProj.active
                || !yoyoProj.TryGetGlobalProjectile(out AmarokProjectile yoyoGlobalProj))
            {
                Projectile.Kill();
                return;
            }

            float time = CrystalIndex / (float)AmarokProjectile.CrystalCount * MathHelper.TwoPi + yoyoGlobalProj.CrystalCounter * 0.1f;

            var vector = (yoyoProj.position - yoyoProj.oldPosition);

            /*Projectile.rotation += MathF.Sign(vector.X) * 0.1f * MathHelper.Clamp(vector.X, -1f, 1f);
            Projectile.rotation *= 0.8f;*/

            //Projectile.Center = proj.Center + Vector2.One.RotatedBy(CrystalIndex / (float)AmarokProjectile.CrystalCount * MathHelper.TwoPi + globalProj.CrystalCounter * 0.1f) * 16f * 5f;
            Projectile.Center = yoyoProj.Center + new Vector2(MathF.Sin(time), MathF.Cos(time) * 0.15f).RotatedBy((Main.player[Projectile.owner].MountedCenter - yoyoProj.Center).ToRotation() + MathHelper.PiOver2) * (Main.player[Projectile.owner].MountedCenter - yoyoProj.Center).Length() * 0.6f;
            Projectile.timeLeft += 1;
        }
    }
}