using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Late;

public class TidalDeluge : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TidalDeluge);
    public override void SetDefaults()
    {
        Item.damage = 430;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 42;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ModContent.RarityType<BrackishRarity>();
        Item.value = AdditionsGlobalItem.UniqueRarityPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<DelugeWhip>();
        Item.shootSpeed = 1f;
        Item.knockBack = 1f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        Projectile p = Main.projectile[player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI)];
        return false;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(AbyssalCurrents.WaterPalette[2]);
    }

    public override bool MeleePrefix() => true;
}