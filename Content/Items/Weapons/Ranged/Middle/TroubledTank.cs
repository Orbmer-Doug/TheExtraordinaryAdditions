using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class TroubledTank : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TroubledTank);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Green);
    }
    public override void SetDefaults()
    {
        Item.damage = 700;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 34;
        Item.height = 52;
        Item.useTime =
        Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 2f;
        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ItemRarityID.Cyan;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<TankHeadHoldout>();
        Item.shootSpeed = 16f;
        Item.noUseGraphic = true;
    }
    public override bool CanShoot(Player player)
    {
        return false;
    }

    public override void HoldItem(Player player)
    {
        player.Additions().SyncMouse = true;
    }
    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ItemID.FragmentVortex, 16);
        recipe.AddTile(TileID.LunarCraftingStation);
        recipe.Register();
    }
}
