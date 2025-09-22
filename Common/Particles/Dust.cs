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
    private struct DustData
    {
        public float Spin;
        public bool Glowing;
        public bool Gravity;
        public bool Wavy;
        public float Timer;
        public float Delay;
    }

    private readonly struct DustParticleDefinition
    {
        static DustParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Dust,
                texture: AssetRegistry.GetTexture(AdditionsTexture.DustParticle),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref DustData custom = ref p.GetCustomData<DustData>();

                    if (custom.Glowing)
                        Lighting.AddLight(p.Position, p.Color.ToVector3() * .5f * p.Opacity);

                    if (custom.Gravity && p.Time > 20f)
                        p.Velocity.Y = MathHelper.Clamp(p.Velocity.Y + .2f, -40f, 22f);

                    if (custom.Wavy)
                        p.Velocity = p.Init.Velocity.VelEqualTrig(MathF.Cos, 24f, .5f, ref custom.Delay, ref custom.Timer);

                    p.Velocity *= 0.98f;
                    p.Rotation += custom.Spin * p.Velocity.X.NonZeroSign();

                    p.Scale = Animators.MakePoly(3).OutFunction(p.LifetimeRatio) * p.Init.Scale;
                    p.Opacity = GetLerpBump(0f, .1f, 1f, .6f, p.TimeRatio);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref DustData custom = ref p.GetCustomData<DustData>();
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Dust].Texture;
                    Rectangle frame = new(12 * p.Frame, 0, 12, 10);
                    Vector2 orig = frame.Size() / 2f;
                    sb.DrawBetter(texture, p.Position, frame, p.Color * p.Opacity, p.Rotation, orig, p.Scale);
                    if (custom.Glowing)
                    {
                        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.GlowHarsh);
                        sb.DrawBetterRect(glow, ToTarget(p.Position, p.Scale * texture.Width, p.Scale * texture.Height), null, p.Color * p.Opacity * .5f, p.Rotation, glow.Size() / 2);
                    }
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnDustParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float rotationspeed = .1f, bool fall = false, bool glowing = false, bool wavy = false, bool collide = true)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Type = ParticleTypes.Dust,
            Rotation = RandomRotation(),
            Frame = Main.rand.Next(6),
            Width = 2,
            Height = 2,
        };
        if (collide)
            particle.AllowedCollisions = CollisionTypes.Solid | CollisionTypes.Liquid;

        ref DustData custom = ref particle.GetCustomData<DustData>();
        custom.Spin = rotationspeed;
        custom.Glowing = glowing;
        custom.Gravity = fall;
        custom.Wavy = wavy;

        if (glowing)
            particle.SetBlendState(BlendState.Additive);

        SafeSpawn(particle);
    }
}
