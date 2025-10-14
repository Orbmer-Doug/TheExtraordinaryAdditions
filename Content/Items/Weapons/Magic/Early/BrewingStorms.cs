using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;

public class BrewingStorms : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BrewingStorms);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(231, 196, 255));
    }

    public override void SetDefaults()
    {
        Item.width = 34;
        Item.height = 38;
        Item.rare = ItemRarityID.Orange;
        Item.useTime = Item.useAnimation = 20;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.autoReuse = true;
        Item.UseSound = null;
        Item.DamageType = DamageClass.Magic;
        Item.damage = 34;
        Item.knockBack = 0f;
        Item.noMelee = true;
        Item.value = AdditionsGlobalItem.RarityOrangeBuyPrice;
        Item.shoot = ModContent.ProjectileType<LightningNimbusSparks>();
        Item.shootSpeed = 18f;
        Item.mana = 7;
        Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player) => false;
}