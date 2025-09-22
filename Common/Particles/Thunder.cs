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
    private struct ThunderParticleData
    {
        public Vector2 Squish;
        public float ShakePower;
    }

    private readonly struct ThunderParticleDefinition
    {
        static ThunderParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Thunder,
                texture: AssetRegistry.GetTexture(AdditionsTexture.ThunderBolt),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref ThunderParticleData custom = ref p.GetCustomData<ThunderParticleData>();
                    p.LockOnDetails?.Apply(ref p.Position);
                    Lighting.AddLight(p.Position, p.Color.ToVector3() * 3f);
                    float fade = Animators.MakePoly(3f).InFunction(p.LifetimeRatio);
                    p.Opacity = fade * p.Init.Opacity;
                    custom.Squish.X = fade;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Thunder].Texture;
                    ref ThunderParticleData custom = ref p.GetCustomData<ThunderParticleData>();
                    Vector2 shake = Vector2.One.RotatedByRandom(MathHelper.TwoPi) * p.LifetimeRatio * custom.ShakePower;
                    Vector2 origin = new(texture.Width / 2f, texture.Height);
                    Color drawColor = Color.Lerp(Color.White, p.Color, p.Time / (float)p.Lifetime);
                    SpriteEffects flip = Main.GlobalTimeWrappedHourly % 30f < 15f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                    sb.DrawBetter(texture, p.Position + shake, null, p.Color * p.Opacity * 0.6f, p.Rotation, origin, custom.Squish * p.Scale, flip);
                    sb.DrawBetter(texture, p.Position, null, drawColor * p.Opacity, p.Rotation, origin, custom.Squish * p.Scale, flip);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnThunderParticle(Vector2 position, int lifetime, float scale, Vector2 squish, float rotation, Color color, float opacity = 1f, float shakePower = 20f, LockOnDetails? lockOn = null)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Rotation = rotation,
            Type = ParticleTypes.Thunder,
            Width = 2,
            Height = 2,
            LockOnDetails = lockOn,
        };
        ref ThunderParticleData custom = ref particle.GetCustomData<ThunderParticleData>();
        custom.Squish = squish;
        custom.ShakePower = shakePower;
        SafeSpawn(particle);
    }
}
