using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class SillyPinkHammer : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SillyPinkHammer);

    public const int BaseUseTime = 50;
    public override void SetDefaults()
    {
        Item.width = Item.height = 96;
        Item.damage = 90;
        Item.DamageType = DamageClass.Melee;
        Item.useAnimation = Item.useTime = BaseUseTime;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.knockBack = 5.25f;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.useTurn = true;
        Item.scale = 1f;
        Item.value = Item.buyPrice(0, 45, 0, 0);
        Item.rare = ItemRarityID.LightRed;

        Item.shootSpeed = 10f;
        Item.shoot = ModContent.ProjectileType<SillyPinkSwing>();
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.autoReuse = false;
        Item.channel = true;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Pink);
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity, type, damage, knockback, ai2: player.itemTimeMax);
        return false;
    }
}
