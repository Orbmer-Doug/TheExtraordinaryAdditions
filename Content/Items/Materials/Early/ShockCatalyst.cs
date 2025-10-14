using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals.ItemGlobal;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Items.Materials.Early;

public class ShockCatalyst : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ShockCatalyst);

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.ColorLocalization(new Color(231, 191, 255));
    }

    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 25;
        Main.RegisterItemAnimation(Item.type, new DrawAnimationVertical(7, 7, false));
        ItemID.Sets.ItemNoGravity[Item.type] = true;
        ItemID.Sets.AnimatesAsSoul[Type] = true;
    }

    public int frameCounter;
    public int frame;
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.ShockCatalyst);
        Texture2D texGlow = AssetRegistry.GetTexture(AdditionsTexture.ShockCatalyst_Glow);

        Rectangle framed = Item.GetCurrentFrame(ref frame, ref frameCounter, 9, 7);
        spriteBatch.Draw(tex, Item.position - Main.screenPosition, framed, lightColor, 0f, Vector2.Zero, 1f, 0, 0f);
        spriteBatch.Draw(texGlow, Item.position - Main.screenPosition, framed, Color.White, 0f, Vector2.Zero, 1f, 0, 0f);
        return false;
    }

    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 30;
        Item.rare = ItemRarityID.Orange;

        Item.maxStack = Item.CommonMaxStack;
        Item.value = AdditionsGlobalItem.RarityWhiteBuyPrice;
    }
}