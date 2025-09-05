using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Legs)]
public class BlueLeggings : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BlueLeggings);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.AliceBlue);
    }

    public override void SetDefaults()
    {
        Item.width = 29;
        Item.height = 21;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Lime;
        Item.defense = 18;
    }
    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Melee) += 25f;
        player.buffImmune[BuffID.Electrified] = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ShroomiteBar, 15);
        recipe.AddIngredient(ItemID.TuxedoPants, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}