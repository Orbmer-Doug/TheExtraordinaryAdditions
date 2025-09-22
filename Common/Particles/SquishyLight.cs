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
    private struct SquishyLightParticleData
    {
        public float SquishStrength;
        public float MaxSquish;
    }

    private readonly struct SquishyLightParticleDefinition
    {
        static SquishyLightParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.SquishyLight,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Light),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    p.Velocity *= p.LifetimeRatio >= 0.34f ? 0.93f : 1.02f;
                    p.Opacity = p.LifetimeRatio > 0.5f ? Convert01To010(p.LifetimeRatio) * 0.2f + 0.8f : Convert01To010(p.LifetimeRatio);
                    p.Scale = Animators.MakePoly(4).OutFunction(p.LifetimeRatio * p.Init.Scale) * 0.5f;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.SquishyLight].Texture;
                    ref SquishyLightParticleData custom = ref p.GetCustomData<SquishyLightParticleData>();
                    float squish = MathHelper.Clamp(p.Velocity.Length() / 10f * custom.SquishStrength, 1f, custom.MaxSquish);
                    float rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                    Vector2 scale = new Vector2(p.Scale - p.Scale * squish * 0.3f, p.Scale * squish) * 0.6f;
                    float properBloomSize = texture.Height / (float)AssetRegistry.GetTexture(AdditionsTexture.GlowSoft).Height;
                    sb.DrawBetter(AssetRegistry.GetTexture(AdditionsTexture.GlowSoft), p.Position, null, p.Color * p.Opacity * 0.8f, rotation, AssetRegistry.GetTexture(AdditionsTexture.GlowSoft).Size() / 2f, scale * 2f * properBloomSize, 0);
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity * 0.8f, rotation, texture.Size() / 2, scale * 1.1f, 0);
                    sb.DrawBetter(texture, p.Position, null, Color.White * p.Opacity * 0.9f, rotation, texture.Size() / 2, scale, 0);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnSquishyLightParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, float squishPower = 1f, float maxSquish = 3f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Type = ParticleTypes.SquishyLight,
            Width = 2,
            Height = 2
        };
        ref SquishyLightParticleData custom = ref particle.GetCustomData<SquishyLightParticleData>();
        custom.SquishStrength = squishPower;
        custom.MaxSquish = maxSquish;
        SafeSpawn(particle);
    }
}
