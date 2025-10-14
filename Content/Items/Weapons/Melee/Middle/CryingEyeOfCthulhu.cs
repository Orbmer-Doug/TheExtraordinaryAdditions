using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class CryingEyeOfCthulhu : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CryingEyeOfCthulhu);

    public override void SetStaticDefaults()
    {
        // These are all related to gamepad controls
        ItemID.Sets.Yoyo[Item.type] = true;
        ItemID.Sets.GamepadExtraRange[Item.type] = 15;
        ItemID.Sets.GamepadSmartQuickReach[Item.type] = true;
    }

    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = 25;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.UseSound = SoundID.Item1;
        Item.damage = 102;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.knockBack = 2.1f;
        Item.channel = true;
        Item.rare = ItemRarityID.Cyan;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.shoot = ModContent.ProjectileType<CryingEye>();
        Item.shootSpeed = 20f;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(138, 179, 255));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.TheEyeOfCthulhu, 1);
        recipe.AddIngredient(ItemID.Kraken, 1);
        recipe.AddIngredient(ItemID.FragmentVortex, 12);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}