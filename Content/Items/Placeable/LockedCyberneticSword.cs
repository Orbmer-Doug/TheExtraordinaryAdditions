using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Content.Tiles;

namespace TheExtraordinaryAdditions.Content.Items.Placeable;

public class LockedCyberneticSword : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LockedCyberneticSword);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<LockedCyberneticPedestal>());
        Item.width = 130;
        Item.height = 140;
        Item.damage = 7;
        Item.maxStack = 1;
        Item.rare = ModContent.RarityType<UniqueRarity>();
        Item.value = Item.buyPrice(0, 50);
    }
}