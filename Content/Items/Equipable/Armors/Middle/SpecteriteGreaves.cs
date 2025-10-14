using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Legs)]
public class SpecteriteGreaves : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SpecteriteGreaves);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(85, 77, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
        Item.defense = 8;
    }

    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Ranged) += 10f;
        player.moveSpeed += 0.5f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ShroomiteLeggings, 1);
        recipe.AddIngredient(ItemID.Ectoplasm, 13);
        recipe.AddIngredient(ItemID.HermesBoots, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.AddTile(TileID.AdamantiteForge);
        recipe.Register();
    }
}