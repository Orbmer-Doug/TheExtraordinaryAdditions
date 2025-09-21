using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Tiles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class GreenBlock : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GreenBlock);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(32, 117, 29));
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 100;
        ItemID.Sets.ExtractinatorMode[Item.type] = Item.type;
    }

    public override void SetDefaults()
    {
        Item.value = 100;
        Item.DefaultToPlaceableTile(ModContent.TileType<GreenBlockPlaced>());
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.GrassSeeds, 4);
        recipe.AddTile(TileID.WorkBenches);
        recipe.Register();
    }

    public override void ExtractinatorUse(int extractinatorBlockType, ref int resultType, ref int resultStack)
    { 
        if (Main.rand.NextBool(3))
        {
            resultType = ItemID.GrassSeeds; 
            if (Main.rand.NextBool(5))
            {
                resultStack += Main.rand.Next(2);
            }
        }
    }
}
