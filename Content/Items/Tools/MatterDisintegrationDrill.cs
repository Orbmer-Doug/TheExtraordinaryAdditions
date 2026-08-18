using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class MatterDisintegrationDrill : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.MatterDisintegrationCannon.Path;

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(227, 170, 36));
    }

    public override void SetDefaults()
    {
        Item.damage = 500;
        Item.knockBack = 0f;
        Item.useTime = 1;
        Item.useAnimation = 25;
        Item.pick = 1000;
        Item.DamageType = DamageClass.Melee;
        Item.width = 90;
        Item.height = 26;
        Item.channel = Item.noUseGraphic = Item.noMelee = Item.autoReuse = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ItemRarityID.Red;
        Item.UseSound = SoundID.Item23;
        Item.shoot = ModContent.ProjectileType<CannonHoldout>();
        Item.shootSpeed = 40f;
        Item.tileBoost = 56;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.LunarBar, 15);
        recipe.AddIngredient(ModContent.ItemType<PlasmaCore>());
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
