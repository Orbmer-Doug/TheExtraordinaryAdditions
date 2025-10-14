using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Multi.Early;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Multi.Early;

public class ChainStrikeJavelin : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ChainStrikeJavelin);

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 99;
    }

    public override void SetDefaults()
    {
        Item.width = 54;
        Item.damage = 22;
        Item.noMelee = true;
        Item.consumable = true;
        Item.noUseGraphic = true;
        Item.useAnimation = Item.useTime = 26;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 0f;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.height = 54;
        Item.maxStack = Item.CommonMaxStack;
        Item.value = Item.buyPrice(0, 0, 0, 60);
        Item.rare = ItemRarityID.Orange;
        Item.shoot = ModContent.ProjectileType<ShockJavelin>();
        Item.shootSpeed = 6.2f;
        Item.DamageType = DamageClass.Melee;
        Item.mana = 3;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(231, 191, 255));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile((IEntitySource)(object)source, position, velocity, type, damage, knockback, player.whoAmI, 0f, 0f, 0f);
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe(150);
        recipe.AddIngredient(ModContent.ItemType<ShockCatalyst>(), 1);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}
