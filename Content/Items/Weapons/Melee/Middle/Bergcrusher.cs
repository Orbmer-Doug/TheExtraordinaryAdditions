using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class Bergcrusher : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Bergcrusher);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(52, 183, 235));
    }

    public override void SetDefaults()
    {
        Item.width = 50;
        Item.height = 78;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.rare = ItemRarityID.LightRed;
        Item.useTime = 50;
        Item.useAnimation = 50;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 12;
        Item.autoReuse = true;
        Item.damage = 83;
        Item.DamageType = DamageClass.Melee;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<BergcrusherSwing>();
    }
    
    public override bool AltFunctionUse(Player player)
    {
        return player.ownedProjectileCounts[ModContent.ProjectileType<Glacier>()] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        BergcrusherSwing swing = Main.projectile[player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI)].As<BergcrusherSwing>();
        swing.Right = player.Additions().MouseRight.Current;

        return false;
    }

    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
}