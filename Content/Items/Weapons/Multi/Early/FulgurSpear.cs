using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Multi.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Multi.Early;

public class FulgurSpear : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FulgurSpear);

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useAnimation = Item.useTime = 36;
        Item.shootSpeed = 42f;
        Item.knockBack = 3f;
        Item.width = 16;
        Item.height = 16;
        Item.UseSound = null;
        Item.shoot = ModContent.ProjectileType<FulgurSwing>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.rare = ItemRarityID.Orange;
        Item.damage = 30;
        Item.DamageType = DamageClass.Melee;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(245, 242, 66));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.ThunderSpear, 1);
        recipe.AddIngredient(ItemID.ThunderStaff, 1);
        recipe.AddIngredient(ItemID.RainCloud, 30);
        recipe.AddIngredient(ItemID.HellstoneBar, 12);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}