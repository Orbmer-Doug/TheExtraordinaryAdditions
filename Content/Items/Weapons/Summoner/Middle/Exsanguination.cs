using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;

public class Exsanguination : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Exsanguination);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 64, 52));
    }

    public override void SetDefaults()
    {
        Item.DefaultToWhip(ModContent.ProjectileType<ExsanguinationProj>(), 20, 6f, 4f, 35);
        Item.rare = ModContent.RarityType<BloodWroughtRarity>();
        Item.damage = 200;
        Item.channel = true;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        return !Main.projectile.Any(n => n.active && n.type == type && n.owner == player.whoAmI);
    }
    public override bool MeleePrefix()
    {
        return true;
    }
}
