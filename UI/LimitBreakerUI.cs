using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.UI;

public class LimitBreakerUI : SmartUIState
{
    public override int InsertionIndex(List<GameInterfaceLayer> layers) =>
        layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");

    public override InterfaceScaleType Scale => InterfaceScaleType.None;

    internal static bool CurrentlyViewing;
    public override bool Visible => CurrentlyViewing;

    public override void Draw(SpriteBatch spriteBatch)
    {
        Player player = Main.LocalPlayer;
        if (player == null || !player.active || player.HeldItem.type != ItemID.BreakerBlade)
            return;

        float interpolant = player.Additions().CurrentLimit;

        Texture2D bar = AssetRegistry.GennedTextures.LimitBreakerBar;
        Texture2D border = AssetRegistry.GennedTextures.LimitBreakerBorder;

        Point pos = (player.Center + new Vector2(-border.Width / 2, -border.Height * 2) +
            Vector2.UnitY * player.gfxOffY - Main.screenPosition).ToPoint();
        Rectangle barTarget = new(pos.X + 22, pos.Y + 22, (int) (interpolant * 42), 10);

        int frameY = 0;
        if (player.Additions().AtMaxLimit)
            frameY = player.AdditionsMisc().GlobalTimer % 8 == 7 ? 10 : 20;

        Rectangle barFrame = new(0, frameY, (int) (interpolant * 42), 10);

        Rectangle borderTarget = new(pos.X, pos.Y, border.Width, border.Height);

        Main.spriteBatch.Draw(border, borderTarget, Color.White);
        Main.spriteBatch.Draw(bar, barTarget, barFrame, Color.White);
    }
}
