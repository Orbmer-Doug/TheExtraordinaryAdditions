using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class FulguriteInAJar : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FulguriteInAJar);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }

    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 24;
        Item.rare = ItemRarityID.Pink;
        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(silver: 60);
    }
}
