using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;

public class RendedStar : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RendedStar);
    public override void SetDefaults()
    {
        Item.damage = 1145;
        Item.channel = true;
        Item.DamageType = DamageClass.Melee;
        Item.width = 156;
        Item.height = 124;
        Item.useTime = 8;
        Item.useAnimation = 8;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 10f;
        Item.crit = -4;
        Item.shoot = ModContent.ProjectileType<RendedStarHoldout>();
        Item.shootSpeed = 15f;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.autoReuse = true;
        Item.value = AdditionsGlobalItem.LaserRarityPrice;
        Item.rare = ModContent.RarityType<LaserClassRarity>();
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.FirstOrDefault(n => n.Name == "Damage").Text = tooltips.FirstOrDefault(n => n.Name == "Damage").Text.Replace("damage", "damage swung");
        tooltips.ColorLocalization(new(255, 72, 31));
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        return false;
    }

    public override void HoldItem(Player player)
    {
        if (player.ownedProjectileCounts[Item.shoot] == 0)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Center, Vector2.Zero, Item.shoot, Item.damage, Item.knockBack, player.whoAmI);

        base.HoldItem(player);
    }
}