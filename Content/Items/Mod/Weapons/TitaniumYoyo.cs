﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Common.Graphics.Renderers;
using System.IO;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class TitaniumYoyoItem : YoyoItem
    {
        public override string Texture => ModAssets.ItemsPath + "TitaniumYoyo";
        public override int GamepadExtraRange => 13;

        public override void YoyoSetDefaults()
        {
            Item.damage = 42;
            Item.knockBack = 4.0f;
            Item.autoReuse = true;

            Item.shoot = ModContent.ProjectileType<TitaniumYoyoProjectile>();

            Item.rare = ItemRarityID.LightRed;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 3, silver: 0, copper: 0);
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.TitaniumBar, 13)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }
    }

    public class TitaniumYoyoProjectile : YoyoProjectile
    {
        public override string Texture => ModAssets.ProjectilesPath + "TitaniumYoyo";
        public override float LifeTime => 15f;
        public override float MaxRange => 275f;
        public override float TopSpeed => 15f;

        private SpriteTrailRenderer spriteTrailRenderer;

        public override void AI()
        {
            Projectile.rotation -= 0.2f;

            spriteTrailRenderer?.SetNextPoint(Projectile.Center + Projectile.velocity, Projectile.rotation);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(Type);

            spriteTrailRenderer ??= new SpriteTrailRenderer(7, TextureAssets.Projectile[Type], TextureAssets.Projectile[Type].Size() * 0.5f, SpriteEffects.None)
                .SetFadingColor(Color.White * 0.3f);

            spriteTrailRenderer.Draw(Main.spriteBatch, -Main.screenPosition, lightColor);

            var texture = TextureAssets.Projectile[Type];
            var position = Projectile.Center + Projectile.gfxOffY * Vector2.UnitY - Main.screenPosition;

            Main.EntitySpriteDraw(texture.Value, position, null, lightColor, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
    }

    public class TitaniumYoyoPlayer : ModPlayer
    {
        public override void OnHitAnything(float x, float y, Entity victim)
        {
            if (victim is not NPC npc
                || Player.HeldItem.type != ModContent.ItemType<TitaniumYoyoItem>()
                || npc.type == NPCID.TargetDummy) return;

            var shardCount = Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard];

            // Titanium armor is not equipped (just yoyo)
            // Copy from vanilla code
            if (!Player.onHitTitaniumStorm)
            {
                Player.AddBuff(BuffID.TitaniumStorm, 600, true, false);

                if (shardCount >= 7) return;

                SpawnTitaniumStormShard(victim);
                return;
            }

            // Titanium armor equipped (with yoyo)
            if (shardCount >= 14) return;

            SpawnTitaniumStormShard(victim);
        }

        private void SpawnTitaniumStormShard(Entity victim)
        {
            Player.ownedProjectileCounts[ProjectileID.TitaniumStormShard]++;

            Projectile.NewProjectile(Player.GetSource_OnHit(victim, "SetBonus_Titanium"), Player.Center, Vector2.Zero, ProjectileID.TitaniumStormShard, 50, 15f, Player.whoAmI, 0f, 0f);
        }
    }

    public class TitaniumYoyoGlobalProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private bool hasBonus;

        public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        {
            return entity.type == ProjectileID.TitaniumStormShard;
        }

        public override void OnSpawn(Projectile proj, IEntitySource source)
        {
            var owner = Main.player[proj.owner];

            if (owner.HeldItem.type == ModContent.ItemType<TitaniumYoyoItem>() && owner.onHitTitaniumStorm)
            {
                hasBonus = true;
            }
        }

        public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (hasBonus)
            {
                modifiers.SourceDamage += 0.3f;
            }
        }

        public override void SendExtraAI(Projectile proj, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(hasBonus);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            hasBonus = bitReader.ReadBit();
        }
    }
}