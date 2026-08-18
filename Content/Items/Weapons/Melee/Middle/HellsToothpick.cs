using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class HellsToothpick : ModItem
{
    public override string Texture => AssetRegistry.GennedTextures.HellsToothpick.Path;

    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void SetDefaults()
    {
        Item.damage = 50;
        Item.knockBack = 0f;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useAnimation = Item.useTime = 36;
        Item.width = Item.height = 12;
        Item.mana = 10;
        Item.UseSound = SoundID.Item1;
        Item.DamageType = DamageClass.Magic;
        Item.autoReuse = false;
        Item.noUseGraphic = true;
        Item.noMelee = true;
        Item.autoReuse = true;

        Item.rare = ItemRarityID.LightRed;
        Item.value = AdditionsGlobalItem.RarityLightRedBuyPrice;
        Item.channel = true;
        Item.shoot = ModContent.ProjectileType<HellsToothpickHeld>();
        Item.shootSpeed = 3.1f;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 98, 7));
    }

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
}
