using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SPYoyoMod.Content.Items.Mod.Weapons;
using System;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using static Mono.Cecil.Cil.OpCodes;

namespace SPYoyoMod.Content.Items.Mod.Misc
{
    public class StrangeDrinkItem : ModItem
    {
        public override string Texture => ModAssets.ItemsPath + "StrangeDrink";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest;
            Item.width = 22;
            Item.height = 46;
        }

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LifeFruit, 3);
            recipe.AddIngredient(ItemID.Milkshake);
            recipe.AddIngredient(ItemID.GrapeJuice);
            recipe.AddIngredient(ItemID.PrismaticPunch);
            recipe.AddIngredient(ItemID.PinaColada);
            recipe.AddIngredient(ItemID.TropicalSmoothie);
            recipe.AddIngredient(ItemID.SmoothieofDarkness);
            recipe.AddIngredient(ItemID.AppleJuice);
            recipe.AddIngredient(ItemID.BananaDaiquiri);
            recipe.AddIngredient(ItemID.Lemonade);
            recipe.AddIngredient(ItemID.PeachSangria);
            recipe.AddTile(TileID.CookingPots);
            recipe.Register();

            recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.LifeFruit, 3);
            recipe.AddIngredient(ItemID.Milkshake);
            recipe.AddIngredient(ItemID.GrapeJuice);
            recipe.AddIngredient(ItemID.PrismaticPunch);
            recipe.AddIngredient(ItemID.PinaColada);
            recipe.AddIngredient(ItemID.TropicalSmoothie);
            recipe.AddIngredient(ItemID.BloodyMoscato);
            recipe.AddIngredient(ItemID.AppleJuice);
            recipe.AddIngredient(ItemID.BananaDaiquiri);
            recipe.AddIngredient(ItemID.Lemonade);
            recipe.AddIngredient(ItemID.PeachSangria);
            recipe.AddTile(TileID.CookingPots);
            recipe.Register();
        }
    }

    public class StrangeDrinkImplemention : ILoadable
    {
        public static int GiftForNurseType { get => ModContent.ItemType<StrangeDrinkItem>(); }
        public static int GiftForPlayerType { get => ModContent.ItemType<SoulTormentorItem>(); }
        public static LocalizedText GiftButtonText { get; private set; }
        public static LocalizedText GiftDialogueText { get; private set; }

        public void Load(Terraria.ModLoader.Mod mod)
        {
            RegisterLocalizedText();

            var setChatButtonsMethodInfo = typeof(NPCLoader).GetMethod(nameof(NPCLoader.SetChatButtons));

            MonoModHooks.Add(setChatButtonsMethodInfo, (orig_SetChatButtonsMethod orig, ref string button, ref string button2) =>
            {
                orig(ref button, ref button2);

                var player = Main.player[Main.myPlayer];

                if (!IsPlayerTalksWithNurse(player)) return;

                SetNurseFirstButtonText(player, ref button);
            });

            IL_Main.GUIChatDrawInner += (il) =>
            {
                var c = new ILCursor(il);

                // if (Main.npc[Main.player[Main.myPlayer].talkNPC].type != 18) return;

                // IL_2070: ldsfld       class Terraria.NPC[] Terraria.Main::npc
                // IL_2075: ldsfld       class Terraria.Player[] Terraria.Main::player
                // IL_207a: ldsfld int32 Terraria.Main::myPlayer
                // IL_207f: ldelem.ref
                // IL_2080: callvirt instance int32 Terraria.Player::get_talkNPC()
                // IL_2085: ldelem.ref
                // IL_2086: ldfld int32 Terraria.NPC::'type'
                // IL_208b: ldc.i4.s     18 // 0x12
                // IL_208d: beq.s IL_2090

                // IL_208f: ret

                if (!c.TryGotoNext(MoveType.Before,
                    i => i.MatchLdsfld(typeof(Main).GetField("npc")),
                    i => i.MatchLdsfld(typeof(Main).GetField("player")),
                    i => i.MatchLdsfld(typeof(Main).GetField("myPlayer")),
                    i => i.MatchLdelemRef(),
                    i => i.MatchCallvirt(typeof(Player).GetMethod("get_talkNPC")),
                    i => i.MatchLdelemRef(),
                    i => i.MatchLdfld(typeof(NPC).GetField("type")),
                    i => i.MatchLdcI4(18),
                    i => i.MatchBeq(out _),
                    i => i.MatchRet())) return;

                if (!c.TryGotoNext(MoveType.After,
                    i => i.MatchLdcI4(12),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchLdcI4(-1),
                    i => i.MatchLdcI4(1),
                    i => i.MatchLdcR4(1),
                    i => i.MatchLdcR4(0),
                    i => i.MatchCall(typeof(SoundEngine).GetMethod("PlaySound", BindingFlags.Static | BindingFlags.NonPublic, new Type[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float) })),
                    i => i.MatchPop())) return;

                c.EmitDelegate(() =>
                {
                    var player = Main.LocalPlayer;

                    if (!IsPlayerTalksWithNurse(player)) return true;

                    return OnClickNurseFirstButton(player);
                });

                var label = c.DefineLabel();

                c.Emit(Brtrue, label);
                c.Emit(Ret);
                c.MarkLabel(label);
            };
        }

        public void Unload()
        {
            GiftButtonText = null;
            GiftDialogueText = null;
        }

        public static void RegisterLocalizedText()
        {
            GiftButtonText = Language.GetOrRegister("Mods.SPYoyoMod.NPCs.NurseNPC.GiftButton");
            GiftDialogueText = Language.GetOrRegister("Mods.SPYoyoMod.Dialogue.NurseNPC.Dialogue.ReceivedGift");
        }

        public static void SetNurseFirstButtonText(Player player, ref string button)
        {
            if (!player.HasItem(GiftForNurseType)) return;

            button = $"[c/{Colors.AlphaDarken(Color.HotPink).Hex3()}:{GiftButtonText.Value}]";
        }

        public static bool OnClickNurseFirstButton(Player player)
        {
            var slotIndex = player.FindItem(GiftForNurseType);
            var hasGiftForNurse = slotIndex >= 0;

            if (!hasGiftForNurse) return true;

            Main.npcChatText = GiftDialogueText.Value;

            player.inventory[slotIndex].TurnToAir();
            player.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(), GiftForPlayerType);

            return false;
        }

        public static bool IsPlayerTalksWithNurse(Player player)
        {
            return player.talkNPC >= 0 && Main.npc[player.talkNPC].type.Equals(NPCID.Nurse);
        }

        public delegate void orig_SetChatButtonsMethod(ref string button, ref string button2);
    }
}