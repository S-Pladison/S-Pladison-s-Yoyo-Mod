using Microsoft.Xna.Framework;
using SPYoyoMod.Common.Networking;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace SPYoyoMod.Content.Items
{
    public abstract class ChestTile : ModTile
    {
        public abstract int PlaceItemType { get; }
        public virtual int KeyItemType { get => -1; }
        public virtual Color MapColor { get => new(174, 129, 92); }

        public sealed override void SetStaticDefaults()
        {
            Main.tileSpelunker[Type] = true;
            Main.tileContainer[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileOreFinderPriority[Type] = 500;

            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.BasicChest[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;
            TileID.Sets.IsAContainer[Type] = true;

            DustType = -1;
            AdjTiles = new int[] { TileID.Containers };

            AddMapEntry(MapColor, this.GetLocalization("MapEntry0"), MapChestName);

            if (KeyItemType >= 0)
            {
                AddMapEntry(MapColor, this.GetLocalization("MapEntry1"), MapChestName);
            }

            RegisterItemDrop(PlaceItemType);

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = new[] { 16, 18 };
            TileObjectData.newTile.HookCheckIfCanPlace = new PlacementHook(Chest.FindEmptyChest, -1, 0, true);
            TileObjectData.newTile.HookPostPlaceMyPlayer = new PlacementHook(Chest.AfterPlacement_Hook, -1, 0, false);
            TileObjectData.newTile.AnchorInvalidTiles = new int[] {
                TileID.MagicalIceBlock,
                TileID.Boulder,
                TileID.BouncyBoulder,
                TileID.LifeCrystalBoulder,
                TileID.RollingCactus
            };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);

            ChestSetStaticDefaults();
        }

        public virtual void ChestSetStaticDefaults() { }

        public override ushort GetMapOption(int i, int j)
        {
            return (ushort)(Main.tile[i, j].TileFrameX / 36);
        }

        public override LocalizedText DefaultContainerName(int frameX, int frameY)
        {
            return this.GetLocalization("MapEntry" + frameX / 36);
        }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override bool IsLockedChest(int i, int j)
        {
            return Main.tile[i, j].TileFrameX / 36 == 1;
        }

        public override bool UnlockChest(int i, int j, ref short frameXAdjustment, ref int dustType, ref bool manual)
        {
            dustType = DustType;
            return true;
        }

        public override bool LockChest(int i, int j, ref short frameXAdjustment, ref bool manual)
        {
            return TileObjectData.GetTileStyle(Main.tile[i, j]) == 0;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Chest.DestroyChest(i, j);
        }

        public override bool RightClick(int i, int j)
        {
            var player = Main.LocalPlayer;
            var tile = Main.tile[i, j];

            Main.mouseRightRelease = false;

            var left = i;
            var top = j;

            if (tile.TileFrameX % 36 != 0) left--;
            if (tile.TileFrameY != 0) top--;

            player.CloseSign();
            player.SetTalkNPC(-1);

            Main.npcChatCornerItem = 0;
            Main.npcChatText = "";

            if (Main.editChest)
            {
                SoundEngine.PlaySound(SoundID.MenuTick);
                Main.editChest = false;
                Main.npcChatText = string.Empty;
            }

            if (player.editedChestName)
            {
                NetMessage.SendData(MessageID.SyncPlayerChest, -1, -1, NetworkText.FromLiteral(Main.chest[player.chest].name), player.chest, 1f);
                player.editedChestName = false;
            }

            var isLocked = Chest.IsLocked(left, top);

            if (Main.netMode == NetmodeID.MultiplayerClient && !isLocked)
            {
                if (left == player.chestX && top == player.chestY && player.chest != -1)
                {
                    player.chest = -1;
                    Recipe.FindRecipes();
                    SoundEngine.PlaySound(SoundID.MenuClose);
                }
                else
                {
                    NetMessage.SendData(MessageID.RequestChestOpen, -1, -1, null, left, top);
                    Main.stackSplit = 600;
                }
            }
            else
            {
                if (isLocked)
                {
                    if (player.ConsumeItem(KeyItemType, includeVoidBag: true) && Chest.Unlock(left, top))
                    {
                        if (Main.netMode == NetmodeID.MultiplayerClient)
                        {
                            NetMessage.SendData(MessageID.LockAndUnlock, -1, -1, null, player.whoAmI, 1f, left, top);
                        }
                    }
                }
                else
                {
                    var chest = Chest.FindChest(left, top);

                    if (chest != -1)
                    {
                        Main.stackSplit = 600;

                        if (chest == player.chest)
                        {
                            player.chest = -1;
                            SoundEngine.PlaySound(SoundID.MenuClose);
                        }
                        else
                        {
                            SoundEngine.PlaySound(player.chest < 0 ? SoundID.MenuOpen : SoundID.MenuTick);
                            player.OpenChest(left, top, chest);
                        }

                        Recipe.FindRecipes();
                    }
                }
            }

            return true;
        }

        public override void MouseOver(int i, int j)
        {
            var player = Main.LocalPlayer;
            var tile = Main.tile[i, j];

            var left = i;
            var top = j;

            if (tile.TileFrameX % 36 != 0) left--;
            if (tile.TileFrameY != 0) top--;

            var chest = Chest.FindChest(left, top);

            player.cursorItemIconID = -1;

            if (chest < 0)
            {
                player.cursorItemIconText = Language.GetTextValue("LegacyChestType.0");
            }
            else
            {
                var defaultName = TileLoader.DefaultContainerName(tile.TileType, tile.TileFrameX, tile.TileFrameY);

                player.cursorItemIconText = Main.chest[chest].name.Length > 0 ? Main.chest[chest].name : defaultName;

                if (player.cursorItemIconText == defaultName)
                {
                    player.cursorItemIconID = PlaceItemType;

                    if (Main.tile[left, top].TileFrameX / 36 == 1)
                    {
                        player.cursorItemIconID = KeyItemType;
                    }

                    player.cursorItemIconText = "";
                }
            }

            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
        }

        public override void MouseOverFar(int i, int j)
        {
            MouseOver(i, j);

            var player = Main.LocalPlayer;

            if (player.cursorItemIconText == "")
            {
                player.cursorItemIconEnabled = false;
                player.cursorItemIconID = 0;
            }
        }

        public static string MapChestName(string name, int i, int j)
        {
            var left = i;
            var top = j;
            var tile = Main.tile[i, j];

            if (tile.TileFrameX % 36 != 0) left--;
            if (tile.TileFrameY != 0) top--;

            var chest = Chest.FindChest(left, top);

            if (chest < 0) return Language.GetTextValue("LegacyChestType.0");
            if (Main.chest[chest].name == "") return name;

            return name + ": " + Main.chest[chest].name;
        }
    }

    public abstract class TrappedChestTile : ModTile
    {
        private class TriggerTrappedChestPacket : NetPacket
        {
            public readonly short Left;
            public readonly short Top;

            public TriggerTrappedChestPacket() { }

            public TriggerTrappedChestPacket(int left, int top)
            {
                Left = (short)left;
                Top = (short)top;
            }

            public override void Send(BinaryWriter writer)
            {
                writer.Write(Left);
                writer.Write(Top);
            }

            public override void Receive(BinaryReader reader, int sender)
            {
                var left = reader.ReadInt16();
                var top = reader.ReadInt16();

                Wiring.SetCurrentUser(sender);
                TrappedChestTile.Trigger(left, top);
                Wiring.SetCurrentUser(-1);

                if (Main.netMode == NetmodeID.Server)
                {
                    new TriggerTrappedChestPacket(left, top).Send(-1, sender);
                }
            }
        }

        public virtual Color MapColor { get => new(174, 129, 92); }

        public sealed override void SetStaticDefaults()
        {
            Main.tileSpelunker[Type] = true;
            Main.tileFrameImportant[Type] = true;
            Main.tileNoAttach[Type] = true;
            Main.tileOreFinderPriority[Type] = 500;

            TileID.Sets.HasOutlines[Type] = true;
            TileID.Sets.AvoidedByNPCs[Type] = true;
            TileID.Sets.InteractibleByNPCs[Type] = true;
            TileID.Sets.DisableSmartCursor[Type] = true;
            TileID.Sets.BasicChestFake[Type] = true;
            TileID.Sets.IsATrigger[Type] = true;

            DustType = -1;
            AdjTiles = new int[] { TileID.FakeContainers };

            AddMapEntry(MapColor, CreateMapEntryName());

            TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
            TileObjectData.newTile.Origin = new Point16(0, 1);
            TileObjectData.newTile.CoordinateHeights = new int[] { 16, 18 };
            TileObjectData.newTile.AnchorInvalidTiles = new int[]
            {
                TileID.MagicalIceBlock,
                TileID.Boulder,
                TileID.BouncyBoulder,
                TileID.LifeCrystalBoulder,
                TileID.RollingCactus
            };
            TileObjectData.newTile.StyleHorizontal = true;
            TileObjectData.newTile.LavaDeath = false;
            TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, TileObjectData.newTile.Width, 0);
            TileObjectData.addTile(Type);
        }

        public virtual void TrappedChestSetStaticDefaults() { }

        public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings)
        {
            return true;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            return false;
        }

        public override ushort GetMapOption(int i, int j)
        {
            return (ushort)(Main.tile[i, j].TileFrameX / 36);
        }

        public override bool RightClick(int i, int j)
        {
            Main.mouseRightRelease = false;

            var tile = Main.tile[i, j];
            var left = i;
            var top = j;

            if (tile.TileFrameX % 36 != 0) left--;
            if (tile.TileFrameY != 0) top--;

            Animation.NewTemporaryAnimation(2, tile.TileType, left, top);
            NetMessage.SendTemporaryAnimation(-1, 2, tile.TileType, left, top);

            Trigger(i, j);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                new TriggerTrappedChestPacket(left, top).Send();

            Wiring.HitSwitch(i, j);
            NetMessage.SendData(MessageID.HitSwitch, -1, -1, null, i, j);

            return true;
        }

        public override void AnimateIndividualTile(int type, int i, int j, ref int frameXOffset, ref int frameYOffset)
        {
            var tile = Main.tile[i, j];
            var left = i;
            var top = j;

            if (tile.TileFrameX % 36 != 0) left--;
            if (tile.TileFrameY != 0) top--;

            if (Animation.GetTemporaryFrame(left, top, out int newFrameYOffset))
            {
                frameYOffset = 38 * newFrameYOffset;
            }
        }

        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;

            int item = TileLoader.GetItemDropFromTypeAndStyle(Type, 0);

            if (item > 0)
            {
                player.cursorItemIconID = item;
            }

            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
        }

        // Vanilla trapped chests do it in Wiring.HitSwitch
        // I could do a hook, but I don't think it's worth it
        public static void Trigger(int i, int j)
        {
            var tile = Main.tile[i, j];
            var left = i;
            var top = j;

            if (tile.TileFrameX % 36 != 0) left--;
            if (tile.TileFrameY != 0) top--;

            SoundEngine.PlaySound(SoundID.Mech, new Vector2(i * 16, j * 16));
            Wiring.TripWire(left, top, 2, 2);
        }
    }
}