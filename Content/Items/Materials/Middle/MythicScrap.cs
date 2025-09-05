using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class MythicScrap : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MythicScrap);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(97, 102, 0));
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 20;
    }

    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 25;
        Item.rare = ItemRarityID.Yellow;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(silver: 75);
    }
}
