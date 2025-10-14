using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct SquishyPixelData
    {
        public Color BloomColor;
        public bool Gravity;
        public float Rot;
        public byte TrailLength;
    }

    private readonly struct SquishyPixelParticleDefinition
    {
        static SquishyPixelParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.SquishyPixel,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref SquishyPixelData custom = ref p.GetCustomData<SquishyPixelData>();
                    p.Rotation += p.Velocity.X * 0.07f;
                    p.Scale = (1f - Animators.MakePoly(5).OutFunction(p.TimeRatio)) * p.Init.Scale;
                    p.Opacity = MathF.Pow(p.LifetimeRatio, 2.5f);
                    p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                    if (custom.Gravity && p.Time > 10f)
                        p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.3f, -30f, 28f);
                    p.Velocity *= 0.96f;
                    p.Velocity = p.Velocity.RotatedBy(custom.Rot);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.SquishyPixel].Texture;
                    ref SquishyPixelData custom = ref p.GetCustomData<SquishyPixelData>();
                    Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                    Vector2 orig = texture.Size() / 2;
                    float squish = MathHelper.Clamp(p.Velocity.Length() / 5f, 1f, 2f);
                    Vector2 scale = new Vector2(p.Scale - p.Scale * squish * 0.3f, p.Scale * squish) * 0.6f;
                    if (custom.TrailLength > 0)
                    {
                        Span<Vector2> oldPos = p.OldPositions;
                        for (int i = 0; i < custom.TrailLength && i < oldPos.Length; i++)
                        {
                            Vector2 old = oldPos[i];
                            float completion = 1f - InverseLerp(0f, custom.TrailLength, i);
                            sb.DrawBetter(bloom, old, null, p.Color * p.Opacity * completion, p.Rotation, bloom.Size() / 2, scale * 0.14f * completion, 0);
                            sb.DrawBetter(texture, old, null, p.Color * p.Opacity, p.Rotation, orig, scale * 7 * completion, 0);
                        }
                    }
                    else
                    {
                        sb.DrawBetter(bloom, p.Position, null, p.Color * p.Opacity, p.Rotation, bloom.Size() / 2, scale * 0.14f, 0);
                        sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity, p.Rotation, orig, scale * 7, 0);
                    }
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnSquishyPixelParticle(Vector2 pos, Vector2 vel, int life, float scale, Color col, Color bloomCol, byte trailLength = 0, bool collide = false, bool fall = false, float velRot = 0f)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = vel,
            Lifetime = life,
            Scale = scale,
            Color = col,
            Opacity = 1f,
            Type = ParticleTypes.SquishyPixel,
            Width = 2,
            Height = 2,
            AllowedCollisions = collide ? CollisionTypes.Solid : CollisionTypes.None
        };
        ref SquishyPixelData custom = ref particle.GetCustomData<SquishyPixelData>();
        custom.BloomColor = bloomCol;
        custom.Gravity = fall;
        custom.Rot = velRot;
        custom.TrailLength = Math.Min(trailLength, (byte)10);
        SafeSpawn(particle);
    }
}
