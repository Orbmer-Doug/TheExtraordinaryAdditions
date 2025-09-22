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
    private struct HeavySmokeData
    {
        public bool Glowing;
        public float Spin;
    }

    private readonly struct HeavySmokeParticleDefinition
    {
        static HeavySmokeParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.HeavySmoke,
                texture: AssetRegistry.GetTexture(AdditionsTexture.HeavySmoke),
                blendState: BlendState.NonPremultiplied,
                update: static (ref ParticleData p) =>
                {
                    ref HeavySmokeData custom = ref p.GetCustomData<HeavySmokeData>();

                    if (custom.Glowing)
                        Lighting.AddLight(p.Position, p.Color.ToVector3() * .5f * p.Opacity);

                    p.Opacity = MathF.Sin(MathHelper.PiOver2 + p.TimeRatio * MathHelper.PiOver2);
                    p.Scale = p.LifetimeRatio * p.Init.Scale;

                    p.Rotation += custom.Spin * p.Velocity.X.NonZeroSign();
                    p.Velocity *= 0.98f;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.HeavySmoke].Texture;
                    int timeFrame = (int)Math.Floor(p.Time / (p.Lifetime / 6f));
                    Rectangle frame = new(p.Frame * 80, timeFrame * 80, 80, 80);
                    SpriteEffects visualDirection = p.Direction.ToSpriteDirection();
                    sb.DrawBetter(texture, p.Position, frame, p.Color * p.Opacity, p.Rotation, frame.Size() * 0.5f, p.Scale, visualDirection);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnHeavySmokeParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, bool glowing = true, float spinSpeed = 0.05f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Type = ParticleTypes.HeavySmoke,
            Rotation = RandomRotation(),
            Frame = Main.rand.Next(7),
        };
        ref HeavySmokeData custom = ref particle.GetCustomData<HeavySmokeData>();
        custom.Spin = spinSpeed;
        custom.Glowing = glowing;
        if (glowing)
            particle.SetBlendState(BlendState.Additive);
        SafeSpawn(particle);
    }
}
