using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class Sangue : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Sangue);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }
    public override void SetDefaults()
    {
        Item.damage = 580;
        Item.knockBack = 3f;
        Item.DamageType = DamageClass.Melee;
        Item.useAnimation = Item.useTime = 15;
        Item.autoReuse = true;
        Item.channel = true;
        Item.shoot = ModContent.ProjectileType<SangueSpin>();
        Item.shootSpeed = 12f;

        Item.value = AdditionsGlobalItem.RarityCyanBuyPrice;
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();

        Item.width = 72;
        Item.height = 132;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.UseSound = SoundID.Item1;
        Item.noMelee = true;
        Item.useTurn = true;
        Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        if (player.ownedProjectileCounts[Item.shoot] > 0)
            return false;

        return true;
    }
    public int attackType = 0;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile.NewProjectile(source, position, velocity.SafeNormalize(Vector2.UnitY), type, damage, knockback, player.whoAmI);
        return false;
    }
    public override bool? CanHitNPC(Player player, NPC target) => false;
    public override bool CanHitPvp(Player player, Player target) => false;
}
