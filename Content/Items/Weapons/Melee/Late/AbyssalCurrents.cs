using CalamityMod.Items.Materials;
using CalamityMod.Items.Placeables;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

/// <summary>
/// Farewell, abysslon
/// </summary>
public class AbyssalCurrents : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbyssalCurrent);

    public static readonly Color[] WaterPalette = new Color[]
    {
        Color.Turquoise,
        Color.DeepSkyBlue,
        Color.Aquamarine,
        Color.Aqua,
        Color.Azure,
        Color.CornflowerBlue,
    };

    public static readonly Color[] BrackishPalette = new Color[]
    {
        new(0, 99, 219),
        new(0, 82, 181),
        new(20, 86, 166),
        new(10, 71, 145),
        new(26, 103, 196)
    };

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(WaterPalette[2]);
    }

    public override void SetDefaults()
    {
        Item.rare = ModContent.RarityType<BrackishRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useAnimation = 37;
        Item.useTime = 37;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.consumable = false;
        Item.width = Item.height = 134;
        Item.damage = 1850;
        Item.knockBack = 0f;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.DamageType = DamageClass.Melee;
        Item.crit = 0;
        Item.shootSpeed = 35f;
        Item.shoot = ModContent.ProjectileType<AbyssalCurrentsHoldout>();
        Item.channel = true;
    }

    public override bool CanShoot(Player player) => true;
    public override bool AltFunctionUse(Player player) => true;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        AbyssalCurrentsHoldout hold = Main.projectile[Projectile.NewProjectile(source, position, velocity, Item.shoot, damage, knockback, player.whoAmI)].As<AbyssalCurrentsHoldout>();
        if (player.altFunctionUse == ItemAlternativeFunctionID.ActivatedAndUsed)
        {
            hold.State = AbyssalCurrentsHoldout.AbyssalState.Spin;
            hold.Projectile.localNPCHitCooldown = 12;
        }
        else
            hold.Projectile.localNPCHitCooldown = -1;
        return false;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.RazorbladeTyphoon, 1);
        recipe.AddIngredient(ModContent.ItemType<Lumenyl>(), 18);
        recipe.AddIngredient(ModContent.ItemType<Voidstone>(), 150);
        recipe.AddIngredient(ItemID.LunarBar, 10);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}