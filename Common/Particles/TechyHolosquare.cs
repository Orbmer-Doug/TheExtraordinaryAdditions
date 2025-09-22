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
    private struct TechyHolosquareParticleData
    {
        public Rectangle TechFrame;
        public int Variant;
        public float Strength;
    }

    private readonly struct TechyHolosquareParticleDefinition
    {
        static TechyHolosquareParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.TechyHolosquare,
                texture: AssetRegistry.GetTexture(AdditionsTexture.TechyHolosquare),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref TechyHolosquareParticleData custom = ref p.GetCustomData<TechyHolosquareParticleData>();
                    p.Opacity = (float)Math.Pow(p.LifetimeRatio, 0.5) * custom.Strength;
                    Lighting.AddLight(p.Position, p.Color.ToVector3() * p.Opacity);
                    p.Rotation = p.Velocity.ToRotation();
                    p.Velocity *= 0.875f;
                    p.Scale *= 0.96f;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref TechyHolosquareParticleData custom = ref p.GetCustomData<TechyHolosquareParticleData>();
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.TechyHolosquare].Texture;

                    for (int i = -1; i <= 1; i++)
                    {
                        Color aberrationColor = Color.White;
                        switch (i)
                        {
                            case -1:
                                aberrationColor = new Color(255, 0, 0, 0);
                                break;
                            case 0:
                                aberrationColor = new Color(0, 255, 0, 0);
                                break;
                            case 1:
                                aberrationColor = new Color(0, 0, 255, 0);
                                break;
                        }
                        Vector2 offset = Utils.RotatedBy(PolarVector(1f, p.Rotation), MathHelper.PiOver2, default) * i;
                        offset *= custom.Strength;
                        sb.DrawBetter(texture, p.Position + offset, custom.TechFrame, p.Color.MultiplyRGB(aberrationColor) * p.Opacity, p.Rotation, custom.TechFrame.Size() / 2f, p.Scale, 0);
                    }
                    sb.DrawBetter(texture, p.Position, custom.TechFrame, p.Color * p.Opacity, p.Rotation, custom.TechFrame.Size() / 2f, p.Scale, 0);
                },
                drawType: DrawTypes.Pixelize
                ));
        }
    }

    public static void SpawnTechyHolosquareParticle(Vector2 position, Vector2 velocity, int lifetime, float scale, Color color, float opacity = 1f, float strength = 1.4f)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = velocity,
            Lifetime = lifetime,
            Scale = scale,
            Color = color,
            Opacity = opacity,
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
            Type = ParticleTypes.TechyHolosquare,
            Width = 2,
            Height = 2
        };
        ref TechyHolosquareParticleData custom = ref particle.GetCustomData<TechyHolosquareParticleData>();
        custom.Variant = Main.rand.Next(6);
        custom.TechFrame = custom.Variant switch
        {
            0 => new Rectangle(8, 0, 6, 6),
            1 => new Rectangle(6, 8, 10, 6),
            2 => new Rectangle(4, 16, 14, 8),
            3 => new Rectangle(2, 26, 18, 10),
            4 => new Rectangle(2, 38, 18, 8),
            5 => new Rectangle(6, 48, 12, 12),
            _ => new Rectangle(0, 0, 0, 0)
        };
        custom.Strength = strength;
        SafeSpawn(particle);
    }
}
