using Microsoft.Xna.Framework.Graphics;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    internal struct ChromaticAberrationData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private readonly struct ChromaticAberrationDefinition
    {
        static ChromaticAberrationDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.ChromaticAberration,
                texture: null,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref ChromaticAberrationData custom = ref p.GetCustomData<ChromaticAberrationData>();

                    p.Opacity = p.Init.Opacity * p.LifetimeRatio; // Fade intensity
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
            ));
        }
    }

    public static void SpawnChromaticAberration(Vector2 position, int lifetime, float intensity, float radius, float sigma = .5f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = radius,
            Color = Color.Transparent,
            Opacity = intensity,
            Type = ParticleTypes.ChromaticAberration,
            Width = 2,
            Height = 2
        };
        ref ChromaticAberrationData custom = ref particle.GetCustomData<ChromaticAberrationData>();
        custom.Sigma = sigma;

        SafeSpawn(particle);
    }
}
