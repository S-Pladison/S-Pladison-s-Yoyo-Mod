using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace SPYoyoMod.Content.Items.Vanilla.Weapons
{
    public class AmarokItem : VanillaYoyoItem
    {
        public override int YoyoType => ItemID.Amarok;
    }

    public class AmarokProjectile : ValorProjectile
    {
        public override int YoyoType => ProjectileID.Amarok;
    }
}