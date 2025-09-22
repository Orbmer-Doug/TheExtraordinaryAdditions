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
    private readonly struct BloodStreakParticleDefinition
    {
        static BloodStreakParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BloodStreak,
                texture: AssetRegistry.GetTexture(AdditionsTexture.BloodParticle),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    p.Velocity *= 0.95f;
                    p.Opacity = Animators.MakePoly(4).OutFunction(p.LifetimeRatio);
                    p.Rotation = p.Velocity.ToRotation();
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.BloodStreak].Texture;
                    float brightness = MathF.Pow(Lighting.Brightness(p.Position.ToTileCoordinates().X, p.Position.ToTileCoordinates().Y), 0.15f);
                    Rectangle frame = texture.Frame(1, 3, 0, (int)(p.LifetimeRatio * 3f));
                    Vector2 origin = frame.Size() * 0.5f;
                    sb.DrawBetter(texture, p.Position, frame, p.Color * brightness * p.Opacity, p.Rotation, origin, p.Scale, 0);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnBloodStreakParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.BloodStreak,
            Width = 2,
            Height = 2
        };
        SafeSpawn(particle);
    }
}
