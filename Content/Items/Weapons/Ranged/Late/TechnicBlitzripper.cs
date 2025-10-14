using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Rarities.AdditionRarities;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;

public class TechnicBlitzripper : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TechnicBlitzripper);

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 222;
        Item.DamageType = DamageClass.Ranged;
        Item.width = 138;
        Item.height = 176;
        Item.useAnimation = Item.useTime = 40;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = 4f;
        Item.UseSound = null;
        Item.autoReuse = true;
        Item.noUseGraphic = true;
        Item.shoot = ProjectileID.PurificationPowder;
        Item.shootSpeed = 12f;
        Item.useAmmo = AmmoID.Bullet;
        Item.useTurn = true;
        Item.rare = ModContent.RarityType<CyberneticRarity>();
        Item.value = AdditionsGlobalItem.LegendaryRarityPrice;
    }
    
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(Color.Cyan);
    }

    public override bool CanShoot(Player player) => false;
}