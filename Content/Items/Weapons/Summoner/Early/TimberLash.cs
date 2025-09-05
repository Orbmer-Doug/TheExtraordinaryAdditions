using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Early;

public class TimberLash : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TimberLash);
    public override void SetDefaults()
    {
        Item.damage = 11;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 42;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ItemRarityID.White;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<TimberWhip>();
        Item.shootSpeed = 1f;
        Item.knockBack = 1f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(201, 142, 32));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Wood, 24);
        recipe.AddRecipeGroup(RecipeGroupID.IronBar, 4);
        recipe.AddIngredient(ItemID.Cobweb, 15);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}