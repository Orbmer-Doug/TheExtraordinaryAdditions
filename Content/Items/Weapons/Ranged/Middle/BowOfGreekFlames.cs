using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class BowOfGreekFlames : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BowOfGreekFlames);
    public override void SetDefaults()
    {
        Item.damage = 79;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 28;
        Item.height = 52;
        Item.maxStack = 1;
        Item.useTime = Item.useAnimation = 55;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 3;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.UseSound = SoundID.Item5;
        Item.useAmmo = AmmoID.Arrow;
        Item.noMelee = false;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<BowOfGreekFlamesHeld>();
        Item.shootSpeed = 30f;
        Item.autoReuse = true;
    }
    public override bool CanShoot(Player player) => false;
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(56, 237, 28));
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.MoltenFury, 1);
        recipe.AddIngredient(ItemID.CursedFlame, 16);
        recipe.AddIngredient(ItemID.SoulofNight, 12);
        recipe.AddIngredient(ItemID.ShadowScale, 8);
        recipe.AddTile(TileID.Anvils);
        recipe.Register();

    }
}
