using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Materials.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class AlucardsSword : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AlucardsSword);

    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 18;
        Item.rare = ItemRarityID.Cyan;
        Item.value = AdditionsGlobalItem.RarityYellowBuyPrice;

        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.UseSound = SoundID.Item10;

        Item.DamageType = DamageClass.MeleeNoSpeed;
        Item.damage = 66;
        Item.knockBack = .1f;
        Item.noMelee = true;

        Item.shoot = ModContent.ProjectileType<AlucardsSwordThrow>();
        Item.channel = true;
        Item.noUseGraphic = true;
        Item.shootSpeed = 15;
        Item.ArmorPenetration = 10;
        Item.crit = 10;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(232, 35, 0));
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<TremorAlloy>(), 10);
        recipe.AddIngredient(ItemID.OrangeBloodroot, 2);
        recipe.AddTile(TileID.BloodMoonMonolith);
        recipe.Register();
    }
}