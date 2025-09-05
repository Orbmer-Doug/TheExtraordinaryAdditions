using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class StygianHeartTrophy : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StygianHeartTrophy);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<StygianHeartTrophyTile>());
        Item.width = 32;
        Item.height = 32;
        Item.rare = ItemRarityID.Blue;
        Item.value = Item.buyPrice(0, 1);
    }
}

