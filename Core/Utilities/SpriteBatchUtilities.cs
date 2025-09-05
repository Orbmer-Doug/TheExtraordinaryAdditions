using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using System;
using System.Reflection;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Capture;
using Terraria.ID;

namespace TheExtraordinaryAdditions.Core.Utilities;

public static partial class Utility
{
    public static Rectangle GetFrameRectangle(Point size, int frameX, int startY = 0, int startX = 0)
    {
        int x = startX + (frameX * size.X);
        return new Rectangle(x, startY, size.X, size.Y);
    }

    public static Color GetMostCommonColor(Texture2D texture)
    {
        // Get pixel data
        Color[] pixels = new Color[texture.Width * texture.Height];
        texture.GetData(pixels);

        // Use array as hash table for counting (assuming 32-bit ARGB)
        int[] colorCounts = new int[1 << 24]; // 16.7M possible colors (ignoring alpha for simplicity)
        int maxCount = 0;
        int dominantColorValue = 0;

        // Count occurrences of each color
        for (int i = 0; i < pixels.Length; i++)
        {
            // Pack RGB into 24-bit integer (ignoring alpha)
            int colorValue = (pixels[i].R << 16) | (pixels[i].G << 8) | pixels[i].B;
            int count = ++colorCounts[colorValue];

            // Track most frequent color
            if (count > maxCount)
            {
                maxCount = count;
                dominantColorValue = colorValue;
            }
        }

        // Reconstruct Color from 24-bit value
        byte r = (byte)((dominantColorValue >> 16) & 0xFF);
        byte g = (byte)((dominantColorValue >> 8) & 0xFF);
        byte b = (byte)(dominantColorValue & 0xFF);

        return new Color(r, g, b);
    }

    public static Texture2D GeneratePaletteTexture(int colorCount)
    {
        Color[] palette = GenerateHuePalette(colorCount, saturation: 1f, value: 1f, hueStart: 0f, hueEnd: 1f);
        Texture2D texture = new(Main.instance.GraphicsDevice, colorCount, 1, false, SurfaceFormat.Color);
        texture.SetData(palette);
        return texture;
    }

    public static Color[] GenerateHuePalette(int colorCount, float saturation = 1f, float value = 1f, float hueStart = 0f, float hueEnd = 1f)
    {
        Color[] palette = new Color[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            float t = i / (float)(colorCount - 1); // 0 to 1 across the palette
            float hue = MathHelper.Lerp(hueStart, hueEnd, t); // Interpolate hue
            palette[i] = HSVToRGB(hue, saturation, value);
        }
        return palette;
    }

    // Similar setup like in HLSL
    public static Color HSVToRGB(float h, float s, float v)
    {
        SystemVector3 c = new(h, s, v);
        SystemVector4 K = new(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
        SystemVector3 p = SystemVector3.Abs(Fract(c.XXX() + K.XYZ()) * 6.0f - K.WWW());
        SystemVector3 rgb = c.Z * SystemVector3.Lerp(K.XXX().ToVector3(), SystemVector3.Clamp(p - K.XXX().ToVector3(), new(0f), new(1f)), c.Y);

        return new Color(rgb.X, rgb.Y, rgb.Z);
    }
    public static SystemVector3 ToVector3(this SystemVector4 v) => new SystemVector3(v.X, v.Y, v.Z) * v.W;
    public static SystemVector4 XXX(this SystemVector4 v) => new(v.X, v.X, v.X, v.X);
    public static SystemVector3 XXX(this SystemVector3 v) => new(v.X, v.X, v.X);
    public static SystemVector3 XYZ(this SystemVector4 v) => new(v.X, v.Y, v.Z);
    public static SystemVector3 WWW(this SystemVector4 v) => new(v.W, v.W, v.W);
    public static SystemVector3 Fract(this SystemVector3 v) => v - new SystemVector3(MathF.Floor(v.X), MathF.Floor(v.Y), MathF.Floor(v.Z));

    public static void DrawText(this SpriteBatch spriteBatch, string text, Vector2 position, Color color)
    {
        spriteBatch.DrawText(text, 0, position, color, Color.Black);
    }

    public static void DrawText(this SpriteBatch spriteBatch, string text, int thickness, Vector2 position, Color textColor, Color shadowColor, Vector2 origin = default, float scale = 1f)
    {
        for (int i = -thickness; i <= thickness; i++)
        {
            for (int k = -thickness; k <= thickness; k++)
            {
                if (i == 0 && k == 0)
                    continue;

                float alpha = MathHelper.Lerp(1f, 0f, Math.Abs((i + k) / 2f));
                spriteBatch.DrawString(FontAssets.MouseText.Value, text, position + new Vector2(i, k), Color.Multiply(shadowColor, alpha), 0f, origin, scale, SpriteEffects.None, 0f);
            }
        }

        spriteBatch.DrawString(FontAssets.MouseText.Value, text, position, textColor, 0f, origin, scale, SpriteEffects.None, 0f);
    }

    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
    {
        spriteBatch.DrawLine(start, MathF.Atan2(end.Y - start.Y, end.X - start.X), Vector2.Distance(start, end), color, thickness);
    }

    public static void DrawLine(this SpriteBatch spriteBatch, Vector2 start, float rotation, float length, Color color, float thickness)
    {
        spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.Pixel), start, AssetRegistry.GetTexture(AdditionsTexture.Pixel).Bounds, color, rotation, new Vector2(0f, 0.5f), new Vector2(length, thickness), SpriteEffects.None, 0);
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

    public static Vector2 CreatePixelationResolution(Vector2 areaSize, Vector2? scale = null) => areaSize / (2 * (scale ?? Vector2.One));

    public static RasterizerState OverflowHiddenRasterizerState { get; }
    static Utility()
    {
        OverflowHiddenRasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };
    }

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


    /// <summary>
    /// Prepares a specialized <see cref="RasterizerState"/> with enabled screen culling, for efficiency reasons. It also informs the <see cref="GraphicsDevice"/> of this change.
    /// </summary>
    public static RasterizerState PrepareScreenCullRasterizer()
    {
        // Apply the screen culling.
        Main.instance.GraphicsDevice.ScissorRectangle = new(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4);
        return DefaultRasterizerScreenCull;
    }

    private static BlendState subtractiveBlending;

    /// <summary>
    /// A blend state that works opposite to <see cref="BlendState.Additive"/>, making colors darker based on intensity rather than brighter.
    /// </summary>
    public static BlendState SubtractiveBlending
    {
        get
        {
            subtractiveBlending ??= new()
            {
                ColorSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.One,
                ColorBlendFunction = BlendFunction.ReverseSubtract,
                AlphaSourceBlend = Blend.SourceAlpha,
                AlphaDestinationBlend = Blend.One,
                AlphaBlendFunction = BlendFunction.ReverseSubtract
            };

            return subtractiveBlending;
        }
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
        // Local variables for convinience.
        GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
        SpriteBatch spriteBatch = Main.spriteBatch;

        // If we are in the menu, a server, or any of these are null, return.
        if (Main.gameMenu || Main.dedServ || renderTarget is null || graphicsDevice is null || spriteBatch is null)
            return;

        // Otherwise set the render target.
        graphicsDevice.SetRenderTarget(renderTarget);

        // "Flush" the screen, removing any previous things drawn to it.
        flushColor ??= Color.Transparent;
        graphicsDevice.Clear(flushColor.Value);
    }

    public static void DrawLineBetter(this SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float width)
    {
        // Draw nothing if the start and end are equal, to prevent division by 0 problems.
        if (start == end)
            return;

        start -= Main.screenPosition;
        end -= Main.screenPosition;

        Texture2D line = AssetRegistry.GetTexture(AdditionsTexture.Line);
        float rotation = (end - start).ToRotation();
        Vector2 scale = new(Vector2.Distance(start, end) / line.Width, width);

        spriteBatch.Draw(line, start, null, color, rotation, line.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);
    }

    public static void DrawHook(this Projectile projectile, Texture2D hookTexture, float angleAdditive = 0f)
    {
        Player player = Main.player[projectile.owner];
        Vector2 center = projectile.Center;
        float angleToMountedCenter = projectile.AngleTo(player.MountedCenter) - MathHelper.Pi / 2f;
        bool canShowHook = true;
        while (canShowHook)
        {
            Vector2 val = player.MountedCenter - center;
            float distanceMagnitude = val.Length();
            if (distanceMagnitude < hookTexture.Height + 1f)
            {
                canShowHook = false;
                continue;
            }
            if (float.IsNaN(distanceMagnitude))
            {
                canShowHook = false;
                continue;
            }
            center += projectile.SafeDirectionTo(player.MountedCenter) * hookTexture.Height;
            Color tileAtCenterColor = Lighting.GetColor((int)center.X / 16, (int)(center.Y / 16f));
            Main.spriteBatch.Draw(hookTexture, center - Main.screenPosition, (Rectangle?)new Rectangle(0, 0, hookTexture.Width, hookTexture.Height), tileAtCenterColor, angleToMountedCenter + angleAdditive, Utils.Size(hookTexture) / 2f, 1f, 0, 0f);
        }
    }

    public static bool DrawTreasureBagInWorld(Item item, SpriteBatch spriteBatch, float rotation, float scale, int whoAmI)
    {
        Texture2D texture = TextureAssets.Item[item.type].Value;
        Rectangle frame = Utils.Frame(texture, 1, 1, 0, 0, 0, 0);
        if (Main.itemAnimations[item.type] != null)
        {
            frame = Main.itemAnimations[item.type].GetFrame(texture, Main.itemFrameCounter[whoAmI]);
        }
        Vector2 frameOrigin = Utils.Size(frame) * 0.5f;
        Vector2 offset = default;
        offset.ToWorldCoordinates(item.width / 2 - frameOrigin.X, item.height - frame.Height);
        Vector2 drawPos = item.position - Main.screenPosition + frameOrigin + offset;
        float localTime = item.timeSinceItemSpawned / 240f + Main.GlobalTimeWrappedHourly * 0.04f;
        float time = Main.GlobalTimeWrappedHourly % 4f / 2f;
        if (time >= 1f)
        {
            time = 2f - time;
        }
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

    public static void TreasureBagLightAndDust(this Item item)
    {
        Vector2 center = item.Center;
        Color val = Color.White;
        Lighting.AddLight(center, ((Color)val).ToVector3() * 0.4f);
        if (item.timeSinceItemSpawned % 12 == 0)
        {
            Vector2 val2 = item.Center + new Vector2(0f, item.height * -0.1f);
            Vector2 direction = Utils.NextVector2CircularEdge(Main.rand, item.width * 0.6f, item.height * 0.6f);
            float distance = 0.3f + Utils.NextFloat(Main.rand) * 0.5f;
            Vector2 velocity = default;
            velocity.ToWorldCoordinates(0f, (0f - Utils.NextFloat(Main.rand)) * 0.3f - 1.5f);
            Vector2 val3 = val2 + direction * distance;
            Vector2? val4 = velocity;
            val = default;
            Dust obj = Dust.NewDustPerfect(val3, 279, val4, 0, val, 1f);
            obj.scale = 0.5f;
            obj.fadeIn = 1.1f;
            obj.noGravity = true;
            obj.noLight = true;
            obj.alpha = 0;
        }
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
    public static void DrawInventoryCustomScale(SpriteBatch spriteBatch, Texture2D texture, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale, float wantedScale = 1f, Vector2 drawOffset = default)
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