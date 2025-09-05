using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class AlucardsSword : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AlucardsSword);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(232, 35, 0));
    }
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 18;
        Item.rare = ItemRarityID.Cyan;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;

        Item.useTime = 30;
        Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.UseSound = SoundID.Item10;

        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.damage = 145;
        Item.knockBack = .1f;
        Item.noMelee = true;

        Item.shoot = ModContent.ProjectileType<AlucardsSwordThrow>();
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.shootSpeed = 15;
        Item.ArmorPenetration = 10;
        Item.crit = 10;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        for (int i = 0; i < 1; i++)
        {
            Vector2 newVelocity = velocity.RotatedByRandom(MathHelper.ToRadians(40));

            newVelocity *= 1f - Main.rand.NextFloat(0.5f);
            Projectile.NewProjectileDirect(source, player.Center, newVelocity, type, damage / 2, knockback, player.whoAmI);
        }
        return false;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 10);
        recipe.AddIngredient(ItemID.OrangeBloodroot, 2);
        recipe.AddTile(TileID.BloodMoonMonolith);
        recipe.Register();
    }
}