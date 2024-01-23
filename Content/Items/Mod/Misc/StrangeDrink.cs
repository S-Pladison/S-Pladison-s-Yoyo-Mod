using SPYoyoMod.Content.Items.Mod.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace SPYoyoMod.Content.Items.Mod.Misc
{
    public class StrangeDrinkItem : ModItem
    {
        public override string Texture { get => ModAssets.ItemsPath + "StrangeDrink"; }

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
            LoadSetChatButtonsHook();
            LoadOnChatButtonClickedHook();
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

        public static void LoadSetChatButtonsHook()
        {
            var setChatButtonsMethodInfo = typeof(NPCLoader).GetMethod(nameof(NPCLoader.SetChatButtons));

            MonoModHooks.Add(setChatButtonsMethodInfo, (orig_SetChatButtonsMethod orig, ref string button, ref string button2) =>
            {
                orig(ref button, ref button2);

                var player = Main.player[Main.myPlayer];

                if (!IsPlayerTalksWithNurse(player) || !player.HasItem(GiftForNurseType)) return;

                button2 = GiftButtonText.Value;
            });
        }

        public static void LoadOnChatButtonClickedHook()
        {
            var onChatButtonClickedMethodInfo = typeof(NPCLoader).GetMethod(nameof(NPCLoader.OnChatButtonClicked));

            MonoModHooks.Add(onChatButtonClickedMethodInfo, (orig_OnChatButtonClickedMethod orig, bool firstButton) =>
            {
                var player = Main.LocalPlayer;

                if (!IsPlayerTalksWithNurse(player) || firstButton)
                {
                    orig(firstButton);
                    return;
                }

                var slotIndex = player.FindItem(GiftForNurseType);
                var hasGiftForNurse = slotIndex >= 0;

                if (!hasGiftForNurse)
                {
                    orig(firstButton);
                    return;
                }

                Main.npcChatText = GiftDialogueText.Value;

                player.inventory[slotIndex].TurnToAir();
                player.QuickSpawnItem(Main.LocalPlayer.GetSource_GiftOrReward(), GiftForPlayerType);

                orig(firstButton);
            });
        }

        public static bool IsPlayerTalksWithNurse(Player player)
        {
            return player.talkNPC >= 0 && Main.npc[player.talkNPC].type.Equals(NPCID.Nurse);
        }

        private delegate void orig_SetChatButtonsMethod(ref string button, ref string button2);
        private delegate void orig_OnChatButtonClickedMethod(bool firstButton);
    }
}