using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Early;

public class RampantShields : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RampantShields);

    public override void SetStaticDefaults()
    {
        ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.damage = 38;
        Item.knockBack = 3f;
        Item.mana = 10;
        Item.width = 38;
        Item.height = 40;
        Item.useTime = 16;
        Item.useAnimation = 16;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.UseSound = SoundID.Item44;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Summon;
        Item.buffType = ModContent.BuffType<FlockOfShields>();
        Item.shoot = ModContent.ProjectileType<EnchantedShield>();
    }

    public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
    {
        position = Main.MouseWorld;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.AddBuff(Item.buffType, 2);
        Projectile.NewProjectileDirect(source, position, velocity, type, damage, knockback, Main.myPlayer);
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddRecipeGroup(RecipeGroupID.IronBar, 16);
        recipe.AddIngredient(ItemID.FallenStar, 4);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
