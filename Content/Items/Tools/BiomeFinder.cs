using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class BiomeFinder : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BiomeFinder);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }
    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 28;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.shootSpeed = 15f;
        Item.autoReuse = true;
        Item.maxStack = 1;
        Item.consumable = false;
        Item.UseSound = SoundID.Item1;
        Item.useAnimation = 15;
        Item.useTime = 15;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.RarityGreenBuyPrice;
        Item.rare = ItemRarityID.Green;
    }
    public override bool CanShoot(Player player) => false;
    public override bool CanUseItem(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddRecipeGroup(AdditionsRecipes.AnyGoldBar, 10);
        recipe.AddIngredient(ItemID.Topaz, 3);
        recipe.AddIngredient(ItemID.Sapphire, 5);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
