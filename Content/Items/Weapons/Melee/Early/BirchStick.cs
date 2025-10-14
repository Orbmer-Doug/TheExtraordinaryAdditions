using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;

public class BirchStick : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BirchStick);

    public override void SetDefaults()
    {
        Item.width = 106;
        Item.height = 98;
        Item.DamageType = DamageClass.Melee;
        Item.damage = 20;
        Item.crit = 8;
        Item.channel = true;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.UseSound = null;
        Item.useTime = Item.useAnimation = 2;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.rare = ItemRarityID.Blue;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.shootSpeed = 1f;
        Item.knockBack = 1f;
        Item.shoot = ModContent.ProjectileType<BirchStickLance>();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(200, 200, 200));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        BirchStickLance lance = Main.projectile[player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI)].As<BirchStickLance>();

        if (player.Additions().MouseRight.Current)
            lance.State = BirchStickLance.BirchStickState.Poke;
        else
            lance.State = BirchStickLance.BirchStickState.BashDown;

        return false;
    }

    public override bool AltFunctionUse(Player player) => true;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.Wood, 120);
        recipe.AddIngredient(ItemID.LivingWoodWand, 1);

        recipe.AddTile(TileID.WorkBenches);
        recipe.Register();
    }
}