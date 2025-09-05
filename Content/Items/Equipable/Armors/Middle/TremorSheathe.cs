using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Legs)]
public class TremorSheathe : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TremorSheathe);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(194, 194, 194));
    }

    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.defense = 16;
    }

    public override void UpdateEquip(Player player)
    {
        player.endurance += .05f;
        player.statLifeMax2 += 50;
        player.GetDamage(DamageClass.Magic) += .1f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 7);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.AddTile(TileID.HeavyWorkBench);
        recipe.Register();
    }

}