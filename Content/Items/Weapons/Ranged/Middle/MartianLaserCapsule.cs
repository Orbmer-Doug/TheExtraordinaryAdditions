using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class MartianLaserCapsule : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MartianLaserCapsule);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 99;
    }

    public override void SetDefaults()
    {
        Item.damage = 14;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 8;
        Item.height = 8;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.rare = ItemRarityID.Yellow;
        Item.shoot = ModContent.ProjectileType<MartianCapsule>();
        Item.shootSpeed = 5f;
        Item.ammo = AmmoID.Bullet;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(30);
        recipe.AddIngredient(ItemID.ChlorophyteBullet, 30);
        recipe.AddIngredient(ItemID.MartianConduitPlating, 10);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}