using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class CharringBarrage : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CharringBarrage);
    public override void SetDefaults()
    {
        Item.damage = 35;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 94;
        Item.height = 36;
        Item.scale = 1f;
        Item.maxStack = 1;
        Item.useTime = Item.useAnimation = 15;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.rare = ItemRarityID.LightPurple;
        Item.UseSound = SoundID.Item11;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<CharringBarrageHoldout>();
        Item.useAmmo = AmmoID.Bullet;
        Item.shootSpeed = 10f;
        Item.autoReuse = true;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(240, 82, 10));
    }
    public override bool CanShoot(Player player) => false;
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.PhoenixBlaster, 1);
        recipe.AddIngredient(ItemID.IllegalGunParts, 1);
        recipe.AddIngredient(ItemID.SoulofFright, 12);
        recipe.AddIngredient(ItemID.HellstoneBar, 4);
        recipe.AddTile(TileID.MythrilAnvil);
        recipe.Register();
    }
}