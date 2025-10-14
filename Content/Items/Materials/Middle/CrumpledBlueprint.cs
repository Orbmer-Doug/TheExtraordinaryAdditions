using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class CrumpledBlueprint : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrumpledBlueprint);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 2;
    }

    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 26;
        Item.rare = ItemRarityID.Cyan;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(silver: 60);
    }
}
