using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Middle;

public class PlasmaCore : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.PlasmaCore);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(255, 161, 94));
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(7, 4, false));

        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public override void PostUpdate()
    {
        float bright = MathF.Sin(Main.GlobalTimeWrappedHourly * 4f) * .2f + .8f;
        Lighting.AddLight(Item.Center, (new Color(255, 153, 0)).ToVector3() * bright);
    }

    public override void SetDefaults()
    {
        Item.width = Item.height = 20;
        Item.rare = ItemRarityID.Red;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = AdditionsGlobalItem.RarityLightPurpleBuyPrice;
    }

    public override void AddRecipes()
    {
        Recipe recipe = CreateRecipe();
        recipe.AddIngredient(ModContent.ItemType<WrithingLight>(), 1);
        recipe.AddIngredient(ModContent.ItemType<EmblazenedEmber>(), 3);
        recipe.AddIngredient(ModContent.ItemType<MythicScrap>(), 5);
        recipe.AddTile(TileID.AdamantiteForge);
        recipe.Register();
    }
}
