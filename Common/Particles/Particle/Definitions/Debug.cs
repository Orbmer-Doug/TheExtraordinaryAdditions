using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private readonly struct DebugParticleDefinition
    {
        static DebugParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.Debug,
                texture: AssetRegistry.GetTexture(AdditionsTexture.Pixel),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    p.Rotation = p.Velocity.ToRotation();
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.Debug].Texture;
                    sb.DrawBetterRect(texture, ToTarget(p.Position, p.Scale, p.Scale), null, p.Color, p.Rotation, texture.Size() / 2);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnDebugParticle(Vector2 pos, Color? color = null, int scale = 5, int life = 10, Vector2? velocity = null)
    {
        ParticleData particle = new()
        {
            Position = pos,
            Velocity = velocity ?? Vector2.Zero,
            Lifetime = life,
            Scale = scale,
            Color = color ?? Color.Red,
            Opacity = 1f,
            Type = ParticleTypes.Debug,
            Width = 2,
            Height = 2
        };
        SafeSpawn(particle);
    }
}
