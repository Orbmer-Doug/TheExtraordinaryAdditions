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
    private readonly struct SnowflakeParticleDefinition
    {
        static SnowflakeParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Snowflake,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Snowflake),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    p.Velocity.X *= 0.96f;
                    p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + 0.12f, -12f, 7f);
                    p.Rotation += p.Velocity.Length() * 0.02f * p.Velocity.X.NonZeroSign();
                    p.Scale = p.Opacity = GetLerpBump(0f, 0.2f, 1f, 0.9f, p.LifetimeRatio);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Rectangle frame = new(0, 26 * p.Frame, 26, 26);
                    sb.DrawBetter(TypeDefinitions[(byte)ParticleTypes.Snowflake].Texture, p.Position, frame, Color.White * p.Opacity, p.Rotation, frame.Size() / 2, p.Scale, 0);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnSnowflakeParticle(Vector2 pos, Vector2 vel, int life, float scale)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = vel,
            Lifetime = life,
            Scale = scale / 2,
            Color = Color.White,
            Opacity = 1f,
            Type = ParticleTypes.Snowflake,
            Width = 2,
            Height = 2,
            Frame = Main.rand.Next(4),
            AllowedCollisions = CollisionTypes.Solid | CollisionTypes.Liquid
        };
        SafeSpawn(particle);
    }
}
