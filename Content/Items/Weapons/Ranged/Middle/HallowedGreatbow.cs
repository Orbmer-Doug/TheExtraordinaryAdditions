using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class HallowedGreatbow : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HallowedGreatbow);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 82;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 30;
        Item.height = 68;
        Item.useTime = Item.useAnimation = 35;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = false;
        Item.knockBack = 1;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.UseSound = SoundID.Item5;
        Item.shoot = ModContent.ProjectileType<HallowedGreatbowHeld>();
        Item.useAmmo = AmmoID.Arrow;
        Item.autoReuse = true;
        Item.shootSpeed = 2f;
        Item.crit = 20;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 229, 84));
    }

    public override bool CanShoot(Player player) => false;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.HallowedBar, 12);
        recipe.AddIngredient(ItemID.SoulofLight, 8);
        recipe.AddIngredient(ItemID.FallenStar, 10);
        recipe.AddIngredient(ItemID.Ruby, 5);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}