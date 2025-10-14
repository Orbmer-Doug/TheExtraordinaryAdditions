using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;


namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class EmblazenedEmber : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EmblazenedEmber);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(171, 78, 12));
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(10, 11, false));

        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void PostUpdate()
    {
        float brightness = Main.essScale * Main.rand.NextFloat(0.9f, 1.1f);
        Lighting.AddLight(Item.Center, 1.2f * brightness, .66f * brightness, .02f * brightness);
    }

    public override void SetDefaults()
    {
        Item.width = 25;
        Item.height = 51;
        Item.rare = ItemRarityID.Yellow;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(silver: 60);
    }
}
