using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class HeavenForgedCannon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavenForgedCannon);
    public override void SetDefaults()
    {
        Item.damage = 1450;
        Item.DamageType = DamageClass.Ranged;
        Item.shoot = ModContent.ProjectileType<HeavenForgedHoldout>();
        Item.useTime = Item.useAnimation = 100;
        Item.shootSpeed = 25f;
        Item.knockBack = 20f;
        Item.width = 110;
        Item.height = 40;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.useAmmo = AmmoID.Rocket;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.useStyle = ItemUseStyleID.Shoot;
    }

    public override bool CanShoot(Player player) => false;

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(33, 170, 191));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.RocketLauncher, 1);
        recipe.AddIngredient(ItemID.SnowmanCannon, 1);
        recipe.AddIngredient(ItemID.LunarBar, 14);
        recipe.AddIngredient(ItemID.FragmentVortex, 16);
        recipe.AddIngredient(ItemID.SoulofSight, 10);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();

    }
}
