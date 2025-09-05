using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;

namespace TheExtraordinaryAdditions.Core.Graphics;

/// <summary>
/// A small manager for drawing more advanced afterimages
/// </summary>
public sealed class FancyAfterimages(int max, Func<Vector2> center)
{
    public readonly int maxAfterimages = max;
    public readonly Func<Vector2> Center = center;

    public List<Afterimage> afterimages;

    [StructLayout(LayoutKind.Auto)]
    public struct Afterimage
    {
        public Afterimage(Vector2 pos, Vector2 scale, float opacity, float rot, SpriteEffects fx = SpriteEffects.None, byte alpha = 0, int glowCount = 0, float glowArea = 0f, Rectangle? frame = null, bool scaleOut = false, float closenessInterpolant = 0f)
        {
            this.pos = pos;
            this.scale = scale;
            this.opacity = opacity;
            this.rot = rot;
            this.fx = fx;
            this.alpha = alpha;
            this.frame = frame;
            this.glowCount = glowCount;
            this.glowArea = glowArea;
            this.scaleOut = scaleOut;
            this.closenessInterpolant = closenessInterpolant;
        }

        public Vector2 pos;
        public readonly Vector2 scale;
        public readonly float opacity;

        /// <summary>
        /// The alpha of this afterimage. Gets brighter the closer to 0.
        /// </summary>
        public readonly byte alpha;
        public readonly float rot;
        public readonly SpriteEffects fx;
        public readonly Rectangle? frame;
        public readonly int glowCount;
        public readonly float glowArea;

        /// <summary>
        /// How close this afterimage should come to the center
        /// </summary>
        public readonly float closenessInterpolant;

        /// <summary>
        /// Should this afterimage scale-out along the trail?
        /// </summary>
        public readonly bool scaleOut;
    }

    public void UpdateFancyAfterimages(Afterimage image)
    {
        if (afterimages == null)
        {
            afterimages = new List<Afterimage>(maxAfterimages);
            for (int i = 0; i < maxAfterimages; ++i)
                afterimages.Add(image);
        }

        // Insert the image
        afterimages.Insert(0, image);

        // Trim excess old data
        if (afterimages.Count > maxAfterimages)
            afterimages.RemoveAt(afterimages.Count - 1);
    }

    public void DrawFancySwordAfterimages(Texture2D tex, Vector2 center, Color[] colors, Vector2 origin, SpriteEffects fx = SpriteEffects.None, float rotOff = 0f, float overallOpacity = 1f, float overallScale = 1f, float colorInterpolantOffset = 0f)
    {
        if (afterimages == null || afterimages.Count == 0)
            return;

        int count = afterimages.Count;
        for (int i = 0; i < count; i++)
        {
            Afterimage afterimage = afterimages[i];
            float interpolant = 1f - InverseLerp(0f, count, i);
            float prev = afterimage.rot + rotOff;
            Vector2 scale = afterimage.scale * MathF.Min(1f, overallScale);
            Color color = (colors.Length == 1 ? colors[0] : MulticolorLerp(interpolant + colorInterpolantOffset, colors))
                with
            { A = (byte)(afterimage.alpha * afterimage.opacity) } * interpolant * afterimage.opacity * overallOpacity;

            Main.spriteBatch.Draw(tex, center - Main.screenPosition, null, color, prev, origin, scale, fx, 0f);

            if (afterimage.glowCount > 0)
            {
                for (int j = 0; j < afterimage.glowCount; j++)
                {
                    Vector2 glowOffset = (MathHelper.TwoPi * j / afterimage.glowCount).ToRotationVector2() * afterimage.glowArea;
                    Main.spriteBatch.Draw(tex, center + glowOffset - Main.screenPosition, null, color, prev, origin, scale, fx, 0f);
                }
            }
        }
    }

    public void DrawFancyAfterimages(Texture2D tex, Color[] colors, float overallOpacity = 1f, float overallScale = 1f, float colorInterpolantOffset = 0f, bool target = false, bool forceRot = false)
    {
        if (afterimages == null || afterimages.Count == 0)
            throw new Exception("No available afterimages!");

        int count = afterimages.Count;
        for (int i = 0; i < count; i++)
        {
            Afterimage afterimage = afterimages[i];
            float interpolant = 1f - InverseLerp(0f, count, i);

            float afterimageClosenessInterpolant = MathHelper.Lerp(-1f, afterimage.closenessInterpolant, 1f);
            Vector2 pos = Vector2.Lerp(afterimage.pos, Center(), afterimageClosenessInterpolant);
            if (!target)
                pos -= Main.screenPosition;

            Rectangle? frame = afterimage.frame;
            Color color = MulticolorLerp(interpolant + colorInterpolantOffset, colors)
                with
            { A = (byte)(afterimage.alpha * afterimage.opacity) } * interpolant * afterimage.opacity * overallOpacity;
            float rotation = forceRot ? afterimages[0].rot : afterimage.rot;
            Vector2 origin = (frame.HasValue ? frame.Value.Size() : tex.Size()) / 2f;
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
                    Vector2 glowOffset = (MathHelper.TwoPi * j / afterimage.glowCount).ToRotationVector2() * afterimage.glowArea;

                    if (target)
                        Main.spriteBatch.Draw(tex, ToTarget(pos + glowOffset, scale), frame, color, rotation, origin, spriteEffects, 0f);
                    else
                        Main.spriteBatch.Draw(tex, pos + glowOffset, frame, color, rotation, origin, scale, spriteEffects, 0f);
                }
            }
        }
    }
}