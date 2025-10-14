using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Buff;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Consumable;

public class SupremeWaterbreathingPotion : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SupremeWaterbreathingPotion);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;

        // Dust that will appear in these colors when the item with ItemUseStyleID.DrinkLiquid is used
        ItemID.Sets.DrinkParticleColors[Type] = [
            new Color(235, 141, 0),
            new Color(222, 146, 31),
            new Color(227, 173, 91)
        ];
    }

    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 32;
        Item.useStyle = ItemUseStyleID.DrinkLiquid;
        Item.useAnimation = 15;
        Item.useTime = 15;
        Item.useTurn = true;
        Item.UseSound = SoundID.Item3;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.rare = ItemRarityID.Orange;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.buffType = ModContent.BuffType<SupremeWaterbreathing>();
        Item.buffTime = SecondsToFrames(260);
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.GillsPotion, 1);
        recipe.AddIngredient(ModContent.ItemType<CracklingFragments>(), 1);
        recipe.AddIngredient(ItemID.Fireblossom, 3);
        recipe.AddTile(TileID.AlchemyTable);
        recipe.Register();
    }
}
