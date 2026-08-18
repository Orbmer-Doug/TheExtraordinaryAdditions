using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Zenith;
using TheExtraordinaryAdditions.Content.Rarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class FinalStrike : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.FinalStrike.Path;

    public override void SetDefaults()
    {
        Item.width = 138;
        Item.height = 140;
        Item.damage = 370;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.useAnimation = 50;
        Item.useTime = 50;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 9f;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<FinalStrikeHoldout>();
        Item.shootSpeed = 20f;
        Item.DamageType = DamageClass.Melee;

        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
        Item.rare = ModContent.RarityType<LegendaryRarity>();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override bool CanUseItem(Player player)
    {
        if (FindProjectile(out Projectile spear, Item.shoot, player.whoAmI))
        {
            if ((int) spear.ai[0] == (int) FinalStrikeHoldout.FinalStrikeState.Aim)
                return false;
        }

        return true;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockback)
    {
        if (player.altFunctionUse != 2)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<FinalStrikeHoldout>(),
                damage, knockback, player.whoAmI);
            return false;
        }

        Projectile spear =
            Main.projectile[
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<FinalStrikeHoldout>(),
                    damage * 2, knockback, player.whoAmI)];
        spear.As<FinalStrikeHoldout>().CurrentState = FinalStrikeHoldout.FinalStrikeState.Stab;

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Spear);
        recipe.AddIngredient(ItemID.DarkLance);
        recipe.AddIngredient(ItemID.Gungnir);
        recipe.AddIngredient(ItemID.NorthPole);
        recipe.AddIngredient(ItemID.DayBreak);
        recipe.AddIngredient(ModContent.ItemType<CondereFulmina>());
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
