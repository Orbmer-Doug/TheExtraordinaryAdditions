using System;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Graphics.Resources;

/// <summary>
/// A small manager for drawing more advanced afterimages
/// </summary>
public sealed class FancyAfterimages
{
    private readonly Afterimage[] buffer;
    private int count;
    private int head;
    private readonly Func<Vector2> center;
    private readonly int maxAfterimages;

    public FancyAfterimages(int max, Func<Vector2> center)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(max);
        buffer = new Afterimage[max];
        maxAfterimages = max;
        this.center = center;
        count = 0;
        head = 0;
    }

    public void UpdateFancyAfterimages(Afterimage image)
    {
        int tail = (head + count) % maxAfterimages;
        buffer[tail] = image;

        if (count < maxAfterimages)
            count++;
        else
            head = (head + 1) % maxAfterimages;
    }

    /// <summary>
    /// Clears the afterimage buffer, resetting the trail.
    /// </summary>
    public void Clear()
    {
        count = head = 0;
    }

    public void DrawFancySwordAfterimages(Texture2D tex, Vector2 center, Color[] colors, Vector2 origin,
        SpriteEffects fx = SpriteEffects.None, float rotOff = 0f, float overallOpacity = 1f, float overallScale = 1f,
        float colorInterpolantOffset = 0f, Rectangle? frame = null)
    {
        if (count == 0)
            return;

        int idx = (head + count - 1) % maxAfterimages;
        for (int i = 0; i < count; i++)
        {
            Afterimage afterimage = buffer[idx];
            float interpolant = 1f - InverseLerp(0f, count, i);
            float prev = afterimage.rot + rotOff;
            Vector2 scale = afterimage.scale * MathF.Min(1f, overallScale);
            Color color =
                (colors.Length == 1 ? colors[0] : MulticolorLerp(interpolant + colorInterpolantOffset, colors))
                    with
                    {
                        A = (byte) (afterimage.alpha * afterimage.opacity)
                    } * interpolant * afterimage.opacity * overallOpacity;

            Main.spriteBatch.Draw(tex, center - Main.screenPosition, frame, color, prev, origin, scale, fx, 0f);

            if (afterimage.glowCount > 0)
            {
                for (int j = 0; j < afterimage.glowCount; j++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * j / afterimage.glowCount).ToRotationVector2() *
                                         afterimage.glowArea;
                    Main.spriteBatch.Draw(tex, center + glowOffset - Main.screenPosition, frame, color, prev, origin,
                        scale, fx, 0f);
                }
            }

            idx = (idx - 1 + maxAfterimages) % maxAfterimages;
        }
    }

    public void DrawFancyAfterimages(Texture2D tex, Color[] colors, float overallOpacity = 1f, float overallScale = 1f,
        float colorInterpolantOffset = 0f, bool target = false, bool forceRot = false)
    {
        if (count == 0)
            return;

        int idx = (head + count - 1) % maxAfterimages;
        for (int i = 0; i < count; i++)
        {
            Afterimage afterimage = buffer[idx];
            float interpolant = 1f - InverseLerp(0f, count, i);

            float afterimageClosenessInterpolant = MathHelper.Lerp(-1f, afterimage.closenessInterpolant, 1f);
            Vector2 pos = Vector2.Lerp(afterimage.pos, center(), afterimageClosenessInterpolant);
            if (!target)
                pos -= Main.screenPosition;

            Rectangle? frame = afterimage.frame;
            Color color = MulticolorLerp(interpolant + colorInterpolantOffset, colors)
                with
                {
                    A = (byte) (afterimage.alpha * afterimage.opacity)
                } * interpolant * afterimage.opacity * overallOpacity;
            float rotation = forceRot ? buffer[0].rot : afterimage.rot;
            Vector2 origin = (frame?.Size() ?? tex.Size()) / 2f;
            Vector2 scale = afterimage.scale * overallScale;
            if (afterimage.scaleOut)
                scale *= interpolant;

            SpriteEffects spriteEffects = afterimage.fx;

            if (target)
                Main.spriteBatch.Draw(tex, ToTarget(pos, scale), frame, color, rotation, origin, spriteEffects, 0f);
            else
                Main.spriteBatch.Draw(tex, pos, frame, color, rotation, origin, scale, spriteEffects, 0f);

            if (afterimage.glowCount > 0)
            {
                for (int j = 0; j < afterimage.glowCount; j++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * j / afterimage.glowCount).ToRotationVector2() *
                                         afterimage.glowArea;

                    if (target)
                    {
                        Main.spriteBatch.Draw(tex, ToTarget(pos + glowOffset, scale), frame, color, rotation,
                            origin,
                            spriteEffects, 0f);
                    }
                    else
                    {
                        Main.spriteBatch.Draw(tex, pos + glowOffset, frame, color, rotation, origin, scale,
                            spriteEffects, 0f);
                    }
                }
            }

            idx = (idx - 1 + maxAfterimages) % maxAfterimages;
        }
    }

    public void DrawFancyAfterimagesPixelated(PixelationLayer layer, BlendState blend, Texture2D tex,
        Color[] colors, float overallOpacity = 1f,
        float overallScale = 1f,
        float colorInterpolantOffset = 0f, bool target = false, bool forceRot = false)
    {
        if (count == 0)
            return;

        int idx = (head + count - 1) % maxAfterimages;
        for (int i = 0; i < count; i++)
        {
            Afterimage afterimage = buffer[idx];
            float interpolant = 1f - InverseLerp(0f, count, i);

            float afterimageClosenessInterpolant = MathHelper.Lerp(-1f, afterimage.closenessInterpolant, 1f);
            Vector2 pos = Vector2.Lerp(afterimage.pos, center(), afterimageClosenessInterpolant);
            if (!target)
                pos -= Main.screenPosition;

            Rectangle? frame = afterimage.frame;
            Color color = MulticolorLerp(interpolant + colorInterpolantOffset, colors)
                with
                {
                    A = (byte) (afterimage.alpha * afterimage.opacity)
                } * interpolant * afterimage.opacity * overallOpacity;
            float rotation = forceRot ? buffer[0].rot : afterimage.rot;
            Vector2 origin = (frame?.Size() ?? tex.Size()) / 2f;
            Vector2 scale = afterimage.scale * overallScale;
            if (afterimage.scaleOut)
                scale *= interpolant;

            SpriteEffects spriteEffects = afterimage.fx;

            if (target)
                SpriteBatch.DrawRectPixelated(layer, blend, tex, ToTarget(pos, scale), frame, color, rotation, origin,
                    spriteEffects);
            else
                SpriteBatch.DrawAltPixelated(layer, blend, tex, pos, frame, color, rotation, origin, scale,
                    spriteEffects);

            if (afterimage.glowCount > 0)
            {
                for (int j = 0; j < afterimage.glowCount; j++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * j / afterimage.glowCount).ToRotationVector2() *
                                         afterimage.glowArea;

                    if (target)
                    {
                        SpriteBatch.DrawRectPixelated(
                            layer,
                            blend,
                            tex,
                            ToTarget(pos + glowOffset, scale),
                            frame,
                            color,
                            rotation,
                            origin,
                            spriteEffects);
                    }
                    else
                    {
                        SpriteBatch.DrawAltPixelated(
                            layer,
                            blend,
                            tex,
                            pos + glowOffset,
                            frame,
                            color,
                            rotation,
                            origin,
                            scale,
                            spriteEffects);
                    }
                }
            }

            idx = (idx - 1 + maxAfterimages) % maxAfterimages;
        }
    }

    public readonly struct Afterimage(
        Vector2 pos,
        Vector2 scale,
        float opacity,
        float rot,
        SpriteEffects fx = SpriteEffects.None,
        byte alpha = 0,
        int glowCount = 0,
        float glowArea = 0f,
        Rectangle? frame = null,
        bool scaleOut = false,
        float closenessInterpolant = 0f)
    {
        public readonly Vector2 pos = pos;
        public readonly Vector2 scale = scale;
        public readonly float opacity = opacity;

        /// <summary>
        /// The alpha of this afterimage. Gets brighter the closer to 0.
        /// </summary>
        public readonly byte alpha = alpha;

        public readonly float rot = rot;
        public readonly SpriteEffects fx = fx;
        public readonly Rectangle? frame = frame;
        public readonly int glowCount = glowCount;
        public readonly float glowArea = glowArea;

        /// <summary>
        /// How close this afterimage should come to the center
        /// </summary>
        public readonly float closenessInterpolant = closenessInterpolant;

        /// <summary>
        /// Should this afterimage scale-out along the trail?
        /// </summary>
        public readonly bool scaleOut = scaleOut;
    }
}
