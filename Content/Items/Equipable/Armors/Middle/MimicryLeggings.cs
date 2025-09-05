using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Legs)]
public class MimicryLeggings : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MimicryLeggings);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override void SetDefaults()
    {
        Item.width = 29;
        Item.height = 21;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.defense = 15;
    }
    public override void UpdateEquip(Player player)
    {
        player.moveSpeed += 0.45f;
        player.GetCritChance<GenericDamageClass>() += 4f;
    }
}