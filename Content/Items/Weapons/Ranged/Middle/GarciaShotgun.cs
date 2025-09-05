using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class GarciaShotgun : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GarciaShotgun);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void SetDefaults()
    {
        // Common Properties
        Item.width = 96;
        Item.height = 22;
        Item.rare = ItemRarityID.Yellow;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;

        // Use Properties
        Item.useTime = 80;
        Item.useAnimation = 80;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.UseSound = null;

        // Weapon Properties
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 88;
        Item.knockBack = 1.9f;
        Item.noMelee = true;

        // Gun Properties
        Item.shoot = ModContent.ProjectileType<GarciaShotgunHoldout>();
        Item.shootSpeed = 20f;
        Item.useAmmo = AmmoID.Bullet;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Shotgun, 1);
        recipe.AddIngredient(ItemID.OrangeBloodroot, 2);
        recipe.AddIngredient(ItemID.SoulofFright, 12);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }

    public override bool CanShoot(Player player) => false;
}