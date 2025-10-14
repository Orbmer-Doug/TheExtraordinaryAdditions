using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct BulletCasingParticleData
    {
        public float RotAmt;
    }

    private readonly struct BulletCasingParticleDefinition
    {
        static BulletCasingParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.BulletCasing,
                texture: AssetRegistry.GetTexture(AdditionsTexture.AntiBulletShell),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref BulletCasingParticleData custom = ref p.GetCustomData<BulletCasingParticleData>();
                    p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + .2f, -22f, 22f);
                    float HeatInterpolant = 1f - InverseLerp(0f, 100f, p.Time);
                    Color HeatColor = Color.Lerp(Color.Chocolate * .9f, Color.Chocolate * 2f, HeatInterpolant);

                    p.Rotation += p.Velocity.Length() * custom.RotAmt;
                    p.Opacity = InverseLerp(p.Lifetime, p.Lifetime - 20f, p.Time);
                    Lighting.AddLight(p.Position, HeatColor.ToVector3() * HeatInterpolant);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.BulletCasing].Texture;
                    sb.DrawBetter(texture, p.Position, null, Lighting.GetColor(p.Position.ToTileCoordinates()) * p.Opacity, p.Rotation, texture.Size() / 2, p.Scale, 0);

                    float HeatInterpolant = 1f - InverseLerp(0f, 100f, p.Time);
                    Color HeatColor = Color.Lerp(Color.Chocolate * .9f, Color.Chocolate * 2f, HeatInterpolant);
                    if (HeatInterpolant > 0f)
                    {
                        const int amt = 20;
                        for (int i = 0; i < amt; i++)
                        {
                            Vector2 offset = (MathHelper.TwoPi * i / amt).ToRotationVector2() * (HeatInterpolant * 3f) - Main.screenPosition;
                            sb.DrawBetter(texture, p.Position + offset, null, HeatColor with { A = 40 } * HeatInterpolant * .95f, p.Rotation, texture.Size() / 2, p.Scale, 0);
                        }
                    }
                },
                drawType: DrawTypes.Manual,
                onCollision: static (ref ParticleData p) =>
                {
                    if (Math.Abs(p.Velocity.X - p.OldVelocity.X) > float.Epsilon)
                        p.Velocity.X = -p.OldVelocity.X * .5f;
                    if (Math.Abs(p.Velocity.Y - p.OldVelocity.Y) > float.Epsilon)
                        p.Velocity.Y = -p.OldVelocity.Y * .5f;

                    p.Velocity.X *= .85f;
                },
                onSpawn: static (ref ParticleData p) =>
                {
                    ref BulletCasingParticleData custom = ref p.GetCustomData<BulletCasingParticleData>();
                    custom.RotAmt = Main.rand.NextFloat(.01f, .03f);
                }
                ));
        }
    }

    public static void SpawnBulletCasingParticle(Vector2 position, Vector2 velocity, float scale)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = SecondsToFrames(5),
            Scale = scale,
            Color = Color.White,
            Width = 20,
            Height = 20,
            Opacity = 1f,
            AllowedCollisions = CollisionTypes.Solid,
            Type = ParticleTypes.BulletCasing,
        };
        SafeSpawn(particle);
    }
}
