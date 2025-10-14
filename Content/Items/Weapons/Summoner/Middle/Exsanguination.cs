using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class Exsanguination : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Exsanguination);

    public override void SetDefaults()
    {
        Item.DefaultToWhip(ModContent.ProjectileType<ExsanguinationProj>(), 20, 6f, 4f, 35);
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.damage = 170;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override bool MeleePrefix() => true;
}
