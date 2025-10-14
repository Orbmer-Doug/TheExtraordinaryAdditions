using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct CloudParticleData
    {
        public Color StartingColor;
        public Color EndingColor;
        public bool LightEffected;
        public float OpacityMultiplier;
        public byte TexType;
    }

    private readonly struct CloudParticleDefinition
    {
        static CloudParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Cloud,
                texture: AssetRegistry.GetTexture(AdditionsTexture.NebulaGas1),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref CloudParticleData custom = ref p.GetCustomData<CloudParticleData>();
                    p.Velocity *= 0.987f;
                    p.Color = Color.Lerp(custom.StartingColor, custom.EndingColor, p.LifetimeRatio);
                    p.Rotation += (Math.Abs(p.Velocity.X) + Math.Abs(p.Velocity.Y)) * .007f * p.Velocity.X.NonZeroSign();
                    p.Scale += 0.009f;
                    p.Opacity = GetLerpBump(0f, .1f, 1f, .7f, p.TimeRatio) * custom.OpacityMultiplier;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref CloudParticleData custom = ref p.GetCustomData<CloudParticleData>();
                    Texture2D texture = custom.TexType switch
                    {
                        2 => AssetRegistry.GetTexture(AdditionsTexture.NebulaGas3),
                        1 => AssetRegistry.GetTexture(AdditionsTexture.NebulaGas2),
                        _ => AssetRegistry.GetTexture(AdditionsTexture.NebulaGas1)
                    };

                    float brightness = MathF.Pow(Lighting.Brightness((int)(p.Position.X / 16f), (int)(p.Position.Y / 16f)), 0.15f) * 0.9f;
                    Color col = p.Color * p.Opacity * (custom.LightEffected ? brightness : 1f);
                    sb.DrawBetterRect(texture, ToTarget(p.Position, new(p.Scale * 2f)), null, col, p.Rotation, texture.Size() / 2f, 0);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    /// <param name="scale">In pixels</param>
    public static void SpawnCloudParticle(Vector2 position, Vector2 velocity, Color startColor, Color endColor, int lifetime, float scale, float opacityEffectiveness, byte texture = 0, bool lightBrightness = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = startColor,
            Opacity = 1f,
            Rotation = RandomRotation(),
            Type = ParticleTypes.Cloud,
            Width = 2,
            Height = 2
        };
        ref CloudParticleData custom = ref particle.GetCustomData<CloudParticleData>();
        custom.StartingColor = startColor;
        custom.EndingColor = endColor;
        custom.LightEffected = lightBrightness;
        custom.OpacityMultiplier = opacityEffectiveness;
        custom.TexType = texture;
        SafeSpawn(particle);
    }
}
