using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct MenacingParticleData
    {
        public float Time;
        public float Delay;
    }

    private readonly struct MenacingParticleDefinition
    {
        static MenacingParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Menacing,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Menacing),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref MenacingParticleData custom = ref p.GetCustomData<MenacingParticleData>();
                    if (p.Time <= 10f)
                        p.Opacity = MathHelper.Clamp(p.Opacity + 0.1f, 0f, 1f);
                    else if (p.Time >= p.Lifetime - 10f)
                        p.Opacity = MathHelper.Clamp(p.Opacity - 0.1f, 0f, 1f);

                    float scaleSine = (1f + MathF.Sin(p.Time * 0.25f)) / 2f;
                    p.Velocity = p.Init.Velocity.VelEqualTrig(MathF.Sin, 30f, .4f, ref custom.Delay, ref custom.Time);
                    p.Scale = MathHelper.Lerp(p.Init.Scale * 0.85f, p.Init.Scale, scaleSine);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Menacing].Texture;
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity, p.Rotation, texture.Size() / 2, p.Scale, 0);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnMenacingParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.Menacing,
        };
        SafeSpawn(particle);
    }
}
