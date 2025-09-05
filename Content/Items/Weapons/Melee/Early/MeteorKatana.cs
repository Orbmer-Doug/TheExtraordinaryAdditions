using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Early;

public class MeteorKatana : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MeteorKatana);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(163, 133, 114));
    }

    public override void SetDefaults()
    {
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useTime = Item.useAnimation = 70;
        Item.damage = 32;
        Item.knockBack = 2.8f;
        Item.width = 69;
        Item.height = 78;
        Item.useTurn = true;
        Item.UseSound = null;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.rare = ItemRarityID.Orange;
        Item.DamageType = DamageClass.Melee;
        Item.shoot = ModContent.ProjectileType<MeteorSwing>();
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.crit = 15;
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        {
            recipe.AddIngredient(ItemID.Katana, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 16);
        }
        recipe.AddTile(TileID.Anvils);
        recipe.Register();
    }
}