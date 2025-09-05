using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Equipable.Armors.Middle;

[AutoloadEquip(EquipType.Body)]
public class BlueTuxedo : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BlueTuxedo);
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
        Item.defense = 23;
    }
    public override void UpdateEquip(Player player)
    {
        player.GetAttackSpeed(DamageClass.Melee) += 0.3f;
        player.buffImmune[BuffID.VortexDebuff] = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ShroomiteBar, 20);
        recipe.AddIngredient(ItemID.TuxedoShirt, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}