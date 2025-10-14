using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;

public class BobmOnAStick : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BobmOnAStick);

    public override void SetDefaults()
    {
        Item.DamageType = DamageClass.Generic;
        Item.width = 100;
        Item.height = 89;
        Item.useTime = 15;
        Item.useAnimation = 50;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true;
        Item.knockBack = 10;
        Item.value = Item.buyPrice(0, 10, 0, 0);
        Item.rare = ItemRarityID.Yellow;
        Item.UseSound = SoundID.Item1;
        Item.autoReuse = true;
        Item.scale = 1f;
        Item.shoot = ModContent.ProjectileType<BobmHoldout>();
        Item.shootSpeed = 0;
        Item.channel = true;
        Item.noUseGraphic = true;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(163, 158, 0));
    }

    public override bool CanUseItem(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }
}
