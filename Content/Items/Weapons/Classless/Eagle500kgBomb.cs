using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Classless;

public class Eagle500kgBomb : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Eagle500kgBomb);
    public override void SetDefaults()
    {
        Item.width = Item.height = 32;
        Item.DamageType = DamageClass.Generic;
        Item.damage = 4000;
        Item.crit = -4;
        Item.rare = ItemRarityID.Red;
        Item.value = AdditionsGlobalItem.RarityRedBuyPrice;
        Item.autoReuse = true;

        Item.UseSound = null;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.shoot = ModContent.ProjectileType<StratagemMark>();
        Item.shootSpeed = 1f;
        Item.noUseGraphic = true;
        Item.noMelee = true;
    }
    public override bool CanShoot(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
}
