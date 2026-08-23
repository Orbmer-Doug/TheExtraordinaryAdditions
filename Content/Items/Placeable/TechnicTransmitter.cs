using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class TechnicTransmitter : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.TechnicTransmitter.Path;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.width = Item.height = 12;
        Item.maxStack = 9999;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<TechnicTransmitterPlaced>();
        Item.rare = ModContent.RarityType<CyberneticRarity>();
    }

    public override void AddRecipes()
    {
        //TODO
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Glass, 40);
        recipe.AddIngredient(ItemID.Wire, 120);
        recipe.Register();
    }
}
