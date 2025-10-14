using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Light;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct GlowParticleData
    {
        public Vector2? HomeInDestination;
        public bool Gravity;
    }

    private readonly struct GlowParticleDefinition
    {
        static GlowParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Glow,
                texture: AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref GlowParticleData custom = ref p.GetCustomData<GlowParticleData>();
                    if (custom.HomeInDestination == null)
                    {
                        if (custom.Gravity && p.Velocity.Length() < 12f)
                            p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.2f, -22f, 22f);
                        p.Velocity *= 0.96f;
                    }
                    else
                    {
                        p.Velocity = Vector2.Lerp(p.Velocity, Vector2.Normalize(custom.HomeInDestination.Value - p.Position), 0.3f);
                        if (Vector2.DistanceSquared(p.Position, custom.HomeInDestination.Value) < 10f * 10f)
                            p.Time = p.Lifetime;
                    }
                    p.Opacity = MathF.Pow(p.LifetimeRatio, 2) * p.Init.Opacity;
                    p.Scale = p.LifetimeRatio * p.Init.Scale;

                    float hitboxTiles = p.Scale / 16f;
                    float desiredRadius = hitboxTiles / 2f;
                    float intensity = CalculateIntensityForRadius(desiredRadius, LightMaskMode.None, 0.5f);
                    Vector3 lightColor = p.Color.ToVector3() * intensity * p.Opacity;
                    Lighting.AddLight(p.Position, lightColor);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Glow].Texture;
                    Vector2 orig = texture.Size() / 2f;
                    sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color * p.Opacity * .3f, p.Rotation, orig);
                    sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color.Lerp(Color.White, .25f) * p.Opacity * .6f, p.Rotation, orig);
                    sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color.Lerp(Color.White, .4f) * p.Opacity, p.Rotation, orig);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    /// <param name="scale">In pixels</param>
    public static void SpawnGlowParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, bool gravity = false, Vector2? homeInDest = null, bool collide = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Type = ParticleTypes.Glow,
            Width = 2,
            Height = 2,
            AllowedCollisions = collide ? CollisionTypes.NPC | CollisionTypes.Solid : CollisionTypes.None,
        };
        ref GlowParticleData custom = ref particle.GetCustomData<GlowParticleData>();
        custom.Gravity = gravity;
        custom.HomeInDestination = homeInDest;
        SafeSpawn(particle);
    }
}
