using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class Downpour : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Downpour);

    public override void SetDefaults()
    {
        Item.damage = 11;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 28;
        Item.height = 60;
        Item.scale = 1f;
        Item.maxStack = 1;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 0;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.UseSound = SoundID.Item21;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<DownpourHeld>();
        Item.useAmmo = AmmoID.Arrow;
        Item.shootSpeed = 10f;
        Item.autoReuse = true;
    }

    public override bool? CanHitNPC(Player player, NPC target) => false;

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.WoodenBow, 1);
        recipe.AddIngredient(ItemID.RainCloud, 60);
        recipe.AddIngredient(ItemID.Feather, 7);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
