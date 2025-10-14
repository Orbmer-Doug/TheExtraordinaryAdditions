using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class AnvilAndPropane : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AnvilAndPropane);

    public override void SetDefaults()
    {
        Item.damage = 272;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 100;
        Item.height = 56;
        Item.useTime =
        Item.useAnimation = 50;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.knockBack = .6f;
        Item.value = Item.buyPrice(0, 90, 50, 0);
        Item.rare = ItemRarityID.Yellow;
        Item.UseSound = AssetRegistry.GetSound(AdditionsSound.ClairDeLune) with
        {
            Volume = 2f,
            MaxInstances = 1,
        };
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.shoot = ModContent.ProjectileType<ThePropane>();
        Item.shootSpeed = 20;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(189, 185, 175));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity * .5f, ModContent.ProjectileType<TheAnvil>(), damage, knockback, player.whoAmI, 0f);
        return true;
    }
}
