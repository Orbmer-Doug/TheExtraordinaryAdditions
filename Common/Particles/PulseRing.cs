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
    private struct PulseRingData
    {
        public Vector2 Squish;
        public Color BaseColor;
        public bool UseAltTexture;
        public float OriginalScale;
        public float FinalScale;
    }

    private readonly struct PulseRingParticleDefinition
    {
        static PulseRingParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.PulseRing,
                texture: AssetRegistry.GetTexture(AdditionsTexture.HollowCircleHighRes),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref PulseRingData custom = ref p.GetCustomData<PulseRingData>();
                    p.Scale = Animators.MakePoly(4).OutFunction.Evaluate(custom.OriginalScale, custom.FinalScale, p.TimeRatio);
                    p.Opacity = (float)Math.Sin(MathHelper.PiOver2 + p.TimeRatio * MathHelper.PiOver2);
                    p.Color = custom.BaseColor * p.Opacity;
                    Lighting.AddLight(p.Position, p.Color.ToVector3() * InverseLerp(0f, custom.FinalScale, p.Scale));
                    p.Velocity *= 0.95f;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref PulseRingData custom = ref p.GetCustomData<PulseRingData>();
                    Texture2D tex = custom.UseAltTexture ? AssetRegistry.GetTexture(AdditionsTexture.HollowCircleFancy) : TypeDefinitions[(byte)ParticleTypes.PulseRing].Texture;
                    sb.DrawBetterRect(tex, ToTarget(p.Position, p.Scale * custom.Squish), null, p.Color, p.Rotation, tex.Size() / 2f);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnPulseRingParticle(Vector2 position, Vector2 velocity, int lifetime, float rot, Vector2 squish, float startScale, float endScale, Color color, bool altTex = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = startScale,
            Color = color,
            Opacity = 1f,
            Rotation = rot,
            Type = ParticleTypes.PulseRing,
            Width = 2,
            Height = 2
        };
        ref PulseRingData custom = ref particle.GetCustomData<PulseRingData>();
        custom.Squish = squish;
        custom.BaseColor = color;
        custom.UseAltTexture = altTex;
        custom.OriginalScale = startScale;
        custom.FinalScale = endScale;
        SafeSpawn(particle);
    }
}
