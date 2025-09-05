using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Body)]
public class TremorPlating : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TremorPlating);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(194, 194, 194));
    }

    public override void SetDefaults()
    {
        Item.width = 44;
        Item.height = 30;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.defense = 37;
    }

    public override void UpdateEquip(Player player)
    {
        player.endurance += .05f;
        player.statLifeMax2 += 20;
        player.GetCritChance(DamageClass.Magic) += 8f;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 10);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.AddTile(TileID.HeavyWorkBench);
        recipe.Register();
    }
}