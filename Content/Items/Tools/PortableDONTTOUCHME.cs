using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Tools;

public class PortableDONTTOUCHME : ModItem, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PortableDONTTOUCHME);
    public override void SetStaticDefaults()
    {
        CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
    }
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(227, 170, 36));
    }

    public override bool? UseItem(Player player)
    {
        Main.GameAskedToQuit = true;
        throw new System.Exception("Whoops! It seems that you pressed the button!");
    }

    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 30;
        Item.useTime = 30;
        Item.scale = 1;
        Item.useAnimation = 30;
        Item.consumable = true;
        Item.maxStack = 1;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = true;
        Item.knockBack = .2f;
        Item.value = 10000;
        Item.rare = ItemRarityID.White;
        Item.autoReuse = true;
        Item.shootSpeed = 1;
    }
}