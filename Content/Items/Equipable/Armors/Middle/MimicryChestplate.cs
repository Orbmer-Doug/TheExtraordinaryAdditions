using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Body)]
public class MimicryChestplate : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MimicryChestplate);
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
        Item.defense = 22;
    }
    public override void UpdateEquip(Player player)
    {
        player.GetDamage(DamageClass.Generic) += 0.09f;
        player.GetCritChance<GenericDamageClass>() += 10f;
    }
}