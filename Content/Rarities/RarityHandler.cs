using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities;

public abstract class Behavior<TInfo>
        where TInfo : struct
{
    public abstract string TexturePath { get; }
    public abstract void Update();
    public abstract void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line = null);
    public virtual bool UseAdditive { get; } = false;
    public TInfo Info;
}

[StructLayout(LayoutKind.Auto)]
public struct RarityParticleInfo(Vector2 position, Vector2 velocity, int life, float scale, Color color, float opacity, float rotation)
{
    public Texture2D Texture;
    public int Type;

    public int Time;
    public int Lifetime = life;
    public float InitScale;
    public float Scale = scale;
    public float Rotation = rotation;
    public Vector2 Position = position;
    public Vector2 Velocity = velocity;
    public float Opacity = opacity;
    public Color DrawColor = color;

    public Rectangle? BaseFrame;
    public readonly float TimeRatio => InverseLerp(0f, Lifetime, Time);
}

public class CustomRaritySystem : ModSystem
{
    public static void GetTextDimensions(in DrawableTooltipLine line, out Vector2 size, out Rectangle rect)
    {
        size = line.Font.MeasureString(line.Text);
        rect = new(-(int)(size.X * 0.5f), -(int)(size.Y * 0.5f), (int)size.X, (int)(size.Y * .75f));
    }

    public static readonly Texture2D GlowTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
    public static void DrawTextWithGlow(DrawableTooltipLine line, Color glowColor, Color textOuterColor, Color? textInnerColor = null, Texture2D glowTexture = null)
    {
        textInnerColor ??= Color.Black;
        glowTexture ??= GlowTexture;

        string text = line.Text;
        Vector2 textSize = line.Font.MeasureString(text);
        Vector2 textCenter = textSize * 0.5f;
        Vector2 textPosition = new(line.X, line.Y);

        // Get the position to draw the glow behind the text.
        Vector2 glowPosition = new(line.X + textCenter.X, line.Y + textCenter.Y / 1.5f);

        // Get the scale of the glow texture based off of the text size.
        Vector2 glowScale = new(textSize.X * 0.115f, 0.6f);
        glowColor.A = 0;

        // Draw the glow texture.
        Main.spriteBatch.DrawBetterRect(glowTexture, ToScreenTarget(glowPosition, textSize * 2.2f), null, glowColor * .85f, 0f, glowTexture.Size() / 2f);

        // Get an offset to the afterimageOffset based on a sine wave.
        float sine = (float)((1 + Math.Sin(Main.GlobalTimeWrappedHourly * 2.5f)) / 2);
        float sineOffset = MathHelper.Lerp(0.5f, 1f, sine);

        // Draw text backglow effects.
        for (int i = 0; i < 12; i++)
        {
            Vector2 afterimageOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly).ToRotationVector2() * (2f * sineOffset);

            ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text,
                (textPosition + afterimageOffset).RotatedBy(MathHelper.TwoPi * (i / 12)),
                textOuterColor * 0.9f, line.Rotation, line.Origin, line.BaseScale);
        }

        // Draw the main inner text.
        Color mainTextColor = Color.Lerp(glowColor, textInnerColor.Value, 0.9f);
        ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text,
            textPosition, mainTextColor, line.Rotation, line.Origin, line.BaseScale);
    }

    public static void DrawTextWithShader(DrawableTooltipLine line, Effect shader, Color textInnerColor)
    {
        string text = line.Text;
        Vector2 textSize = line.Font.MeasureString(text);
        Vector2 textCenter = textSize * 0.5f;
        Vector2 textPosition = new(line.X, line.Y);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, shader, Main.UIScaleMatrix);

        ChatManager.DrawColorCodedString(Main.spriteBatch, line.Font, text,
            textPosition, textInnerColor, line.Rotation, line.Origin, line.BaseScale);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
    }

    public static void UpdateAndDrawParticles(DrawableTooltipLine line, ref List<Behavior<RarityParticleInfo>> particleList)
    {
        if (!Main.mouseText)
            return;

        GetTextDimensions(in line, out Vector2 textSize, out Rectangle textRect);

        for (int i = 0; i < particleList.Count; i++)
        {
            ref RarityParticleInfo info = ref particleList[i].Info;
            info.Time++;

            if (info.Time > info.Lifetime)
                particleList.RemoveAt(i);

            info.Position += info.Velocity;
            particleList[i].Update();
        }

        var alpha = particleList.Where(p => !p.UseAdditive).ToList();
        var additive = particleList.Where(p => p.UseAdditive).ToList();

        if (alpha.Count > 0)
        {
            foreach (Behavior<RarityParticleInfo> particle in alpha)
            {
                RarityParticleInfo info = particle.Info;
                particle.Draw(Main.spriteBatch, new Vector2(line.X, line.Y) + textSize * 0.5f + info.Position);
            }
        }

        if (additive.Count > 0)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

            foreach (Behavior<RarityParticleInfo> particle in additive)
            {
                RarityParticleInfo info = particle.Info;
                particle.Draw(Main.spriteBatch, new Vector2(line.X, line.Y) + textSize * 0.5f + info.Position, line);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor, DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
        }
    }

    public override void OnModLoad()
    {
        LoadParticles(Mod.Code);
    }

    internal static readonly Dictionary<Type, int> particleTypeLookup = [];
    internal static readonly Dictionary<int, Texture2D> particleTextureLookup = [];
    public static void LoadParticles(Assembly assembly)
    {
        int currentParticleID = 0;

        if (particleTypeLookup.Count != 0 && particleTypeLookup != null)
            currentParticleID = particleTypeLookup.Values.Max() + 1;

        foreach (Type particleType in AssemblyManager.GetLoadableTypes(assembly))
        {
            // Don't attempt to load abstract types
            if (!particleType.IsSubclassOf(typeof(Behavior<RarityParticleInfo>)) || particleType.IsAbstract)
                continue;

            Behavior<RarityParticleInfo> particle = (Behavior<RarityParticleInfo>)RuntimeHelpers.GetUninitializedObject(particleType);

            // Store an ID for the particle. All particles of this type that are spawned will copy the ID
            particleTypeLookup[particleType] = currentParticleID;

            // Store the particle's texture in the lookup table
            Texture2D particleTexture = ModContent.Request<Texture2D>(particle.TexturePath, AssetRequestMode.ImmediateLoad).Value;
            particleTextureLookup[currentParticleID] = particleTexture;

            // Increment the particle ID
            currentParticleID++;
        }
    }

    public static Behavior<RarityParticleInfo> Spawn(ref List<Behavior<RarityParticleInfo>> list, Behavior<RarityParticleInfo> particle)
    {
        if (Main.dedServ)
            return particle;

        ref RarityParticleInfo info = ref particle.Info;

        info.Time = new();
        info.InitScale = info.Scale;
        info.Type = particleTypeLookup[particle.GetType()];
        info.Texture = particleTextureLookup[info.Type];
        list.Add(particle);

        return particle;
    }
}