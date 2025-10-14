using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static partial class Utility
{
    public static Rectangle GetFrameRectangle(Point size, int frameX, int startY = 0, int startX = 0)
    {
        int x = startX + (frameX * size.X);
        return new Rectangle(x, startY, size.X, size.Y);
    }

    public static void DrawText(this SpriteBatch spriteBatch, string text, int thickness, Vector2 position,
        Color textColor, Color shadowColor, Vector2 origin = default, float scale = 1f, float rotation = 0f)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        Vector2 originFixed = ChatManager.GetStringSize(font, text, Vector2.One) * origin;
        for (int i = -thickness; i <= thickness; i++)
        {
            for (int k = -thickness; k <= thickness; k++)
            {
                if (i == 0 && k == 0)
                    continue;

                float alpha = MathHelper.Lerp(1f, 0f, Math.Abs((i + k) / 2f));
                spriteBatch.DrawString(font, text, position + new Vector2(i, k), Color.Multiply(shadowColor, alpha), rotation, originFixed, scale, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.DrawString(font, text, position, textColor, rotation, originFixed, scale, SpriteEffects.None, 0f);
    }

    public static void DrawBorderStringEightWay(SpriteBatch sb, DynamicSpriteFont font, string text, Vector2 baseDrawPosition, Color main, Color border, float rotation, float scale = 1f)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 drawPosition = baseDrawPosition + new Vector2(x, y);
                if (x != 0 || y != 0)
                {
                    DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, drawPosition, border, rotation, default, scale, 0, 0f);
                }
            }
        }
        DynamicSpriteFontExtensionMethods.DrawString(sb, font, text, baseDrawPosition, main, rotation, default, scale, 0, 0f);
    }

    public static void DrawBetterRect(this SpriteBatch sb, Texture2D tex, Rectangle rect, Rectangle? source, Color color, float rot, Vector2 orig, SpriteEffects fx = SpriteEffects.None, bool subtract = false) =>
        sb.Draw(tex, subtract ? new(rect.X - (int)Main.screenPosition.X, rect.Y - (int)Main.screenPosition.Y, rect.Width, rect.Height) : rect, source, color, rot, orig, fx, 0f);

    public static void DrawBetter(this SpriteBatch sb, Texture2D tex, Vector2 pos, Rectangle? source, Color color, float rot, Vector2 orig, float scale, SpriteEffects fx = SpriteEffects.None) =>
        sb.Draw(tex, pos - Main.screenPosition, source, color, rot, orig, scale, fx, 0f);

    public static void DrawBetter(this SpriteBatch sb, Texture2D tex, Vector2 pos, Rectangle? source, Color color, float rot, Vector2 orig, Vector2 scale, SpriteEffects fx = SpriteEffects.None) =>
        sb.Draw(tex, pos - Main.screenPosition, source, color, rot, orig, scale, fx, 0f);

    public static void PixelDraw(this SpriteBatch sb, Texture2D tex, Vector2 pos, Rectangle? source, Color color, float rot, Vector2 orig, Vector2 scale, SpriteEffects fx = SpriteEffects.None) =>
        sb.Draw(tex, (pos - Main.screenPosition) / 2f, source, color, rot, orig, scale / 2f, fx, 0f);

    public static RenderTarget2D CreateScreenSizedTarget(int screenWidth, int screenHeight) =>
        new(Main.graphics.GraphicsDevice, screenWidth, screenHeight, true, SurfaceFormat.Color, DepthFormat.Depth24, 8, RenderTargetUsage.PreserveContents);

    public static void EnterShaderRegionAlt(this SpriteBatch sb)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, DefaultRasterizerScreenCull, null, Main.GameViewMatrix.TransformationMatrix);
    }

    public static void EnterShaderRegion(this SpriteBatch sb, BlendState newBlendState = null, Effect effect = null)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);
    }

    public static void ExitShaderRegion(this SpriteBatch sb)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }

    public static void SetBlendState(this SpriteBatch sb, BlendState blendState)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }

    public static void ResetBlendState(this SpriteBatch sb)
    {
        sb.SetBlendState(BlendState.AlphaBlend);
    }

    /// <summary>
    /// Resets a sprite batch with a desired <see cref="BlendState"/>. The <see cref="SpriteSortMode"/> is specified as <see cref="SpriteSortMode.Deferred"/>. If <see cref="SpriteSortMode.Immediate"/> is needed, use <see cref="PrepareForShaders"/> instead.
    /// <br></br>
    /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker systems.
    /// </summary>
    /// <param name="sb">The sprite batch.</param>
    /// <param name="newBlendState">The desired blend state.</param>
    public static void UseBlendState(this SpriteBatch sb, BlendState newBlendState)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, newBlendState, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    /// Resets the sprite batch with <see cref="SpriteSortMode.Immediate"/> blending, along with an optional <see cref="BlendState"/>. For use when shaders are necessary.
    /// <br></br>
    /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker devices.
    /// </summary>
    /// <param name="sb">The sprite batch.</param>
    /// <param name="newBlendState">An optional blend state. If none is supplied, <see cref="BlendState.AlphaBlend"/> is used.</param>
    public static void PrepareForShaders(this SpriteBatch sb, BlendState newBlendState = null, bool ui = false)
    {
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, ui ? Main.UIScaleMatrix : Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    /// Resets the sprite batch to its 'default' state relative to most effects in the game, with a default blend state and sort mode. For use after the sprite batch state has been altered and needs to be reset.
    /// <br></br>
    /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker systems.
    /// </summary>
    /// <param name="sb">The sprite batch.</param>
    /// <param name="end">Whether to call <see cref="SpriteBatch.End"/> first and flush the contents of the previous draw batch. Defaults to true.</param>
    public static void ResetToDefault(this SpriteBatch sb, bool end = true)
    {
        if (end)
            sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
    }

    /// <summary>
    /// Resets the sprite batch to its 'default' state relative to the UI, with a default blend state and sort mode. For use after the sprite batch state has been altered and needs to be reset.
    /// <br></br>
    /// Like any sprite batch resetting function, use this sparingly. Overusage (such as performing this operation multiple times per frame) will lead to significantly degraded performance on weaker systems.
    /// </summary>
    /// <param name="sb">The sprite batch.</param>
    /// <param name="end">Whether to call <see cref="SpriteBatch.End"/> first and flush the contents of the previous draw batch. Defaults to true.</param>
    public static void ResetToDefaultUI(this SpriteBatch sb, bool end = true)
    {
        if (end)
            sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
    }

    private static RasterizerState cullClockwiseAndScreen;

    private static RasterizerState cullCounterclockwiseAndScreen;

    private static RasterizerState cullOnlyScreen;

    public static RasterizerState CullClockwiseAndScreen
    {
        get
        {
            if (cullClockwiseAndScreen is null)
            {
                cullClockwiseAndScreen = RasterizerState.CullClockwise;
                cullClockwiseAndScreen.ScissorTestEnable = true;
            }

            return cullClockwiseAndScreen;
        }
    }

    public static RasterizerState CullCounterclockwiseAndScreen
    {
        get
        {
            if (cullCounterclockwiseAndScreen is null)
            {
                cullCounterclockwiseAndScreen = RasterizerState.CullCounterClockwise;
                cullCounterclockwiseAndScreen.ScissorTestEnable = true;
            }

            return cullCounterclockwiseAndScreen;
        }
    }

    public static RasterizerState CullOnlyScreen
    {
        get
        {
            if (cullOnlyScreen is null)
            {
                cullOnlyScreen = RasterizerState.CullNone;
                cullOnlyScreen.ScissorTestEnable = true;
            }

            return cullOnlyScreen;
        }
    }
    public static RasterizerState DefaultRasterizerScreenCull => Main.gameMenu || Main.LocalPlayer.gravDir == 1f ? CullCounterclockwiseAndScreen : CullClockwiseAndScreen;

    public static void SwapToRenderTarget(this RenderTarget2D renderTarget, Color? flushColor = null)
    {
        GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
        SpriteBatch spriteBatch = Main.spriteBatch;

        if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
            return;

        // Set the render target
        graphicsDevice.SetRenderTarget(renderTarget);

        // Flush the screen, removing any previous things drawn to it
        flushColor ??= Color.Transparent;
        graphicsDevice.Clear(flushColor.Value);
    }

    public static bool DrawTreasureBagInWorld(Item item, SpriteBatch spriteBatch, float rotation, float scale, int whoAmI)
    {
        Texture2D texture = TextureAssets.Item[item.type].Value;
        Rectangle frame = Utils.Frame(texture, 1, 1, 0, 0, 0, 0);
        if (Main.itemAnimations[item.type] != null)
            frame = Main.itemAnimations[item.type].GetFrame(texture, Main.itemFrameCounter[whoAmI]);
        
        Vector2 frameOrigin = Utils.Size(frame) * 0.5f;
        Vector2 offset = default;
        offset.ToWorldCoordinates(item.width / 2 - frameOrigin.X, item.height - frame.Height);
        Vector2 drawPos = item.position - Main.screenPosition + frameOrigin + offset;
        float localTime = item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;
        float time = Main.GlobalTimeWrappedHourly % 4f / 2f;
        if (time >= 1f)
            time = 2f - time;
        time = time * 0.5f + 0.5f;
        for (int j = 0; j < 4; j++)
        {
            Vector2 pulseOffset = Utils.RotatedBy(Vector2.UnitY, (double)((j / 4f + localTime) * (MathHelper.Pi * 2f)), default) * time * 8f;
            spriteBatch.Draw(texture, drawPos + pulseOffset, (Rectangle?)frame, new Color(90, 70, 255, 50), rotation, frameOrigin, scale, 0, 0f);
        }
        for (int i = 0; i < 3; i++)
        {
            Vector2 pulseOffset2 = Utils.RotatedBy(Vector2.UnitY, (double)((i / 3f + localTime) * (MathHelper.Pi * 2f)), default) * time * 4f;
            spriteBatch.Draw(texture, drawPos + pulseOffset2, (Rectangle?)frame, new Color(140, 120, 255, 77), rotation, frameOrigin, scale, 0, 0f);
        }
        return true;
    }

    /// <summary>
    /// Draws a newly scaled item in the inventory
    /// </summary>
    /// <param name="spriteBatch"></param>
    /// <param name="texture">The texture</param>
    /// <param name="position">Position in the inventory</param>
    /// <param name="frame">Frames of the item</param>
    /// <param name="drawColor"></param>
    /// <param name="itemColor"></param>
    /// <param name="origin">Origin of the item</param>
    /// <param name="scale">The scale of the item</param>
    /// <param name="wantedScale">The actual scale wanted, putting 1 makes the full sprite</param>
    /// <param name="drawOffset">Offset of it</param>
    public static void DrawInventoryCustomScale(SpriteBatch spriteBatch, Texture2D texture, Vector2 position,
        Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale, float wantedScale = 1f, Vector2 drawOffset = default)
    {
        wantedScale = Math.Max(scale, wantedScale * Main.inventoryScale);
        position += drawOffset * wantedScale;
        spriteBatch.Draw(texture, position, (Rectangle?)frame, drawColor, 0f, origin, wantedScale, 0, 0f);
    }

    public static Rectangle GetCurrentFrame(this Item item, ref int frame, ref int frameCounter, int frameDelay, int frameAmt, bool frameCounterUp = true)
    {
        if (frameCounter >= frameDelay)
        {
            frameCounter = -1;
            frame = (frame != frameAmt - 1) ? (frame + 1) : 0;
        }
        if (frameCounterUp)
            frameCounter++;

        return new Rectangle(0, item.height * frame, item.width, item.height);
    }

    /// <summary>
    /// Draw the bare bones of a projectile
    /// </summary>
    public static void DrawBaseProjectile(this Projectile projectile, Color color, SpriteEffects fx = SpriteEffects.None, Texture2D overrideTex = default)
    {
        Texture2D texture = overrideTex ?? projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
        Vector2 drawPosition = projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, frame, projectile.GetAlpha(color), projectile.rotation, frame.Size() / 2f, projectile.scale, fx, 0);
    }

    public static void DrawProjectileBackglow(this Projectile projectile, Color backglowColor, float backglowArea, byte alpha = 0,
        int amount = 10, SpriteEffects spriteEffects = 0, Rectangle? frame = null, Texture2D overrideTexture = null, Vector2? orig = null)
    {
        Texture2D texture = overrideTexture ?? TextureAssets.Projectile[projectile.type].Value;

        frame ??= texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame, 0, 0);

        Vector2 drawPosition = projectile.Center - Main.screenPosition;
        Vector2 origin = orig ?? frame.Value.Size() * 0.5f;
        Color color = projectile.GetAlpha(backglowColor * projectile.Opacity) with { A = alpha };
        for (int i = 0; i < amount; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / amount).ToRotationVector2() * backglowArea;
            Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, color * 0.95f, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
        }
    }

    public static void DrawNPCBackglow(this NPC npc, Color backglowColor, float backglowArea, SpriteEffects spriteEffects, Rectangle frame, byte alpha = 0, int amount = 10, Vector2 screenPos = default, Texture2D overrideTexture = null)
    {
        Texture2D texture = overrideTexture ?? TextureAssets.Npc[npc.type].Value;
        if (screenPos == default)
            screenPos = Main.screenPosition;
        Vector2 drawPosition = npc.Center - screenPos;
        Vector2 origin = frame.Size() * 0.5f;
        Color color = npc.GetAlpha(backglowColor * npc.Opacity) with { A = alpha };
        for (int i = 0; i < amount; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / amount).ToRotationVector2() * backglowArea;
            Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, color * 0.95f, npc.rotation, origin, npc.scale, spriteEffects, 0f);
        }
    }

    public static Rectangle ToTarget(Vector2 pos, int width, int height) =>
         new((int)(pos.X - Main.screenPosition.X), (int)(pos.Y - Main.screenPosition.Y), width, height);
    public static Rectangle ToTarget(Vector2 pos, float width, float height) =>
     new((int)(pos.X - Main.screenPosition.X), (int)(pos.Y - Main.screenPosition.Y), (int)width, (int)height);
    public static Rectangle ToTarget(Vector2 pos, Vector2 size) =>
     new((int)(pos.X - Main.screenPosition.X), (int)(pos.Y - Main.screenPosition.Y), (int)size.X, (int)size.Y);
    public static Rectangle ToScreenTarget(Vector2 pos, Vector2 size) =>
     new((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
}