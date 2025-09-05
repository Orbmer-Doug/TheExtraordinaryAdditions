using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;

public class CrystallineSnapcurve : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrystallineSnapcurve);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public const int TotalTime = 100;
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.LightCyan);
    }
    public override void SetDefaults()
    {
        Item.damage = 40;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 32;
        Item.height = 74;
        Item.useTime = Item.useAnimation = TotalTime;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 1f;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.shoot = ProjectileID.PurificationPowder;
        Item.shootSpeed = 10f;
        Item.useAmmo = AmmoID.Arrow;
        Item.useTurn = true;
        Item.value = AdditionsGlobalItem.RarityGreenBuyPrice;
        Item.rare = ItemRarityID.Green;
    }
    public override bool CanShoot(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Amethyst, 1);
        recipe.AddIngredient(ItemID.Topaz, 1);
        recipe.AddIngredient(ItemID.Sapphire, 1);
        recipe.AddIngredient(ItemID.Emerald, 1);
        recipe.AddIngredient(ItemID.Ruby, 1);
        recipe.AddIngredient(ItemID.Diamond, 1);
        recipe.AddIngredient(ItemID.StoneBlock, 50);
        recipe.AddIngredient(ItemID.Cobweb, 25);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}