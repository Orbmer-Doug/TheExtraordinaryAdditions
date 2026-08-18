using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static class SpriteBatchUtils
{
    /// <summary>
    /// Regular additive blending has the GPU calculating <c>result = shaderOutput.rgb * shaderOutput.a + destination</c>, which is not ideal in shaders returning 0 alpha
    /// </summary>
    public static readonly BlendState AdditiveBlendNoAlpha = new BlendState()
    {
        ColorBlendFunction = BlendFunction.Add,
        ColorSourceBlend = Blend.One,
        ColorDestinationBlend = Blend.One,
        AlphaBlendFunction = BlendFunction.Add,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.One
    };

    public static Rectangle GetFrameRectangle(Point size, int frameX, int startY = 0, int startX = 0)
    {
        int x = startX + frameX * size.X;
        return new Rectangle(x, startY, size.X, size.Y);
    }

    public static void DrawBorderStringEightWay(SpriteBatch sb, DynamicSpriteFont font, string text,
        Vector2 baseDrawPosition, Color main, Color border, float rotation, float scale = 1f)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2 drawPosition = baseDrawPosition + new Vector2(x, y);
                if (x != 0 || y != 0)
                {
                    sb.DrawString(font, text, drawPosition, border, rotation, default, scale, 0, 0f);
                }
            }
        }

        sb.DrawString(font, text, baseDrawPosition, main, rotation, default, scale, 0, 0f);
    }

    public static RenderTarget2D CreateScreenSizedTarget(int screenWidth, int screenHeight) =>
        new(Main.graphics.GraphicsDevice, screenWidth, screenHeight, true, SurfaceFormat.Color, DepthFormat.Depth24, 8,
            RenderTargetUsage.PreserveContents);

    extension(SpriteBatch spriteBatch)
    {
        public void SetBlendState(BlendState blendState)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, blendState, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public void EnterShaderRegion(Effect effect = null, BlendState newBlendState = null, Matrix? matrix = null)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, newBlendState ?? BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, effect, matrix ?? Main.GameViewMatrix.TransformationMatrix);
        }

        public void ResetToDefault()
        {
            spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        public void ResetToDefaultUI(bool end = true)
        {
            if (end)
                spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer, null, Main.UIScaleMatrix);
        }

        public void DrawText(string text, int thickness, Vector2 position,
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
                    spriteBatch.DrawString(font, text, position + new Vector2(i, k), Color.Multiply(shadowColor, alpha),
                        rotation, originFixed, scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.DrawString(font, text, position, textColor, rotation, originFixed, scale, SpriteEffects.None,
                0f);
        }

        public void DrawBetterRect(Texture2D tex, Rectangle rect, Rectangle? source,
            Color color, float rot, Vector2 orig, SpriteEffects fx = SpriteEffects.None, bool subtract = false) =>
            spriteBatch.Draw(tex,
                subtract
                    ? new(rect.X - (int) Main.screenPosition.X, rect.Y - (int) Main.screenPosition.Y, rect.Width,
                        rect.Height)
                    : rect, source, color, rot, orig, fx, 0f);

        public void DrawBetter(Texture2D tex, Vector2 pos, Rectangle? source, Color color,
            float rot, Vector2 orig, float scale, SpriteEffects fx = SpriteEffects.None) =>
            spriteBatch.Draw(tex, pos - Main.screenPosition, source, color, rot, orig, scale, fx, 0f);

        public void DrawBetter(Texture2D tex, Vector2 pos, Rectangle? source, Color color,
            float rot, Vector2 orig, Vector2 scale, SpriteEffects fx = SpriteEffects.None) =>
            spriteBatch.Draw(tex, pos - Main.screenPosition, source, color, rot, orig, scale, fx, 0f);

        public void PixelDraw(Texture2D tex, Vector2 pos, Rectangle? source, Color color,
            float rot, Vector2 orig, Vector2 scale, SpriteEffects fx = SpriteEffects.None) =>
            spriteBatch.Draw(tex, (pos - Main.screenPosition) / 2f, source, color, rot, orig, scale / 2f, fx, 0f);

        public static void DrawAltPixelated(PixelationLayer layer, BlendState blend, Texture2D tex, Vector2 pos,
            Rectangle? source, Color color, float rot, Vector2 orig, float scale, SpriteEffects fx = SpriteEffects.None,
            ManagedShader shader = null) =>
            PixelationSystem.QueueTextureRenderAction(tex, pos - Main.screenPosition, source, color, rot, orig, scale,
                fx, layer, false, blend, shader);

        public static void DrawAltPixelated(PixelationLayer layer, BlendState blend, Texture2D tex, Vector2 pos,
            Rectangle? source, Color color, float rot, Vector2 orig, Vector2 scale,
            SpriteEffects fx = SpriteEffects.None, ManagedShader shader = null) =>
            PixelationSystem.QueueTextureRenderAction(tex, pos - Main.screenPosition, source, color, rot, orig, scale,
                fx, layer, false, blend, shader);

        public static void DrawRectPixelated(PixelationLayer layer, BlendState blend, Texture2D tex, Rectangle rect,
            Color color, ManagedShader shader = null)
        {
            PixelationSystem.QueueTextureRenderAction(tex, rect.TopLeft(), null, color, 0f, Vector2.Zero, rect.Size(),
                SpriteEffects.None,
                layer, true, blend, shader);
        }

        public static void DrawRectPixelated(PixelationLayer layer, BlendState blend, Texture2D tex, Rectangle rect,
            Rectangle? source, Color color, float rot, Vector2 orig, SpriteEffects fx = SpriteEffects.None,
            bool subtract = false, ManagedShader shader = null)
        {
            Rectangle pos = subtract
                ? new Rectangle(
                    rect.X - (int) Main.screenPosition.X,
                    rect.Y - (int) Main.screenPosition.Y, rect.Width,
                    rect.Height)
                : rect;

            PixelationSystem.QueueTextureRenderAction(tex, pos.TopLeft(), source, color, rot, orig, pos.Size(), fx,
                layer, true, blend, shader);
        }
    }

    public static bool DrawTreasureBagInWorld(Item item, SpriteBatch spriteBatch, float rotation, float scale,
        int whoAmI)
    {
        Texture2D texture = TextureAssets.Item[item.type].Value;
        Rectangle frame = texture.Frame();
        if (Main.itemAnimations[item.type] != null)
            frame = Main.itemAnimations[item.type].GetFrame(texture, Main.itemFrameCounter[whoAmI]);

        Vector2 frameOrigin = frame.Size() * 0.5f;
        Vector2 offset = default;
        offset.ToWorldCoordinates(item.width / 2f - frameOrigin.X, item.height - frame.Height);
        Vector2 drawPos = item.position - Main.screenPosition + frameOrigin + offset;
        float localTime = item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;
        float time = Main.GlobalTimeWrappedHourly % 4f / 2f;
        if (time >= 1f)
            time = 2f - time;
        time = time * 0.5f + 0.5f;
        for (int j = 0; j < 4; j++)
        {
            Vector2 pulseOffset = Vector2.UnitY.RotatedBy((j / 4f + localTime) * (MathHelper.Pi * 2f)) * time * 8f;
            spriteBatch.Draw(texture, drawPos + pulseOffset, frame, new Color(90, 70, 255, 50), rotation, frameOrigin,
                scale, 0, 0f);
        }

        for (int i = 0; i < 3; i++)
        {
            Vector2 pulseOffset2 = Vector2.UnitY.RotatedBy((i / 3f + localTime) * (MathHelper.Pi * 2f)) * time * 4f;
            spriteBatch.Draw(texture, drawPos + pulseOffset2, frame, new Color(140, 120, 255, 77), rotation,
                frameOrigin, scale, 0, 0f);
        }

        return true;
    }

    public static void DrawInventoryCustomScale(SpriteBatch spriteBatch, Texture2D texture, Vector2 position,
        Rectangle frame, Color drawColor, Vector2 origin, float scale, float wantedScale = 1f,
        Vector2 drawOffset = default)
    {
        wantedScale = Math.Max(scale, wantedScale * Main.inventoryScale);
        position += drawOffset * wantedScale;
        spriteBatch.Draw(texture, position, frame, drawColor, 0f, origin, wantedScale, 0, 0f);
    }

    /// <summary>
    /// Draw the bare bones of a projectile
    /// </summary>
    public static void DrawBaseProjectile(this Projectile projectile, Color color,
        SpriteEffects fx = SpriteEffects.None, Texture2D overrideTex = default)
    {
        Texture2D texture = overrideTex ?? projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
        Vector2 drawPosition = projectile.Center;
        Main.spriteBatch.DrawBetter(texture, drawPosition, frame, projectile.GetAlpha(color), projectile.rotation,
            frame.Size() / 2f, projectile.scale, fx);
    }

    public static void DrawBaseProjectile(this Projectile projectile, PixelationLayer layer, BlendState blend,
        Color color,
        SpriteEffects fx = SpriteEffects.None, Texture2D overrideTex = default)
    {
        Texture2D texture = overrideTex ?? projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
        Vector2 drawPosition = projectile.Center;
        SpriteBatch.DrawAltPixelated(layer, blend, texture, drawPosition, frame, projectile.GetAlpha(color),
            projectile.rotation,
            frame.Size() / 2f, projectile.scale, fx);
    }

    public static void DrawProjectileBackglow(this Projectile projectile, Color backglowColor, float backglowArea,
        byte alpha = 0,
        int amount = 10, SpriteEffects spriteEffects = 0, Rectangle? frame = null, Texture2D overrideTexture = null,
        Vector2? orig = null)
    {
        Texture2D texture = overrideTexture ?? TextureAssets.Projectile[projectile.type].Value;

        frame ??= texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);

        Vector2 drawPosition = projectile.Center - Main.screenPosition;
        Vector2 origin = orig ?? frame.Value.Size() * 0.5f;
        Color color = projectile.GetAlpha(backglowColor * projectile.Opacity) with { A = alpha };
        for (int i = 0; i < amount; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / amount).ToRotationVector2() * backglowArea;
            Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, color * 0.95f, projectile.rotation, origin,
                projectile.scale, spriteEffects, 0f);
        }
    }

    public static void DrawProjectileBackglow(this Projectile projectile, PixelationLayer layer, BlendState blend,
        Color backglowColor, float backglowArea,
        byte alpha = 0,
        int amount = 10, SpriteEffects spriteEffects = 0, Rectangle? frame = null, Texture2D overrideTexture = null,
        Vector2? orig = null)
    {
        Texture2D texture = overrideTexture ?? TextureAssets.Projectile[projectile.type].Value;

        frame ??= texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);

        Vector2 drawPosition = projectile.Center - Main.screenPosition;
        Vector2 origin = orig ?? frame.Value.Size() * 0.5f;
        Color color = projectile.GetAlpha(backglowColor * projectile.Opacity) with { A = alpha };
        for (int i = 0; i < amount; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / amount).ToRotationVector2() * backglowArea;
            SpriteBatch.DrawAltPixelated(layer, blend, texture, drawPosition + drawOffset, frame, color * 0.95f,
                projectile.rotation, origin,
                projectile.scale, spriteEffects);
        }
    }

    public static void DrawNPCBackglow(this NPC npc, Color backglowColor, float backglowArea,
        SpriteEffects spriteEffects, Rectangle frame, byte alpha = 0, int amount = 10, Vector2 screenPos = default,
        Texture2D overrideTexture = null)
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
            Main.spriteBatch.Draw(texture, drawPosition + drawOffset, frame, color * 0.95f, npc.rotation, origin,
                npc.scale, spriteEffects, 0f);
        }
    }

    public static SpriteEffects ToSpriteDirection(this int dir) =>
        dir == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

    public static Rectangle ToTarget(Vector2 pos, int width, int height) =>
        new((int) (pos.X - Main.screenPosition.X), (int) (pos.Y - Main.screenPosition.Y), width, height);

    public static Rectangle ToTarget(Vector2 pos, float width, float height) =>
        new((int) (pos.X - Main.screenPosition.X), (int) (pos.Y - Main.screenPosition.Y), (int) width, (int) height);

    public static Rectangle ToTarget(Vector2 pos, Vector2 size) =>
        new((int) (pos.X - Main.screenPosition.X), (int) (pos.Y - Main.screenPosition.Y), (int) size.X, (int) size.Y);

    public static Rectangle ToScreenTarget(Vector2 pos, Vector2 size) =>
        new((int) pos.X, (int) pos.Y, (int) size.X, (int) size.Y);

    public static void RenderRect(this RotatedRectangle rect, Color? col = null)
    {
        Rectangle destinationRectangle = new Rectangle(
            (int)(rect.PivotPoint.X - Main.screenPosition.X),
            (int)(rect.PivotPoint.Y - Main.screenPosition.Y),
            rect.Width,
            rect.Height
        );

        Main.spriteBatch.Draw(
            AssetRegistry.GennedTextures.Pixel,
            destinationRectangle,
            null,
            col ?? Color.White,
            rect.Rotation,
            rect.Pivot * AssetRegistry.GennedTextures.Pixel.Value.Size(),
            SpriteEffects.None,
            0f
        );
    }
}
