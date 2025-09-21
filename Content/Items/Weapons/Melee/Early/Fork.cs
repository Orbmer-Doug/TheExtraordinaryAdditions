using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;

public class Fork : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Fork);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(100, 100, 100));
    }
    public override void SetDefaults()
    {
        // Common Properties
        Item.width = Item.height = 64;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.rare = ItemRarityID.LightRed;

        // Use Properties
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;

        // Weapon Properties
        Item.knockBack = 1;
        Item.autoReuse = true;
        Item.damage = 25;
        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.noUseGraphic = true;

        Item.shoot = ModContent.ProjectileType<ForkStab>();
        Item.shootSpeed = 4f;
    }
    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
        return false;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.IronShortsword, 4);
        recipe.AddIngredient(ItemID.Bone, 40);
        recipe.AddIngredient(ItemID.Silk, 3);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();

        Recipe recipe2 = CreateRecipe();
        recipe2.AddIngredient(ItemID.LeadShortsword, 4);
        recipe2.AddIngredient(ItemID.Bone, 40);
        recipe2.AddIngredient(ItemID.Silk, 3);
        recipe2.AddTile(TileID.Anvils);
        recipe2.Register();
    }
}