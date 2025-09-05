using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Early;

public class StellarKunai : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.StellarKunai);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        Utility.ColorLocalization(tooltips, Color.CornflowerBlue);
    }
    public override void SetDefaults()
    {
        Item.width = Item.height = 48;
        Item.damage = 22;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.useAnimation = Item.useTime = 15;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 5.5f;
        Item.UseSound = null;
        Item.value = AdditionsGlobalItem.RarityGreenBuyPrice;
        Item.rare = ItemRarityID.Green;
        Item.shoot = ModContent.ProjectileType<StellarKunaiProj>();
        Item.shootSpeed = 3f;
        Item.DamageType = DamageClass.Summon;
        Item.autoReuse = true;
    }

    public override bool CanUseItem(Player player)
    {
        return player.CountOwnerProjectiles(Item.shoot) <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    public override void AddRecipes()
    {
        CreateRecipe(1)
            .AddIngredient(ItemID.SunplateBlock, 25)
            .AddIngredient(ItemID.FallenStar, 7)
            .AddIngredient(ItemID.Chain, 12)
            .AddTile(TileID.SkyMill)
            .Register();
    }
}
