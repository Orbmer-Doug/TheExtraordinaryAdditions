using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;

public class Fireball : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Fireball);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 56;
        Item.DamageType = DamageClass.Magic;
        Item.width = 32;
        Item.height = 40;
        Item.useTime = Item.useAnimation = 30;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.noUseGraphic = true;
        Item.channel = true;
        Item.knockBack = .8f;
        Item.value = AdditionsGlobalItem.RarityPinkBuyPrice;
        Item.rare = ItemRarityID.Pink;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.shoot = ModContent.ProjectileType<FireballHoldout>();
        Item.shootSpeed = 20f;
        Item.crit = 40; //it is very effective
        Item.mana = 15;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(227, 170, 36));
    }

    public override bool CanShoot(Player player) => false;
}