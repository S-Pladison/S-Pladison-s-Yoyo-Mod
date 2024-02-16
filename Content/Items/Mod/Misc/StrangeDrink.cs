using Microsoft.Xna.Framework;
using MonoMod.Cil;
using SPYoyoMod.Common.ModCompatibility;
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

            IL_Main.GUIChatDrawInner += (il) =>
            {
                var c = new ILCursor(il);

                ModifyButtonText(c);

                c.Index = 0;

                ModifyOnClickButton(c);
            };

            ModContent.GetInstance<DialogueTweakCompatibility>().AddButton(
                npcType: NPCID.Nurse,
                buttonText: () =>
                {
                    var button = "";
                    SetNurseFirstButtonText(ref button);
                    return button;
                },
                iconTexturePath: "Terraria/Images/NPC_Head_" + NPCHeadID.Nurse,
                hoverCallback: () =>
                {
                    if (!Main.mouseLeft || !Main.mouseLeftRelease) return;

                    OnClickNurseFirstButton();
                },
                availability: () =>
                {
                    return IsPlayerHaveGift(out _);
                }
            );
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

        public static void ModifyButtonText(ILCursor c)
        {
            // NPCLoader.SetChatButtons(ref button, ref button2);

            // IL_14f7: ldloca.s 11
            // IL_14f9: ldloca.s 12
            // IL_14fb: call void Terraria.ModLoader.NPCLoader::SetChatButtons(string &, string &)

            int buttonIndex = -1;

            if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdloca(out buttonIndex),
                i => i.MatchLdloca(out _),
                i => i.MatchCall(typeof(NPCLoader).GetMethod(nameof(NPCLoader.SetChatButtons), BindingFlags.Static | BindingFlags.Public)))) return;

            c.Emit(Ldloca, buttonIndex);
            c.EmitDelegate((ref string button) =>
            {
                var player = Main.LocalPlayer;

                if (!IsPlayerTalksWithNurse()) return;

                SetNurseFirstButtonText(ref button);
            });
        }

        public static void ModifyOnClickButton(ILCursor c)
        {
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
                if (!IsPlayerTalksWithNurse()) return true;

                return OnClickNurseFirstButton();
            });

            var label = c.DefineLabel();

            c.Emit(Brtrue, label);
            c.Emit(Ret);
            c.MarkLabel(label);
        }

        public static void SetNurseFirstButtonText(ref string button)
        {
            if (!IsPlayerHaveGift(out _)) return;

            button = $"[c/{Colors.AlphaDarken(Color.HotPink).Hex3()}:{GiftButtonText.Value}]";
        }

        public static bool OnClickNurseFirstButton()
        {
            if (!IsPlayerHaveGift(out int slotIndex)) return true;

            Main.npcChatText = GiftDialogueText.Value;

            Main.LocalPlayer.inventory[slotIndex].TurnToAir();
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(), GiftForPlayerType);

            return false;
        }

        public static bool IsPlayerTalksWithNurse()
        {
            return Main.LocalPlayer.talkNPC >= 0 && Main.npc[Main.LocalPlayer.talkNPC].type.Equals(NPCID.Nurse);
        }

        public static bool IsPlayerHaveGift(out int slotIndex)
        {
            var player = Main.LocalPlayer;
            slotIndex = player.FindItem(GiftForNurseType);

            return slotIndex >= 0;
        }
    }
}