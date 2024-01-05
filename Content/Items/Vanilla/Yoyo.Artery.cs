using Microsoft.Xna.Framework;
using SPYoyoMod.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Vanilla
{
    public class ArteryYoyoItem : GlobalItem
    {
        public override bool AppliesToEntity(Item item, bool lateInstantiation) { return item.type.Equals(ItemID.CrimsonYoyo); }
    }

    public class ArteryYoyoProjectile : GlobalProjectile, IModifyYoyoStats, IPostDrawYoyoString
    {
        public override bool AppliesToEntity(Projectile proj, bool lateInstantiation) { return proj.type.Equals(ProjectileID.CrimsonYoyo); }

        public void ModifyYoyoStats(Projectile _, ref YoyoStatModifiers statModifiers)
        {
            if (Main.dayTime) return;

            statModifiers.LifeTime += 2f;
            statModifiers.MaxRange += 2f;
        }

        public void PostDrawYoyoString(Projectile proj, Vector2 mountedCenter)
        {
            //Main.spriteBatch.Draw(TextureAssets.Sun.Value, proj.Center - Main.screenPosition, Color.White);
        }
    }
}