using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class CryingEyeOfCthulhu : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CryingEyeOfCthulhu);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(138, 179, 255));
    }
    public override void SetStaticDefaults()
    {
        // These are all related to gamepad controls and don't seem to affect anything else
        ItemID.Sets.Yoyo[Item.type] = true; // Used to increase the gamepad range when using Strings.
        ItemID.Sets.GamepadExtraRange[Item.type] = 15; // Increases the gamepad range. Some vanilla values: 4 (Wood), 10 (Valor), 13 (Yelets), 18 (The Eye of Cthulhu), 21 (Terrarian).
        ItemID.Sets.GamepadSmartQuickReach[Item.type] = true; // Unused, but weapons that require aiming on the screen are in this set.
    }

    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 26;

        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = 25;
        Item.useAnimation = 25;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.UseSound = SoundID.Item1;

        Item.damage = 120;
        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.knockBack = 2.1f;
        Item.channel = true;
        Item.rare = ItemRarityID.Cyan;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;

        Item.shoot = ModContent.ProjectileType<CryingEye>();
        Item.shootSpeed = 20f;
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