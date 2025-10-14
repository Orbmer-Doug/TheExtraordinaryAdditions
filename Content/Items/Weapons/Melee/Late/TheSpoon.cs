using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class TheSpoon : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheSpoon);

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 60;
        Item.damage = 800;
        Item.knockBack = 40.5f;
        Item.width = Item.height = 60;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.rare = ItemRarityID.Purple;
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<SpoonSwing>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(200, 113, 15));
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.HallowedBar, 15);
        recipe.AddIngredient(ItemID.LunarOre, 50);
        recipe.AddIngredient(ItemID.FragmentSolar, 16);
        recipe.AddIngredient(ItemID.SoulofFright, 14);
        recipe.AddIngredient(ItemID.SoulofMight, 12);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}