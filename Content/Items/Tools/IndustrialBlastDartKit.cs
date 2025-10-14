using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class IndustrialBlastDartKit : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.IndustrialBlastDartKit);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.ItemsThatCountAsBombsForDemolitionistToSpawn[Type] = true;
        Item.ResearchUnlockCount = 1;
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.shootSpeed = 15f;
        Item.shoot = ModContent.ProjectileType<ProximityDart>();
        Item.width = 30;
        Item.height = 40;
        Item.autoReuse = true;
        Item.maxStack = 1;
        Item.consumable = false;
        Item.UseSound = SoundID.Item1;
        Item.useAnimation = 15;
        Item.useTime = 15;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.rare = ItemRarityID.Orange;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<ProximityDart>(), damage, knockback);
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddRecipeGroup(RecipeGroupID.IronBar, 65);
        recipe.AddIngredient(ItemID.Dynamite, 99);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}