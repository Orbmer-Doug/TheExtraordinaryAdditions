using CalamityMod.Items.Materials;
using CalamityMod.Rarities;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class FlorescenceRounds : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FlorescenceRounds);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 180;
    }

    public override void SetDefaults()
    {
        Item.damage = 3;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 8;
        Item.height = 8;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
        Item.rare = ModContent.RarityType<Turquoise>();
        Item.shoot = ModContent.ProjectileType<Florescence>();
        Item.shootSpeed = 20f;
        Item.ammo = AmmoID.Bullet;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(120);
        recipe.AddIngredient(ModContent.ItemType<UelibloomBar>(), 1);
        recipe.AddIngredient(ModContent.ItemType<LivingShard>(), 1);
        recipe.AddIngredient(ItemID.GrassSeeds, 5);
        recipe.AddIngredient(ItemID.ChlorophyteBullet, 60);
        recipe.AddCondition(Condition.NearWater);
        recipe.Register();
    }
}
