using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct SparkleParticleData
    {
        public Color BloomColor;
        public float BloomScale;
        public float Spin;
    }

    private readonly struct SparkleParticleDefinition
    {
        static SparkleParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Sparkle,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CritSpark),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref SparkleParticleData custom = ref p.GetCustomData<SparkleParticleData>();
                    p.Opacity = MathF.Pow(MathHelper.SmoothStep(1, 0, p.TimeRatio), 0.3f);
                    p.Velocity *= 0.89f;
                    p.Rotation += custom.Spin * p.Velocity.X.NonZeroSign() * (p.TimeRatio > 0.5f ? 1f : 0.5f);
                    p.Scale = -MathF.Pow(p.TimeRatio, 7) + 1f * p.Init.Scale;
                    Lighting.AddLight(p.Position, custom.BloomColor.ToVector3() * p.Opacity);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref SparkleParticleData custom = ref p.GetCustomData<SparkleParticleData>();
                    Texture2D sparkTexture = TypeDefinitions[(byte)ParticleTypes.Sparkle].Texture;
                    Texture2D bloomTexture = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                    float properBloomSize = sparkTexture.Height / (float)bloomTexture.Height + 0.05f;
                    sb.DrawBetter(bloomTexture, p.Position, null, custom.BloomColor * p.Opacity * 0.5f, 0f, bloomTexture.Size() / 2f, p.Scale * custom.BloomScale * properBloomSize, 0);
                    sb.DrawBetter(sparkTexture, p.Position, null, p.Color * p.Opacity, p.Rotation, sparkTexture.Size() / 2f, p.Scale, 0);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnSparkleParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, Color bloomColor, float bloomScale = 1f, float spin = 0.1f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Rotation = RandomRotation(),
            Type = ParticleTypes.Sparkle,
            Width = 2,
            Height = 2
        };
        ref SparkleParticleData custom = ref particle.GetCustomData<SparkleParticleData>();
        custom.BloomColor = bloomColor;
        custom.BloomScale = bloomScale;
        custom.Spin = spin;
        SafeSpawn(particle);
    }
}
