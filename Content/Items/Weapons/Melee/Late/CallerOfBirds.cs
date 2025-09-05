using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class CallerOfBirds : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CallerOfBirds);
    public override void SetDefaults()
    {
        Item.damage = 510;
        Item.DamageType = DamageClass.Melee;
        Item.width = 28;
        Item.height = 52;
        Item.maxStack = 1;
        Item.useTime = 6;
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 2;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.rare = ItemRarityID.Purple;
        Item.UseSound = null;
        Item.noMelee = false;
        Item.shoot = ModContent.ProjectileType<CallerOfBirdsCall>();
        Item.shootSpeed = 20f;
        Item.autoReuse = true;
        Item.channel = true;
        Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(64, 81, 219));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Meowmere, 1);
        recipe.AddIngredient(ItemID.Hay, 150);
        recipe.AddIngredient(ItemID.Bird, 15);
        recipe.AddIngredient(ItemID.RainCloud, 30);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
