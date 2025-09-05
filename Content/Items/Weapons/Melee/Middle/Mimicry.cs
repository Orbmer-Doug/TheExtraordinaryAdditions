using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class Mimicry : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Mimicry);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(193, 0, 0));
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 60;
        Item.damage = 1500;
        Item.knockBack = 1.5f;
        Item.width = Item.height = 20;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<MimicrySlash>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override bool AltFunctionUse(Player player) => true;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.Additions().MouseLeft.Current)
        {
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
        }
        else if (player.Additions().MouseRight.Current)
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<MimicrySpear>(), (int)(damage * 1.25f), knockback * 1.25f, player.whoAmI, 0, 0f, 0f);

        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.BloodButcherer, 1);
        recipe.AddIngredient(ModContent.ItemType<StygianEyeball>(), 1);
        recipe.AddIngredient(ItemID.FragmentSolar, 10);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}