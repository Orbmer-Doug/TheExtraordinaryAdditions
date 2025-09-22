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
    private struct BloomPixelData
    {
        public Color BloomColor;
        public float BloomScale;
        public Vector2? HomeInDestination;
        public bool Gravity;
        public bool Intense;
        public byte TrailLength;
        public float VelMult; // For homing acceleration
    }

    private readonly struct BloomPixelParticleDefinition
    {
        static BloomPixelParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BloomPixel,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref BloomPixelData custom = ref p.GetCustomData<BloomPixelData>();

                    // Fading behavior
                    if (p.LifetimeRatio < 0.4f)
                    {
                        p.Opacity *= 0.91f;
                        p.Scale *= 0.96f;
                        p.Velocity *= 0.94f;
                    }
                    p.Rotation += p.Velocity.X * 0.07f;

                    // Homing logic
                    if (custom.HomeInDestination.HasValue)
                    {
                        Vector2 dest = custom.HomeInDestination.Value;
                        p.Velocity = Vector2.Lerp(p.Velocity, p.Position.SafeDirectionTo(dest) * custom.VelMult, 0.4f);
                        if (custom.VelMult < 26f)
                            custom.VelMult += 0.05f;
                        if (p.Position.WithinRange(dest, 10f))
                            p.Time = p.Lifetime;
                    }
                    else
                    {
                        if (custom.Gravity)
                            p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.3f, -10f, 18f);
                        else
                            p.Velocity *= 0.95f;
                    }
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref BloomPixelData custom = ref p.GetCustomData<BloomPixelData>();
                    Texture2D pixel = TypeDefinitions[(byte)ParticleTypes.SquishyPixel].Texture;
                    Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                    SpriteEffects direction = p.Direction.ToSpriteDirection();
                    Vector2 bloomOrigin = bloom.Size() / 2f;
                    Vector2 pixelOrigin = pixel.Size() / 2f;

                    if (custom.TrailLength > 0)
                    {
                        Span<Vector2> oldPos = p.OldPositions;
                        for (int i = 0; i < custom.TrailLength && i < oldPos.Length; i++)
                        {
                            float completion = 1f - InverseLerp(0f, custom.TrailLength, i);
                            if (custom.Intense)
                                sb.DrawBetter(bloom, p.Position, null, p.Color * p.Opacity * 0.55f, p.Rotation, bloomOrigin, custom.BloomScale * 0.15f, 0);
                            sb.DrawBetter(pixel, oldPos[i], null, p.Color * p.Opacity, p.Rotation, pixelOrigin, p.Scale * 6f * completion, direction);
                        }
                    }
                    else
                    {
                        sb.DrawBetter(bloom, p.Position, null, p.Color * p.Opacity * (custom.Intense ? 1.1f : 0.55f), p.Rotation, bloomOrigin, custom.BloomScale * (custom.Intense ? .25f : .15f), 0);
                        sb.DrawBetter(pixel, p.Position, null, p.Color * p.Opacity, p.Rotation, pixelOrigin, p.Scale * 6f, direction);
                    }
                },
                drawType: DrawTypes.Pixelize,
                onCollision: static (ref ParticleData p) =>
                {
                    ref BloomPixelData custom = ref p.GetCustomData<BloomPixelData>();
                    if (custom.Gravity)
                    {
                        Vector2 oldVel = p.OldVelocity;
                        if (Math.Abs(p.Velocity.X - oldVel.X) > float.Epsilon)
                            p.Velocity.X = -oldVel.X * 0.9f;
                        if (Math.Abs(p.Velocity.Y - oldVel.Y) > float.Epsilon)
                            p.Velocity.Y = -oldVel.Y * 0.9f;
                    }
                }
                ));
        }
    }

    public static void SpawnBloomPixelParticle(Vector2 position, Vector2 velocity, int lifetime, float scale,
Color color, Color bloomColor, Vector2? homeInDestination = null, float bloomScale = 1f,
byte trailLength = 0, bool gravity = false, bool intense = false)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = 1f,
            Type = ParticleTypes.BloomPixel,
            Width = 2,
            Height = 2,
            AllowedCollisions = gravity ? CollisionTypes.Solid | CollisionTypes.Player : CollisionTypes.None
        };
        ref BloomPixelData custom = ref particle.GetCustomData<BloomPixelData>();
        custom.BloomColor = bloomColor;
        custom.BloomScale = bloomScale;
        custom.HomeInDestination = homeInDestination;
        custom.Gravity = gravity;
        custom.Intense = intense;
        custom.TrailLength = Math.Min(trailLength, (byte)10); // Cap at OldPositions length
        custom.VelMult = velocity.Length();

        SafeSpawn(particle);
    }
}
