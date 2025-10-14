using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct TwinkleParticleData
    {
        public int TotalStarPoints;
        public Color BackglowBloomColor;
        public Vector2 ScaleFactor;
    }

    private readonly struct TwinkleParticleDefinition
    {
        static TwinkleParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Twinkle,
                texture: AssetRegistry.GetTexture(AdditionsTexture.StarLong),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref TwinkleParticleData custom = ref p.GetCustomData<TwinkleParticleData>();
                    p.Opacity = GetLerpBump(0f, 10f, p.Lifetime, 16f, p.Time);
                    p.Velocity *= 0.94f;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Twinkle].Texture;
                    ref TwinkleParticleData custom = ref p.GetCustomData<TwinkleParticleData>();
                    Vector2 scale = custom.ScaleFactor * p.Opacity * 0.1f;
                    scale *= MathF.Sin(Main.GlobalTimeWrappedHourly * 30f + p.Time * 0.08f) * 0.125f + 1f;
                    int instanceCount = custom.TotalStarPoints / 2;
                    Color backglowBloomColor = custom.BackglowBloomColor * p.Opacity;
                    Color color = p.Color * p.Opacity;
                    float spokesExtendOffset = 0.6f;

                    sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.GlowSoft), p.Position, null, backglowBloomColor * 0.83f, 0f, AssetRegistry.GetTexture(AdditionsTexture.GlowSoft).Size() * 0.5f, scale * 7.2f, 0);
                    sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.BloomFlare), p.Position, null, color, p.Rotation - Main.GlobalTimeWrappedHourly * 0.9f, AssetRegistry.GetTexture(AdditionsTexture.BloomFlare).Size() * 0.5f, scale * 0.42f, 0);
                    sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.BloomFlare), p.Position, null, color, p.Rotation + Main.GlobalTimeWrappedHourly * 0.91f, AssetRegistry.GetTexture(AdditionsTexture.BloomFlare).Size() * 0.5f, scale * 0.42f, 0);

                    for (int i = 0; i < instanceCount; i++)
                    {
                        float rotationOffset = MathHelper.Pi * i / instanceCount;
                        Vector2 localScale = scale;
                        if (rotationOffset != 0f)
                            localScale *= MathF.Pow(MathF.Sin(rotationOffset), 1.5f);
                        for (float s = 1f; s > 0.3f; s -= 0.2f)
                            sb.DrawBetter(texture, p.Position, null, color, rotationOffset, texture.Size() * 0.5f, new Vector2(1f - (spokesExtendOffset - 0.6f) * 0.4f, spokesExtendOffset) * localScale * s, 0);
                    }
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnTwinkleParticle(Vector2 position, Vector2 velocity, int lifetime, Vector2 scaleFactor, Color color, int totalStarPoints, Color backglowBloomColor = default, float rotation = 0f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = 1f,
            Color = color,
            Opacity = 1f,
            Rotation = rotation,
            Type = ParticleTypes.Twinkle,
            Width = 2,
            Height = 2
        };
        ref TwinkleParticleData custom = ref particle.GetCustomData<TwinkleParticleData>();
        custom.TotalStarPoints = totalStarPoints;
        custom.BackglowBloomColor = backglowBloomColor;
        custom.ScaleFactor = scaleFactor;
        SafeSpawn(particle);
    }
}
