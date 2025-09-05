using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class ComicallyLargeKnife : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ComicallyLargeKnife);
    public override void SetDefaults()
    {
        Item.width = 62;
        Item.height = 60;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.useTime = Item.useAnimation = 29;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = .6f;
        Item.autoReuse = true;
        Item.damage = 180;
        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<KnifeStab>();
    }
    public override void HoldItem(Player player)
    {

        player.Additions().SyncMouse = true;
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
        recipe.AddIngredient(ItemID.PsychoKnife, 1);
        recipe.AddIngredient(ItemID.BatBat, 1);
        recipe.AddIngredient(ItemID.BreakerBlade, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}