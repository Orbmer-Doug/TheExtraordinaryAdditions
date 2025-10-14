using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct MistParticleData
    {
        public float Spin;
        public Color Start;
        public Color End;
        public float Alpha;
    }

    private readonly struct MistParticleDefinition
    {
        static MistParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Mist,
                texture: AssetRegistry.GetTexture(AdditionsTexture.MistParticle),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref MistParticleData custom = ref p.GetCustomData<MistParticleData>();
                    p.Rotation += custom.Spin * (p.Velocity.X > 0).ToDirectionInt();
                    p.Velocity *= 0.9f;

                    if (custom.Alpha > 90f)
                    {
                        Lighting.AddLight(p.Position, p.Color.ToVector3() * 0.1f);
                        p.Scale += 0.01f;
                        custom.Alpha -= 3f;
                    }
                    else
                    {
                        p.Scale *= 0.975f;
                        custom.Alpha -= 2f;
                    }
                    if (custom.Alpha < 0f)
                        p.Time = p.Lifetime;

                    p.Color = Color.Lerp(custom.Start, custom.End, MathHelper.Clamp((255f - custom.Alpha - 100f) / 80f, 0f, 1f)) * (custom.Alpha / byte.MaxValue);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Mist].Texture;
                    Rectangle frame = texture.Frame(1, 3, 0, p.Frame);
                    sb.DrawBetter(texture, p.Position, frame, p.Color with { A = 0 }, p.Rotation, frame.Size() * .5f, p.Scale, p.Direction.ToSpriteDirection());
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnMistParticle(Vector2 position, Vector2 velocity, float scale, Color start, Color end, float alpha, float rotSpeed = 0f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Scale = scale,
            Lifetime = SecondsToFrames(2),
            Frame = Main.rand.Next(3),
            Type = ParticleTypes.Mist,
        };
        ref MistParticleData custom = ref particle.GetCustomData<MistParticleData>();
        custom.Start = start;
        custom.End = end;
        custom.Alpha = alpha;
        custom.Spin = rotSpeed;
        SafeSpawn(particle);
    }
}
