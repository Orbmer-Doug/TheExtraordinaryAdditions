//TODO

using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Vanity;

[AutoloadEquip(EquipType.Legs)]
public class AvatarLeggings : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.AvatarLeggings.Path;

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(156, 184, 253));
    }

    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 12;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.vanity = true;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Silk, 6);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
