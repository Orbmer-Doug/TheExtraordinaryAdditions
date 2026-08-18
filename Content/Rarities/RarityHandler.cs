using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Daybreak.Common.Features.Rarities;
using Daybreak.Common.Rendering;
using ReLogic.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI.Chat;
using TheExtraordinaryAdditions.Core.Utilities;
using SpriteBatchSnapshot = Daybreak.Common.Rendering.SpriteBatchSnapshot;
using Vector2 = Microsoft.Xna.Framework.Vector2;

namespace TheExtraordinaryAdditions.Content.Rarities;

public class CustomRaritySystem : ModSystem
{
    public override void OnModLoad()
    {
        RarityParticles.RegisterAll();
    }

    public static void GetTextDimensions(DynamicSpriteFont font, string text, Vector2 pos, out Vector2 size,
        out Rectangle rect)
    {
        size = font.MeasureString(text);
        rect = new(0, 0, (int) size.X, (int) (size.Y * .75f));
    }

    public static void DrawTextWithShader(DynamicSpriteFont font, string text, Vector2 position, Color color,
        float rotation,
        Vector2 origin, Vector2 scale, Effect shader)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, shader, Main.UIScaleMatrix);

        ChatManager.DrawColorCodedString(Main.spriteBatch, font, text,
            position, color, rotation, origin, scale);

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor,
            DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);
    }
}

public static class RarityParticles
{
    public enum ParticleType : byte
    {
        Sparkle,
        Star,
        Pixel,
        Holosquare,
        Droplet,
    }

    [StructLayout(LayoutKind.Auto)]
    public unsafe struct ParticleInfo(
        ParticleType type,
        Vector2 position,
        Vector2 velocity,
        int life,
        float scale,
        Color color,
        float opacity,
        float rotation)
    {
        public readonly ParticleType Type = type;
        public int Time;
        public int Lifetime = life;
        public float InitScale;
        public float Scale = scale;
        public float Rotation = rotation;
        public Vector2 Position = position;
        public Vector2 Velocity = velocity;
        public float Opacity = opacity;
        public Color DrawColor = color;

        public readonly float TimeRatio => InverseLerp(0f, Lifetime, Time);

        private const byte CustomDataSize = 128;
        private fixed byte customData[CustomDataSize];
        public Span<byte> CustomData => MemoryMarshal.CreateSpan(ref customData[0], CustomDataSize);

        public ref T GetCustomData<T>() where T : unmanaged
        {
            if (sizeof(T) > CustomDataSize)
                throw new ArgumentException(
                    $"Type {typeof(T).Name} exceeds CustomData size ({CustomDataSize} bytes) by {sizeof(T)}.");
            return ref MemoryMarshal.AsRef<T>(CustomData);
        }
    }

    public delegate void Update(ref ParticleInfo info);

    public delegate void Draw(ref ParticleInfo info, Vector2 offset);

    public readonly struct ParticleDef(Update update, Draw draw, bool additive = false)
    {
        public readonly Update Update = update;
        public readonly Draw Draw = draw;
        public readonly bool Additive = additive;
    }

    public static readonly ParticleDef[] TypeDefinitions =
        new ParticleDef[(int) (GetLastEnumValue<ParticleType>() + 1)];

    public const uint MaxParticles = 256;

    public static void UpdateAndDrawParticles(RarityDrawContext ctx, DynamicSpriteFont font, string text,
        Vector2 position,
        ref ParticleInfo[] particles, ref ulong[] presence)
    {
        if (ctx.DrawKind == RarityDrawContext.Kind.PopupText)
            return;

        var iterator = new BitmaskUtils.IndicesEnumerable(presence.AsSpan(0, presence.Length), MaxParticles);
        foreach (int index in iterator)
        {
            ref ParticleInfo info = ref particles[index];
            info.Time++;
            info.Position += info.Velocity;
            ref ParticleDef def = ref TypeDefinitions[(byte) info.Type];
            def.Update.Invoke(ref info);
            if (info.Time > info.Lifetime)
            {
                BitmaskUtils.SetBit(in presence, index, false);
                continue;
            }
        }

        // this is fine

        if (particles == null)
            return;

        var alpha = particles.Where(f => !TypeDefinitions[(byte) f.Type].Additive).ToArray();
        if (alpha.Length > 0)
        {
            for (int i = 0; i < alpha.Length; i++)
            {
                ref ParticleInfo info = ref alpha[i];
                TypeDefinitions[(byte) info.Type].Draw?.Invoke(ref info, position);
            }
        }

        var additive = particles.Where(f => TypeDefinitions[(byte) f.Type].Additive).ToArray();
        if (additive.Length > 0)
        {
            Main.spriteBatch.End(out SpriteBatchSnapshot ss);
            Main.spriteBatch.Begin(ss with { BlendState = BlendState.Additive });

            for (int i = 0; i < additive.Length; i++)
            {
                ref ParticleInfo info = ref additive[i];
                TypeDefinitions[(byte) info.Type].Draw?.Invoke(ref info, position);
            }

            Main.spriteBatch.Restart(ss);
        }
    }

    public static void Add(ref ParticleInfo[] particles, ref ulong[] presence, in ParticleInfo particle)
    {
        int index = BitmaskUtils.AllocateIndex(presence, MaxParticles, true);
        particles[index] = particle with { InitScale = particle.Scale };
    }

    public static void RegisterAll()
    {
        SparkleDef();
        StarDef();
        PixelDef();
        HolosquareDef();
        DropletDef();
    }

    private static void SparkleDef()
    {
        TypeDefinitions[(byte) ParticleType.Sparkle] = new ParticleDef(
            (ref info) =>
            {
                info.Scale *= 0.97f;
                info.Opacity = MathF.Pow(MathHelper.SmoothStep(1, 0, info.TimeRatio), .1f);
                info.Rotation = info.Velocity.ToRotation() + MathHelper.PiOver2;
                info.Velocity *= .96f;
            },
            (ref info, off) =>
            {
                Texture2D tex = AssetRegistry.GennedTextures.Gleam;
                Vector2 orig = tex.Size() / 2;
                Main.spriteBatch.Draw(tex, info.Position + off, null, info.DrawColor * .15f * info.Opacity,
                    info.Rotation, orig,
                    new Vector2(.5f, 1.4f) * info.Scale * 2f, 0, 0f);
                Main.spriteBatch.Draw(tex, info.Position + off, null,
                    info.DrawColor.Lerp(Color.White, .1f) * .5f * info.Opacity, info.Rotation,
                    orig, new Vector2(.4f, 1.2f) * info.Scale * 1.5f, 0, 0f);
                Main.spriteBatch.Draw(tex, info.Position + off, null,
                    info.DrawColor.Lerp(Color.White, .2f) * info.Opacity, info.Rotation, orig,
                    new Vector2(.3f, 1f) * info.Scale, 0, 0f);
            },
            additive: true
        );
    }

    public static void SpawnSparkle(ref ParticleInfo[] particles, ref ulong[] presence, Vector2 pos, Vector2 vel,
        int life, float scale,
        Color col)
    {
        Add(ref particles, ref presence, new ParticleInfo(
            ParticleType.Sparkle,
            pos,
            vel,
            life,
            scale,
            col,
            1f,
            0f
        ));
    }

    private static void StarDef()
    {
        TypeDefinitions[(byte) ParticleType.Star] = new ParticleDef(
            (ref info) =>
            {
                info.Opacity = MathHelper.SmoothStep(1f, 0f, info.TimeRatio);
                info.Rotation += info.Velocity.Length() * .07f;
                info.Scale = (1f - info.TimeRatio) * info.InitScale;
                info.Velocity *= .92f;
            },
            (ref info, off) =>
            {
                Texture2D tex = AssetRegistry.GennedTextures.CritSpark;
                Texture2D bloom = AssetRegistry.GennedTextures.GlowParticleSmall;
                Vector2 bloomScale = tex.Size() / bloom.Size() + new Vector2(.05f);
                Main.spriteBatch.Draw(bloom, info.Position + off, null, info.DrawColor * .12f * info.Opacity, 0f,
                    bloom.Size() / 2,
                    bloomScale, 0, 0f);
                Main.spriteBatch.Draw(tex, info.Position + off, null, info.DrawColor * info.Opacity, info.Rotation,
                    tex.Size() / 2,
                    info.Scale, 0, 0f);
            },
            additive: true
        );
    }

    public static void SpawnStar(ref ParticleInfo[] particles, ref ulong[] presence, Vector2 pos, Vector2 vel, int life,
        float scale,
        Color col)
    {
        Add(ref particles, ref presence, new ParticleInfo(
            ParticleType.Star,
            pos,
            vel,
            life,
            scale,
            col,
            1f,
            RandomRotation()
        ));
    }

    private unsafe struct PixelData
    {
        public Color BloomColor;
        public Vector2? HomeIn;
        public byte TrailLength;
        public float Delay;
        public float Timer;
        public Vector2 InitVel;

        private const int Max = 10;
        private fixed float oldPositions[Max * 2];

        public Span<Vector2> OldPositions =>
            MemoryMarshal.CreateSpan(ref Unsafe.As<float, Vector2>(ref oldPositions[0]), Max);
    }

    private static void PixelDef()
    {
        TypeDefinitions[(byte) ParticleType.Pixel] = new ParticleDef(
            (ref info) =>
            {
                ref PixelData data = ref info.GetCustomData<PixelData>();
                Span<Vector2> oldPos = data.OldPositions;
                for (int i = oldPos.Length - 1; i >= 1; i--)
                    oldPos[i] = oldPos[i - 1];
                oldPos[0] = info.Position;

                if (info.TimeRatio > .7f)
                {
                    info.Scale *= .9f;
                    info.Opacity *= .92f;
                }

                Vector2? home = data.HomeIn;
                if (home != null)
                {
                    info.Velocity = Vector2.Lerp(info.Velocity, info.Position.SafeDirectionTo(home.Value) * 5f, .2f);
                    if (info.Position.WithinRange(home.Value, 10f))
                        info.Time = info.Lifetime;
                }
                else
                {
                    info.Velocity = data.InitVel.VelEqualTrig(MathF.Cos, 20f, .4f, ref data.Delay, ref data.Timer);
                    info.Velocity *= .96f;
                }

                info.Rotation += info.Velocity.Length() / 5;
            },
            (ref info, off) =>
            {
                Texture2D tex = AssetRegistry.GennedTextures.Pixel;
                Texture2D bloom = AssetRegistry.GennedTextures.GlowParticleSmall;
                ref PixelData data = ref info.GetCustomData<PixelData>();
                Main.spriteBatch.Draw(bloom, info.Position + off, null, data.BloomColor * info.Opacity * .3f,
                    info.Rotation, bloom.Size() / 2, info.Scale / 4,
                    0, 0f);

                if (data.TrailLength > 0)
                {
                    for (int i = 0; i < data.TrailLength && i < data.OldPositions.Length; i++)
                    {
                        Vector2 old = data.OldPositions[i];
                        float completion = 1f - InverseLerp(0f, data.OldPositions.Length, i);
                        Main.spriteBatch.Draw(tex, old + off, null, info.DrawColor * info.Opacity * completion,
                            info.Rotation, tex.Size() / 2, info.Scale * 6 * completion, 0, 0f);
                    }
                }
                else
                    Main.spriteBatch.Draw(tex, info.Position + off, null, info.DrawColor * info.Opacity, info.Rotation,
                        tex.Size() / 2,
                        info.Scale * 6, 0, 0f);
            },
            additive: true
        );
    }

    public static void SpawnPixel(ref ParticleInfo[] particles, ref ulong[] presence, Vector2 pos, Vector2 vel,
        int life, float scale,
        Color col, Color bloom, Vector2? home = null,
        byte trail = 0)
    {
        ParticleInfo info = new ParticleInfo(
            ParticleType.Pixel,
            pos,
            vel,
            life,
            scale,
            col,
            1f,
            RandomRotation()
        );
        ref PixelData data = ref info.GetCustomData<PixelData>();
        data.BloomColor = bloom;
        data.HomeIn = home;
        data.OldPositions.Fill(pos);
        data.TrailLength = trail;
        data.InitVel = vel;
        Add(ref particles, ref presence, info);
    }

    public struct HolosquareData
    {
        public int Variant;
        public Rectangle TechFrame;
        public float Strength;
    }

    private static void HolosquareDef()
    {
        TypeDefinitions[(byte) ParticleType.Holosquare] = new ParticleDef(
            (ref info) =>
            {
                if (info.Time < 3f)
                    info.Velocity *= 1.2f;
                else
                    info.Velocity *= .975f;

                float completion = GetLerpBump(0f, .1f, 1f, .7f, info.TimeRatio);
                info.Scale = completion * info.InitScale;
                info.Opacity = completion * info.GetCustomData<HolosquareData>().Strength;

                info.Rotation = info.Velocity.ToRotation();
            },
            (ref info, off) =>
            {
                Texture2D tex = AssetRegistry.GennedTextures.TechyHolosquare;
                ref HolosquareData data = ref info.GetCustomData<HolosquareData>();

                for (int i = -1; i <= 1; i++)
                {
                    Color aberrationColor = i switch
                    {
                        -1 => new Color(255, 0, 0, 0),
                        0 => new Color(0, 255, 0, 0),
                        1 => new Color(0, 0, 255, 0),
                        _ => Color.White
                    };
                    Vector2 offset = Vector2.UnitX.RotatedBy(info.Rotation).RotatedBy(MathHelper.PiOver2) * i;
                    offset *= data.Strength;

                    Main.spriteBatch.Draw(tex, info.Position + off + offset, data.TechFrame,
                        info.DrawColor.MultiplyRGB(aberrationColor) * info.Opacity, info.Rotation,
                        data.TechFrame.Size() / 2f,
                        new Vector2(info.Scale, 1f), 0, 0f);
                }
            },
            additive: true
        );
    }

    public static void SpawnHolosquare(ref ParticleInfo[] particles, ref ulong[] presence, Vector2 pos, Vector2 vel,
        int life, float scale,
        Color col, float opacity = 1f, float strength = 1.5f)
    {
        ParticleInfo info = new ParticleInfo(
            ParticleType.Holosquare,
            pos,
            vel,
            life,
            scale,
            col,
            opacity,
            RandomRotation()
        );
        ref HolosquareData data = ref info.GetCustomData<HolosquareData>();
        data.Variant = Main.rand.Next(6);
        data.TechFrame = data.Variant switch
        {
            0 => new Rectangle(8, 0, 6, 6),
            1 => new Rectangle(6, 8, 10, 6),
            2 => new Rectangle(4, 16, 14, 8),
            3 => new Rectangle(2, 26, 18, 10),
            4 => new Rectangle(2, 38, 18, 8),
            5 => new Rectangle(6, 48, 12, 12),
            _ => data.TechFrame
        };
        data.Strength = strength;
        Add(ref particles, ref presence, info);
    }

    private static void DropletDef()
    {
        TypeDefinitions[(byte) ParticleType.Droplet] = new ParticleDef(
            (ref info) =>
            {
                info.DrawColor.A = 0;
                info.Scale = MathF.Pow(MathHelper.SmoothStep(1, 0, info.TimeRatio), .4f);
                info.Opacity *= .98f;
                info.Rotation = info.Velocity.ToRotation() - MathHelper.PiOver2;
                info.Velocity *= .92f;
            },
            (ref info, off) =>
            {
                Texture2D tex = AssetRegistry.GennedTextures.DropletTexture;
                Main.spriteBatch.Draw(tex, info.Position + off, null, info.DrawColor * info.Opacity * .4f,
                    info.Rotation, tex.Size() / 2, info.Scale / 3 * 1.2f, 0, 0f);

                Main.spriteBatch.Draw(tex, info.Position + off, null, info.DrawColor * info.Opacity, info.Rotation,
                    tex.Size() / 2,
                    info.Scale / 3, 0, 0f);
            },
            additive: false
        );
    }

    public static void SpawnDroplet(ref ParticleInfo[] particles, ref ulong[] presence, Vector2 pos, Vector2 vel,
        int life, float scale,
        Color col)
    {
        Add(ref particles, ref presence, new ParticleInfo(
            ParticleType.Droplet,
            pos,
            vel,
            life,
            scale,
            col,
            1f,
            0f
        ));
    }
}
