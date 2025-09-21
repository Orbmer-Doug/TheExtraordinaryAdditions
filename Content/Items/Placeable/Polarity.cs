using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class Polarity : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Polarity);
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<PolarityPlaced>());

        Item.width = 120;
        Item.height = 90;
        Item.maxStack = 99;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(0, 1, 0, 0);
    }
}
