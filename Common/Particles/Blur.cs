using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct BlurParticleData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private readonly struct BlurParticleDefinition
    {
        static BlurParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Blur,
                texture: null,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    // Update custom data
                    ref BlurParticleData custom = ref p.GetCustomData<BlurParticleData>();

                    p.Opacity = p.Init.Opacity * p.LifetimeRatio; // Fade intensity
                    p.Scale = p.Init.Scale * p.LifetimeRatio; // Fade radius
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
            ));
        }
    }

    public static void SpawnBlurParticle(Vector2 position, int lifetime, float intensity, float radius, float sigma = .5f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = radius,
            Color = Color.Transparent,
            Opacity = intensity,
            Type = ParticleTypes.Blur,
            Width = 2,
            Height = 2
        };
        ref BlurParticleData custom = ref particle.GetCustomData<BlurParticleData>();
        custom.Sigma = sigma;

        SafeSpawn(particle);
    }
}
