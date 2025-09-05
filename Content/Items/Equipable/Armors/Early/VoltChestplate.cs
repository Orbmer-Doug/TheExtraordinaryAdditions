using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Early;

[AutoloadEquip(EquipType.Body)]
public class VoltChestplate : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VoltChestplate);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(231, 191, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 26;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.defense = 6;
        Item.rare = ItemRarityID.Orange;
    }

    public override void UpdateEquip(Player player)
    {
        Lighting.AddLight(player.Center, new Color(206, 125, 255).ToVector3() * .33f);

        player.statManaMax2 += 40;
        player.GetCritChance<MeleeDamageClass>() += 6f;
        player.GetCritChance<MagicDamageClass>() += 6f;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<ShockCatalyst>(), 15);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }

}
