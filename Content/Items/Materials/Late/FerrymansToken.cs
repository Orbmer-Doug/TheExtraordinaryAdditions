using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Late;

public class FerrymansToken : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FerrymansToken);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.width = Item.height = 38;
        Item.rare = ItemRarityID.Blue;
        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(gold: 5);
    }
}
