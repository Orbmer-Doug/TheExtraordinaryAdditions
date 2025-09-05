using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;
using TheExtraordinaryAdditions.Content.Tiles;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;

public class FlagPole : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlagPole);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 5;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<FlagPolePlaced>());
        Item.width = 12;
        Item.height = 12;
        Item.value = Item.sellPrice(0, 0, 50, 0);
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        {
            recipe.AddRecipeGroup("AnyIronBar", 18);
            recipe.AddRecipeGroup("AnyGoldBar", 6);
            recipe.AddIngredient(ItemID.Silk, 16);
        }
        recipe.AddTile(TileID.Sawmill);
        recipe.Register();
    }
}