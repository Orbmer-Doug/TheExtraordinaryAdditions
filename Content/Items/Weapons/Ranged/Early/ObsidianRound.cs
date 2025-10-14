using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class ObsidianRound : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ObsidianRound);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 99;
    }

    public override void SetDefaults()
    {
        Item.damage = 10;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 8;
        Item.height = 8;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.shoot = ModContent.ProjectileType<ObsidianShot>();
        Item.shootSpeed = 16f;
        Item.ammo = AmmoID.Bullet;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(60);
        recipe.AddIngredient(ItemID.MusketBall, 60);
        recipe.AddIngredient(ItemID.Obsidian, 3);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
