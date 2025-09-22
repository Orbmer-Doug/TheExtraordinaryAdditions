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
    private struct SmokeParticleData
    {
        public float Alpha;
        public float InitAlpha;
        public Color Start;
        public Color End;
    }

    private readonly struct SmokeParticleDefinition
    {
        static SmokeParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Smoke,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Invisible),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) => {  },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Smoke].Texture;
                    sb.DrawBetter(texture, p.Position, null, p.Color, p.Rotation, texture.Size() / 2, p.Scale, 0);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnSmokeParticle(Vector2 pos, Vector2 vel, float scale, Color start, Color end, float alpha)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = vel,
            Lifetime = SecondsToFrames(2),
            Scale = scale * 0.2f,
            Color = start,
            Opacity = 1f,
            Type = ParticleTypes.Smoke,
            Width = 2,
            Height = 2
        };
        ref SmokeParticleData custom = ref particle.GetCustomData<SmokeParticleData>();
        custom.Alpha = custom.InitAlpha = alpha;
        custom.Start = start;
        custom.End = end;
        SafeSpawn(particle);
    }
}
