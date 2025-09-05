using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;

public class HellsToothpick : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HellsToothpick);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(235, 98, 7));
    }


    public override void SetDefaults()
    {
        Item.damage = 60;
        Item.knockBack = 0f;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.useAnimation = 12;
        Item.useTime = 12;
        Item.width = 24;
        Item.height = 100;
        Item.UseSound = SoundID.Item1;
        Item.DamageType = DamageClass.MeleeNoSpeed;
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

    public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
}