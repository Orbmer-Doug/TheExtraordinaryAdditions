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
    private readonly struct BloodParticleDefinition
    {
        static BloodParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Blood,
                texture: AssetRegistry.GetTexture(AdditionsTexture.BloodParticle2),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    p.Scale = MathF.Pow(MathHelper.SmoothStep(1, 0, p.TimeRatio), .2f) * p.Init.Scale;
                    p.Velocity.X *= 0.97f;
                    p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.8f, -22f, 22f);
                    p.Opacity = -MathF.Pow(p.LifetimeRatio, 6f) + 1f;
                    p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Blood].Texture;
                    float verticalStretch = Utils.GetLerpValue(0f, 24f, Math.Abs(p.Velocity.Y), clamped: true) * 0.84f;
                    float brightness = MathF.Pow(Lighting.Brightness((int)(p.Position.X / 16f), (int)(p.Position.Y / 16f)), 0.15f);
                    Vector2 scale = new Vector2(1f, verticalStretch + 1f) * p.Scale * 0.1f;
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity * brightness, p.Rotation, texture.Size() / 2, scale, 0);
                },
                drawType: DrawTypes.Manual,
                onCollision: static (ref ParticleData p) => p.Time = p.Lifetime
            ));
        }
    }

    public static void SpawnBloodParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.Blood,
            AllowedCollisions = CollisionTypes.Solid | CollisionTypes.Liquid | CollisionTypes.Player | CollisionTypes.NPC,
        };
        SafeSpawn(particle);
    }
}
