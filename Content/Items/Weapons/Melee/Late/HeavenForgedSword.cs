using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

/// <summary>
/// First breakthrough in more complicated coding
/// </summary>
public class HeavenForgedSword : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavenForgedSword);

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 40;
        Item.damage = 660;
        Item.knockBack = 11f;
        Item.width = 128;
        Item.height = 116;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.value = AdditionsGlobalItem.RarityPurpleBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<HeavenForgedSwing>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(11, 113, 153));
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<RejuvenatedHolySword>(), 1);
        recipe.AddIngredient(ItemID.LunarBar, 12);
        recipe.AddIngredient(ItemID.SoulofSight, 10);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}