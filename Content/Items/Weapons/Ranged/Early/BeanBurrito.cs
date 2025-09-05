using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class BeanBurrito : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BeanBurrito);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(227, 112, 5));
    }
    public override void SetDefaults()
    {
        Item.width = 35;
        Item.height = 16;
        Item.scale = 1f;
        Item.rare = ItemRarityID.Red;
        Item.useAnimation = 12;
        Item.useTime = 4;
        Item.reuseDelay = 14;
        Item.consumeAmmoOnLastShotOnly = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.UseSound = SoundID.Item2;
        Item.DamageType = DamageClass.Ranged;
        Item.damage = 26;

        Item.knockBack = 0f;
        Item.noMelee = true;
        Item.shoot = ModContent.ProjectileType<BeanFire>();
        Item.shootSpeed = 10f;
    }
    public override void UpdateInventory(Player player)
    {
        player.miscCounter++;
        if (player.miscCounter % 20 == 19)
        {
            int rand = Main.rand.Next(1, 5);
            int rand2 = Main.rand.Next(0, 99);
            Item.value = Item.buyPrice(0, rand, rand2, 0);
        }
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        int amount = 1;
        int degrees = 1;
        float thevelocity = 1f;
        if (player.Additions().AshersTie || player.Additions().TungstenTie)
        {
            amount = 4;
            degrees = 15;
            thevelocity = 2f;
            damage *= 7;
        }
        for (int i = 0; i < amount; i++)
        {
            // Rotate the velocity randomly by 30 degrees at max.
            Vector2 newVelocity = velocity.RotatedByRandom(MathHelper.ToRadians(degrees));

            // Decrease velocity randomly for nicer visuals.
            newVelocity *= thevelocity - Main.rand.NextFloat(0.3f);

            // Create a projectile.
            Projectile.NewProjectileDirect(source, position, newVelocity, type, damage, knockback, player.whoAmI);
        }

        return false; // Return false because we don't want tModLoader to shoot projectile
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BeeWax, 15);
        recipe.AddIngredient(ItemID.Hay, 20);
        recipe.AddIngredient(ItemID.GoldCoin, 4);
        recipe.AddTile(TileID.WorkBenches);
        recipe.Register();
    }
}