using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class StygianEyeball : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StygianEyeball);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 62;
        Item.rare = ItemRarityID.Blue;
        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(gold: 5);
    }
}
