using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

/// <summary>
/// First item in the mod. (was just holy sword) <br></br>
/// 7/8/2023
/// </summary>
public class RejuvenatedHolySword : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RejuvenatedHolySword);

    public override void SetDefaults()
    {
        Item.width = 62;
        Item.height = 70;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;
        Item.rare = ItemRarityID.Yellow;
        Item.useTime = Item.useAnimation = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 4f;
        Item.damage = 245;
        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<HolySwordSwing>();
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        HolySwordSwing swing = Main.projectile[Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 0f)].As<HolySwordSwing>();
        swing.Mark = player.Additions().SafeMouseRight.Current;
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.HallowedBar, 15);
        recipe.AddIngredient(ItemID.Ruby, 3);
        recipe.AddIngredient(ItemID.SoulofLight, 8);
        recipe.AddIngredient(ItemID.Ectoplasm, 12);
        recipe.AddIngredient(ItemID.BrokenHeroSword, 1);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}