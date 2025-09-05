using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class Atorcoppe : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Atorcoppe);

    public override void SetDefaults()
    {
        Item.damage = 47;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 42;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ItemRarityID.LightRed;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<AttorcoppeProjectile>();
        Item.shootSpeed = 1f;
        Item.knockBack = 2f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
        player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI, 0f, 0f, 1f);

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.SpiderFang, 16);
        recipe.AddIngredient(ItemID.Cobweb, 200);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }

    public override bool MeleePrefix()
    {
        return true;
    }
}