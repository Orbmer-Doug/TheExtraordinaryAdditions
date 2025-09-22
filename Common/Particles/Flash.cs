using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct FlashParticleData
    {
        public float Sigma; // Gaussian falloff smoothness (0.5 to 2.0)
    }

    private readonly struct FlashParticleDefinition
    {
        static FlashParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Flash,
                texture: null,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    // Update custom data
                    ref FlashParticleData custom = ref p.GetCustomData<FlashParticleData>();

                    p.Opacity = p.Init.Opacity * p.LifetimeRatio; // Fade intensity
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
            ));
        }
    }

    public static void SpawnFlash(Vector2 position, int lifetime, float intensity, float radius, float sigma = .5f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = radius,
            Color = Color.Transparent,
            Opacity = intensity,
            Type = ParticleTypes.Flash,
            Width = 2,
            Height = 2
        };
        ref FlashParticleData custom = ref particle.GetCustomData<FlashParticleData>();
        custom.Sigma = sigma;

        SafeSpawn(particle);
    }
}
