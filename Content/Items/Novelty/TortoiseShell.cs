using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Items.Novelty;

public class TortoiseShell : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TortoiseShell);
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 26;
        Item.rare = ItemRarityID.Lime;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(silver: 60);
    }
}
