using Mono.Cecil.Cil;
using MonoMod.Cil;
using SPYoyoMod.Content.Items.Mod.Yoyos;
using SPYoyoMod.Core.ModSupport;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Miscellaneous
{
    public sealed class StrangeDrinkItem : ModItem
    {
        public override string Texture => $"{nameof(SPYoyoMod)}/Assets/Items/Mod.Miscellaneous/StrangeDrink_Item";

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Quest;
            Item.width = 22;
            Item.height = 46;
        }
    }

    [Autoload(Side = ModSide.Client)]
    public sealed class StrangeDrinkNurseButtonImplemention : ILoadable
    {
        public static LocalizedText ButtonText { get; private set; }
        public static LocalizedText DialogueText { get; private set; }

        void ILoadable.Load(Terraria.ModLoader.Mod mod)
        {
            ButtonText = Language.GetOrRegister("Mods.SPYoyoMod.NPCs.NurseNPC.Button.StrangeDrink");
            DialogueText = Language.GetOrRegister("Mods.SPYoyoMod.NPCs.NurseNPC.Dialogue.StrangeDrink");

            // Изменяем кнопку отхила игрока на новую в случае, когда у игрока в инвентаре есть напиток
            IL_Main.GUIChatDrawInner += (il) =>
            {
                var c = new ILCursor(il);

                ModifyButtonText(c);

                c.Index = 0;

                ModifyOnClickButton(c);
            };

            // Т.к. данный мод переделывает все диалоговые меню с NPC, то нам также нужно взаимодействовать и с ним;
            // Можно было б изменить кнопку отхила, как это делается в ванилке, но легче просто добавить новую кнопку...
            DialogueTweakSupport.AddButton(
                npcType: NPCID.Nurse,
                buttonText: () =>
                {
                    var button = "";

                    UpdateNurseFirstButtonText(ref button);

                    return button;
                },
                iconTexturePath: null,
                hoverCallback: () =>
                {
                    if (!Main.mouseLeft || !Main.mouseLeftRelease)
                        return;

                    OnClickNurseFirstButton();
                },
                availability: () =>
                {
                    return DoesPlayerHaveStrangeDrink(out _);
                }
            );
        }

        void ILoadable.Unload()
        {
            ButtonText = null;
            DialogueText = null;
        }

        private static bool IsPlayerTalksWithNurse()
            => Main.LocalPlayer.talkNPC >= 0 && Main.npc[Main.LocalPlayer.talkNPC].type.Equals(NPCID.Nurse);

        private static bool DoesPlayerHaveStrangeDrink(out int slotIndex)
        {
            var player = Main.LocalPlayer;
            slotIndex = player.FindItem(ModContent.ItemType<StrangeDrinkItem>());

            return slotIndex >= 0;
        }

        private static void UpdateNurseFirstButtonText(ref string button)
        {
            if (!DoesPlayerHaveStrangeDrink(out _))
                return;

            button = $"[c/{Colors.AlphaDarken(ItemRarity.GetColor(ItemRarityID.Quest)).Hex3()}:{ButtonText.Value}]";
        }

        private static bool OnClickNurseFirstButton()
        {
            if (!DoesPlayerHaveStrangeDrink(out int slotIndex))
                return true;

            Main.npcChatText = DialogueText.Value;

            Main.LocalPlayer.inventory[slotIndex].TurnToAir();
            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(), ModContent.ItemType<SoulTormentorItem>());

            return false;
        }

        private static void ModifyButtonText(ILCursor c)
        {
            // NPCLoader.SetChatButtons(ref button, ref button2);

            // IL_14f7: ldloca.s 11
            // IL_14f9: ldloca.s 12
            // IL_14fb: call void Terraria.ModLoader.NPCLoader::SetChatButtons(string &, string &)

            int buttonIndex = -1;

            if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdloca(out buttonIndex),
                i => i.MatchLdloca(out _),
                i => i.MatchCall(typeof(NPCLoader).GetMethod(nameof(NPCLoader.SetChatButtons), BindingFlags.Static | BindingFlags.Public))))
            {
                ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(StrangeDrinkNurseButtonImplemention)}..{nameof(IL_Main.GUIChatDrawInner)}..{nameof(ModifyButtonText)}\" failed...");
                return;
            }

            c.Emit(OpCodes.Ldloca, buttonIndex);
            c.EmitDelegate((ref string button) =>
            {
                if (!IsPlayerTalksWithNurse())
                    return;

                UpdateNurseFirstButtonText(ref button);
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
                i => i.MatchRet()))
            {
                ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(StrangeDrinkNurseButtonImplemention)}..{nameof(IL_Main.GUIChatDrawInner)}..{nameof(ModifyOnClickButton)}\" failed...");
                return;
            }

            if (!c.TryGotoNext(MoveType.After,
                i => i.MatchLdcI4(12),
                i => i.MatchLdcI4(-1),
                i => i.MatchLdcI4(-1),
                i => i.MatchLdcI4(1),
                i => i.MatchLdcR4(1),
                i => i.MatchLdcR4(0),
                i => i.MatchCall(typeof(SoundEngine).GetMethod("PlaySound", BindingFlags.Static | BindingFlags.NonPublic, [typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(float)])),
                i => i.MatchPop()))
            {
                ModContent.GetInstance<SPYoyoMod>().Logger.Warn($"IL edit \"{nameof(StrangeDrinkNurseButtonImplemention)}..{nameof(IL_Main.GUIChatDrawInner)}..{nameof(ModifyOnClickButton)}\" failed...");
                return;
            }

            c.EmitDelegate(() =>
            {
                if (!IsPlayerTalksWithNurse())
                    return true;

                return OnClickNurseFirstButton();
            });

            var label = c.DefineLabel();

            c.Emit(OpCodes.Brtrue, label);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(label);
        }
    }
}
