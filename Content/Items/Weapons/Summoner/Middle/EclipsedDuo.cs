using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class EclipsedDuo : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EclipsedDuo);

    public override void SetDefaults()
    {
        Item.damage = 168;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 30;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ItemRarityID.LightPurple;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<EclipseWhip>();
        Item.shootSpeed = 1f;
        Item.knockBack = 3f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public int Switch;
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        EclipseWhip whip = Main.projectile[player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI)].As<EclipseWhip>();
        whip.Moon = Switch == 1;
        Switch = (Switch + 1) % 2;
        NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, player.whoAmI);

        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 213, 176));
    }

    public override bool MeleePrefix()
    {
        return true;
    }
}