using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SPYoyoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Weapons
{
    public class CulminationItem : YoyoItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "Sirius"; }

        public CulminationItem() : base(gamepadExtraRange: 15) { }

        public override void YoyoSetDefaults()
        {
            Item.damage = 43;
            Item.knockBack = 2.5f;

            Item.shoot = ModContent.ProjectileType<CulminationProjectile>();

            Item.rare = ItemRarityID.Red;
            Item.value = Terraria.Item.sellPrice(platinum: 0, gold: 1, silver: 50, copper: 0);
        }

        public override void PostDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, float rotation, float scale, int whoAmI)
        {
            try
            {
                Player other = Main.LocalPlayer;

                //Player drawPlayer = Main.playerVisualClone[owner];
                var drawPlayer = new Player();
                drawPlayer.CopyVisuals(other);



                /*drawPlayer.isFirstFractalAfterImage = true;
                drawPlayer.firstFractalAfterImageOpacity = 1f;*/
                drawPlayer.ResetEffects();
                drawPlayer.ResetVisibleAccessories();
                drawPlayer.UpdateDyes();
                drawPlayer.DisplayDollUpdate();
                drawPlayer.UpdateSocialShadow();
                drawPlayer.itemAnimation = 0;
                /*drawPlayer.itemAnimationMax = 60;
                drawPlayer.itemAnimation = (int)projectile.localAI[0];
                drawPlayer.itemRotation = projectile.velocity.ToRotation();
                drawPlayer.heldProj = index2;*/
                drawPlayer.Center = Item.Center + new Vector2(50, 0);

                drawPlayer.direction = 1;
                drawPlayer.itemRotation = (float)Math.Atan2((Item.Center.Y - drawPlayer.MountedCenter.Y) * (double)drawPlayer.direction, (Item.Center.X - drawPlayer.MountedCenter.X) * (double)drawPlayer.direction);

                rotation = drawPlayer.itemRotation * drawPlayer.gravDir - 1.57079637f * drawPlayer.direction;
                drawPlayer.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
                drawPlayer.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, rotation);

                drawPlayer.velocity = Vector2.Zero;
                //drawPlayer.wingFrame = 2;
                drawPlayer.PlayerFrame();
                Main.PlayerRenderer.DrawPlayer(Main.Camera, drawPlayer, drawPlayer.position, 0.0f, drawPlayer.fullRotationOrigin);
            }
            catch (Exception)
            {
                //TimeLogger.DrawException(ex);
                //Main.projectile[projCache[index1]].active = false;
            }
        }
    }

    public class CulminationProjectile : YoyoProjectile
    {
        public static readonly float PlayerSpawnRadius;

        static CulminationProjectile()
        {
            PlayerSpawnRadius = 16f * 16f;
        }

        public override string Texture { get => ModAssets.ProjectilesPath + "Sirius"; }
        public int PlayerSpawnTimer { get; private set; }

        public CulminationProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void Load()
        {
            On_Main.DrawPlayers_BehindNPCs += (orig, main) =>
            {
                orig(main);

                foreach (var proj in DrawUtils.GetActiveForDrawProjectiles())
                {
                    if (proj.ModProjectile is CulminationProjectile culminationProj)
                        culminationProj.DrawPlayers();
                }
            };
        }

        /*public override void YoyoOnSpawn(Player owner, IEntitySource source)
        {
            PlayerSpawnTimer = 30;
        }*/

        public override bool YoyoPreAI(Player owner)
        {
            if (--PlayerSpawnTimer <= 0)
            {
                PlayerSpawnTimer = 60 * 2;

                SpawnPlayer();
            }

            return true;
        }

        private void SpawnPlayer()
        {
            // Кароче, чекай в Player рандомную телепортацию, чтобы реализовать спавн игроков вокруг йо-йо

            var center = Projectile.Center;
            var randPosX = Main.rand.NextFloat(center.X - PlayerSpawnRadius, center.X + PlayerSpawnRadius);
        }

        private void DrawPlayers()
        {
            try
            {
                var owner = Main.player[Projectile.owner];
                var drawPlayer = new Player();

                CopyVisual(owner, drawPlayer);
                SetVisual(drawPlayer, 0);

                drawPlayer.ResetEffects();
                drawPlayer.ResetVisibleAccessories();
                drawPlayer.UpdateDyes();
                drawPlayer.DisplayDollUpdate();
                drawPlayer.UpdateSocialShadow();
                drawPlayer.itemAnimation = 0;
                /*drawPlayer.itemAnimationMax = 60;
                drawPlayer.itemAnimation = (int)projectile.localAI[0];
                drawPlayer.itemRotation = projectile.velocity.ToRotation();
                drawPlayer.heldProj = index2;*/
                drawPlayer.Center = Projectile.Center + new Vector2(50, 0);

                drawPlayer.direction = 1;
                drawPlayer.itemRotation = (float)Math.Atan2((Projectile.Center.Y - drawPlayer.MountedCenter.Y) * (double)drawPlayer.direction, (Projectile.Center.X - drawPlayer.MountedCenter.X) * (double)drawPlayer.direction);

                var rotation = drawPlayer.itemRotation * drawPlayer.gravDir - 1.57079637f * drawPlayer.direction;
                drawPlayer.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
                drawPlayer.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Quarter, rotation);

                drawPlayer.velocity = Vector2.Zero;
                //drawPlayer.wingFrame = 2;
                drawPlayer.PlayerFrame();
                drawPlayer.socialIgnoreLight = true;
                Main.PlayerRenderer.DrawPlayer(Main.Camera, drawPlayer, drawPlayer.position, 0.0f, drawPlayer.fullRotationOrigin);
            }
            catch (Exception)
            {

            }
        }

        private void CopyVisual(Player from, Player to)
        {
            // Cut copy drawPlayer.CopyVisuals(owner);

            to.skinVariant = from.skinVariant;
            to.direction = from.direction;
            to.selectedItem = from.selectedItem;
            to.extraAccessory = from.extraAccessory;
            to.skinColor = from.skinColor;
            to.eyeColor = from.eyeColor;
            to.hair = from.hair;
            to.hairColor = from.hairColor;
            to.shirtColor = from.shirtColor;
            to.underShirtColor = from.underShirtColor;
            to.pantsColor = from.pantsColor;
            to.shoeColor = from.shoeColor;
            to.position = from.position;
            to.velocity = from.velocity;
            to.statLife = from.statLife;
            to.statLifeMax = from.statLifeMax;
            to.statLifeMax2 = from.statLifeMax2;
            to.statMana = from.statMana;
            to.statManaMax = from.statManaMax;
            to.statManaMax2 = from.statManaMax2;
            to.hideMisc = from.hideMisc;
        }

        private void SetVisual(Player player, int index)
        {
            var playerVisualLoader = ModContent.GetInstance<CulminationPlayerVisualLoader>();
            var playerVisual = playerVisualLoader.Data.ElementAt(index);

            player.armor[0].SetDefaults(playerVisual.Helmet);
            player.armor[1].SetDefaults(playerVisual.Breastplate);
            player.armor[2].SetDefaults(playerVisual.Greaves);
        }
    }

    public class CulminationPlayerVisualLoader : ILoadable
    {
        public record struct PlayerVisualData(int Helmet, int Breastplate, int Greaves);

        public IReadOnlyList<PlayerVisualData> Data { get => dataArray; }

        private PlayerVisualData[] dataArray;

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            dataArray = new PlayerVisualData[]
            {
                new() { Helmet = ItemID.WoodHelmet, Breastplate = ItemID.WoodBreastplate, Greaves = ItemID.WoodGreaves },
                new() { Helmet = ItemID.CrimsonHelmet, Breastplate = ItemID.CrimsonScalemail, Greaves = ItemID.CrimsonGreaves }
            };
        }

        void ILoadable.Unload() { }
    }

    /*public class CulminationProjectile : YoyoProjectile
    {
        public override string Texture { get => ModAssets.ProjectilesPath + "Sirius"; }
        public CulminationExtraProjectile ExtraProj { get => Main.projectile[extraProjIndex].ModProjectile as CulminationExtraProjectile; }

        private int extraProjIndex;

        public CulminationProjectile() : base(lifeTime: -1f, maxRange: 300f, topSpeed: 13f) { }

        public override void OnSpawn(IEntitySource source)
        {
            extraProjIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CulminationExtraProjectile>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        }
    }

    public class CulminationExtraProjectile : ModProjectile
    {
        private int parentProjIndex;

        public override bool PreAI()
        {
            return base.PreAI();
        }

        public override bool PreDraw(ref Color lightColor)
        {

            return true;
        }
    }*/
}