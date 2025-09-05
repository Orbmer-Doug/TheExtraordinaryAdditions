using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.UI;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.UI.LaserUI;

public class LaserResourceUI : SmartUIState
{
    public override int InsertionIndex(List<GameInterfaceLayer> layers) => layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
    public override InterfaceScaleType Scale => InterfaceScaleType.None;

    public static readonly Texture2D outline = AssetRegistry.GetTexture(AdditionsTexture.LaserResource);

    public override void OnInitialize()
    {
        Dest = new Vector2(Main.screenWidth / 2, Main.screenHeight / 2) + new Vector2(0f, outline.Size().Y);
        Left.Set(Dest.X - outline.Size().X / 2, 0f);
        Top.Set(Dest.Y - outline.Size().Y / 2, 0f);

        Width = new(outline.Size().X, 0f);
        Height = new(outline.Size().Y, 0f);
    }

    public override bool Visible => Main.LocalPlayer.GetModPlayer<LaserResource>().HoldingLaserWeapon;
    private bool BeingDragged;
    private Vector2 Dest;
    public override void Draw(SpriteBatch spriteBatch)
    {
        LaserResource modPlayer = Main.LocalPlayer.GetModPlayer<LaserResource>();
        Vector2 pos = GetInnerDimensions().ToRectangle().Center();

        Vector2 backgroundScale = Vector2.One * Main.UIScale;
        float rotation = 0f;
        Color color = Color.White * modPlayer.HeatBarAlpha;

        // Draw the border
        spriteBatch.Draw(outline, pos, null, color, rotation, outline.Size() * .5f, backgroundScale, 0, 0f);

        // Draw the bar
        float quotient = Utils.Clamp((float)modPlayer.HeatCurrent / modPlayer.HeatMax2, 0f, 1f);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Perlin);

        ManagedShader shader = AssetRegistry.GetShader("OverheatIndicator");
        shader.TrySetParameter("Completion", 1f - quotient);
        shader.TrySetParameter("Time", Main.GlobalTimeWrappedHourly);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, shader.Effect, Matrix.Identity);

        shader.Render();
        spriteBatch.DrawBetterRect(tex, ToScreenTarget(pos + new Vector2(-outline.Width / 2 + 4f, 12f), new Vector2(100, 24) * backgroundScale), null, Color.White * modPlayer.HeatBarAlpha, 0f, Vector2.Zero);

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);
    }

    public override void SafeUpdate(GameTime gameTime)
    {
        LaserResource modPlayer = Main.LocalPlayer.GetModPlayer<LaserResource>();
        if (BeingDragged)
        {
            Dest = Vector2.Lerp(Dest, Main.MouseScreen, .7f);
            Left.Set(Dest.X - outline.Size().X / 2, 0f);
            Top.Set(Dest.Y - outline.Size().Y / 2, 0f);
            Recalculate();
        }

        if (IsMouseHovering && modPlayer.HeatMax > 0f)
        {
            string HeatStr = modPlayer.HeatCurrent.ToString("n2");
            string maxHeatStr = modPlayer.HeatMax.ToString("n2");
            string textToDisplay = $"{GetTextValue("UI.Heat")}: {HeatStr}/{maxHeatStr}\n";
            textToDisplay = Main.keyState.IsKeyDown(Keys.LeftShift) ? textToDisplay + GetTextValue("UI.HeatInfoText") : textToDisplay + GetTextValue("UI.HeatShiftText");
            Main.instance.MouseText(textToDisplay, 0, 0, -1, -1, -1, -1, 0);
            modPlayer.HeatBarAlpha = MathHelper.Lerp(modPlayer.HeatBarAlpha, 0.25f, 0.035f);
        }

        if (IsMouseHovering && Main.LocalPlayer.GetModPlayer<GlobalPlayer>().MouseMiddle.Current)
            BeingDragged = true;
        else
            BeingDragged = false;
    }
}