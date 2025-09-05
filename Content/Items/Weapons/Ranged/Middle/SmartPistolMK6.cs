using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Misc;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class SmartPistolMK6 : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SmartPistolMK6);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.OrangeRed);
    }
    public override void SetDefaults()
    {
        Item.damage = 525;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 48;
        Item.height = 30;
        Item.useTime = 26;
        Item.useAnimation = 26;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<SmartPistolHeld>();
        Item.shootSpeed = 2f;
        Item.crit = 0;
        Item.noUseGraphic = true;
    }
    public override bool CanShoot(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Handgun, 1);
        recipe.AddIngredient(ItemID.FragmentVortex, 16);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
