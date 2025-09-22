using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct ShockwaveParticleData
    {
        public float Frequency;
        public float Chromatic;
        public float RingSize;
        public float MaxSize;
    }

    private readonly struct ShockwaveParticleDefinition
    {
        static ShockwaveParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Shockwave,
                texture: AssetRegistry.InvisTex,
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref ShockwaveParticleData custom = ref p.GetCustomData<ShockwaveParticleData>();
                    p.Opacity = Convert01To010(p.TimeRatio);
                    p.Scale = Animators.MakePoly(2.2f).InOutFunction.Evaluate(0f, custom.MaxSize, p.TimeRatio);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) => { },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnShockwaveParticle(Vector2 pos, int life, float frequency, float radius, float ringSize, float aberration = 0.2f, Vector2? velocity = null)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = velocity ?? Vector2.Zero,
            Lifetime = life,
            Scale = 0f,
            Type = ParticleTypes.Shockwave,
            Width = 2,
            Height = 2
        };
        ref ShockwaveParticleData custom = ref particle.GetCustomData<ShockwaveParticleData>();
        custom.Frequency = frequency;
        custom.Chromatic = aberration;
        custom.RingSize = ringSize / Main.ScreenSize.X;
        custom.MaxSize = radius;

        SafeSpawn(particle);
    }
}
