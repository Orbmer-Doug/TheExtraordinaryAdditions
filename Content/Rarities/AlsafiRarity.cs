using Daybreak.Common.Features.Rarities;
using Daybreak.Common.Rendering;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using SpriteBatchSnapshot = Daybreak.Common.Rendering.SpriteBatchSnapshot;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class AlsafiRarity : ModRarity, IRarityTextRenderer
{
    public override Color RarityColor => Color.OrangeRed;
    public static ManagedRenderTarget TextTarget;

    public override void Load()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent += RenderToTarget;
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            TextTarget?.Dispose();
            TextTarget = null;
        });
        RenderTargetManager.RenderTargetUpdateLoopEvent -= RenderToTarget;
    }

    private void RenderToTarget()
    {
        if (Main.gameMenu || TextTarget == null)
            return;
        var gd = Main.instance.GraphicsDevice;
        gd.SetRenderTarget(TextTarget);
        gd.Clear(Color.Transparent);
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Matrix.Identity);
        ChatManager.DrawColorCodedString(Main.spriteBatch, FontAssets.MouseText.Value,
            Mod.GetLocalization("Items.GlareOfAlsafi.DisplayName").Value, Vector2.Zero,
            Color.White, 0f, Vector2.Zero, Vector2.One);
        Main.spriteBatch.End();
        gd.SetRenderTarget(null);
    }

    public void RenderText(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, SpriteEffects effects, RarityDrawContext drawContext, float maxWidth = -1,
        float spread = 2)
    {
        if (TextTarget == null)
        {
            if (Main.dedServ)
                return;
            Vector2 size = font.MeasureString(text);
            int width = (int) size.X;
            int height = (int) size.Y;
            Main.QueueMainThreadAction(() =>
            {
                TextTarget = new ManagedRenderTarget(false,
                    (_, _) => new RenderTarget2D(Main.instance.GraphicsDevice, width * 2, height));
            });
            return;
        }

        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position + new Vector2(1f), Color.White * .4f, rotation, origin, scale);

        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position - new Vector2(1f), Color.White * .4f, rotation, origin, scale);
        ManagedShader fire = AssetRegistry.GennedShaders.OverheatIndicator;
        fire.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 3f);
        fire.SetTexture(TextTarget, 0, SamplerState.PointClamp);
        fire.SetTexture(AssetRegistry.GennedTextures.Perlin, 1, SamplerState.LinearWrap);

        Main.spriteBatch.End(out SpriteBatchSnapshot ss);
        Main.spriteBatch.Begin(ss with { CustomEffect = fire.Effect });
        fire.Render();
        Main.spriteBatch.Draw(TextTarget, position, null, Color.White, 0f,  drawContext.DrawKind == RarityDrawContext.Kind.PopupText ? font.MeasureString(text) / 2f : Vector2.Zero, scale,
            SpriteEffects.None, 0f);

        Main.spriteBatch.Restart(ss);
    }
}
