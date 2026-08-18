using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class LockedCyberneticSword : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.LockedCyberneticSword.Path;

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<LockedCyberneticPedestal>());
        Item.width = 130;
        Item.height = 140;
        Item.maxStack = 1;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = Item.buyPrice(0, 50);
    }
}
