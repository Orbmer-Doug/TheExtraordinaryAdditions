using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Globals.DrawLayers;

public class SupremeBubbleLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.BackAcc);
    }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        if (drawInfo.drawPlayer.Additions().Buffs[GlobalPlayer.AdditionsBuff.BigOxygen])
            return drawInfo.shadow == 0f;
        return false;
    }

    public override void Draw(ref PlayerDrawSet drawInfo)
    {
        Texture2D texture = AssetRegistry.GetTexture(AdditionsTexture.Bubble);
        int drawX = (int)(drawInfo.Center.X - Main.screenPosition.X);
        int drawY = (int)(drawInfo.Center.Y - Main.screenPosition.Y);
        Vector2 bubbleScale = Vector2.One * (1f * 0.8f + MathF.Cos(Main.GlobalTimeWrappedHourly * 1.1f + drawInfo.drawPlayer.whoAmI) * 0.04f);
        Vector2 scalingDirection = -Vector2.UnitY.RotatedBy(drawInfo.drawPlayer.whoAmI % 4 / 4f * MathHelper.TwoPi);
        bubbleScale += scalingDirection * (float)Cos01(Main.GlobalTimeWrappedHourly * 3.1f + drawInfo.drawPlayer.whoAmI) * 0.16f;
        drawInfo.DrawDataCache.Add(new DrawData(texture, new Vector2(drawX, drawY), null, Color.White with { A = 0 }, 0f, texture.Size() * 0.5f, bubbleScale / 3, 0, 0f));
    }
}
