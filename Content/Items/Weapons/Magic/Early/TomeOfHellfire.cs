using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;

public class TomeOfHellfire : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TomeOfHellfire);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 234, 224));
    }

    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 30;
        Item.rare = ItemRarityID.Orange;
        Item.useTime = 30;
        Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.UseSound = null;
        Item.DamageType = DamageClass.Magic;
        Item.damage = 19;
        Item.knockBack = .1f;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.shoot = ModContent.ProjectileType<HellfireHoldout>();
        Item.shootSpeed = 13f;
        Item.mana = 6;
        Item.channel = true;
        Item.noUseGraphic = true;
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Book, 1);
        recipe.AddIngredient(ItemID.HellstoneBar, 6);
        recipe.AddIngredient(ItemID.Fireblossom, 4);
        recipe.AddIngredient(ItemID.AshBlock, 120);
        recipe.AddTile(TileID.Bookcases);
        recipe.Register();
    }
}