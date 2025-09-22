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
    private readonly struct BloomLineParticleDefinition
    {
        static BloomLineParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BloomLine,
                texture: AssetRegistry.GetTexture(AdditionsTexture.BloomLineSmall),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    p.Scale = p.LifetimeRatio * p.Init.Scale;
                    p.Opacity = Animators.Circ.OutFunction(p.LifetimeRatio);
                    p.Velocity *= 0.95f;
                    p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.BloomLine].Texture;
                    Vector2 scale = new Vector2(0.5f, 1.6f) * p.Scale;
                    for (float i = .25f; i < 1f; i += .25f)
                    {
                        sb.DrawBetter(texture, p.Position, null, p.Color, p.Rotation, texture.Size() / 2, scale * i, 0);
                        sb.DrawBetter(texture, p.Position, null, p.Color, p.Rotation, texture.Size() / 2, scale * new Vector2(0.45f, 1f) * i, 0);
                    }
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnBloomLineParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.BloomLine,
        };
        SafeSpawn(particle);
    }
}
