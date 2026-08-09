using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class TorrentialTides : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TorrentialTides);

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 60;
        Item.damage = 1850;
        Item.knockBack = 4.5f;
        Item.width = 132;
        Item.height = 328;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.rare = ModContent.RarityType<BrackishRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = ModContent.GetInstance<MeleeNoSpeedDamageClass>();
        Item.shoot = ModContent.ProjectileType<TorrentialCleave>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(96, 143, 181));
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Flairon, 1);
        //TODO
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
