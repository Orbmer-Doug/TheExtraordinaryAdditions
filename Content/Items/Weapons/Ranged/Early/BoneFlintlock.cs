using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class BoneFlintlock : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BoneFlintlock);

    public override void SetDefaults()
    {
        Item.damage = 14;
        Item.knockBack = 1f;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 46;
        Item.height = 22;
        Item.scale = 1f;
        Item.maxStack = 1;
        Item.useTime = Item.useAnimation = 15;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.rare = ItemRarityID.Orange;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.UseSound = SoundID.Item11;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<BoneFlintlockHeld>();
        Item.useAmmo = AmmoID.Bullet;
        Item.shootSpeed = 6f;
        Item.autoReuse = true;
    }
    public override bool CanShoot(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.FlintlockPistol, 1);
        recipe.AddIngredient(ItemID.Cobweb, 25);
        recipe.AddIngredient(ItemID.Bone, 30);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}