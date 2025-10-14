using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct SparkParticleData
    {
        public Vector2? HomeInDestination;
        public bool Gravity;
    }

    private readonly struct SparkParticleDefinition
    {
        static SparkParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Spark,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Gleam),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref SparkParticleData custom = ref p.GetCustomData<SparkParticleData>();
                    if (custom.HomeInDestination.HasValue)
                    {
                        Vector2 dest = custom.HomeInDestination.Value;
                        float currentDirection = p.Velocity.ToRotation();
                        float idealDirection = (dest - p.Position).ToRotation();
                        p.Velocity = currentDirection.AngleLerp(idealDirection, 0.03f).ToRotationVector2() * p.Velocity.Length();
                        p.Velocity += (dest - p.Position) * 0.005f;
                        if (p.Position.WithinRange(dest, 10f))
                            p.Time = p.Lifetime;
                    }
                    else
                    {
                        p.Velocity *= 0.94f;
                        if (p.Velocity.Length() < 12f && custom.Gravity)
                        {
                            p.Velocity.X *= 0.94f;
                            p.Velocity.Y += 0.25f;
                        }
                    }
                    p.Scale = p.LifetimeRatio * p.Init.Scale;
                    p.Opacity = MathHelper.SmoothStep(1, 0, p.TimeRatio) * p.Init.Opacity;
                    p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Spark].Texture;
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity * 0.15f, p.Rotation, texture.Size() / 2, new Vector2(0.5f, 1.4f) * p.Scale * 2f, 0);
                    sb.DrawBetter(texture, p.Position, null, Color.Lerp(Color.White, p.Color, 0.2f) * p.Opacity * 0.5f, p.Rotation, texture.Size() / 2, new Vector2(0.4f, 1.2f) * p.Scale * 1.5f, 0);
                    sb.DrawBetter(texture, p.Position, null, Color.Lerp(Color.White, p.Color, 0.4f) * p.Opacity, p.Rotation, texture.Size() / 2, new Vector2(0.3f, 1f) * p.Scale, 0);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnSparkParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, bool gravity = false, bool collide = false, Vector2? homeInDest = null)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.Spark,
            Width = 2,
            Height = 2,
            AllowedCollisions = collide ? CollisionTypes.Solid : CollisionTypes.None
        };
        ref SparkParticleData custom = ref particle.GetCustomData<SparkParticleData>();
        custom.HomeInDestination = homeInDest;
        custom.Gravity = gravity;
        SafeSpawn(particle);
    }
}
