using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    private struct CartoonAngerData
    {
        public int RandomID;
        public Color StartingColor;
        public Color EndingColor;
    }

    private readonly struct CartoonAngerParticleDefinition
    {
        static CartoonAngerParticleDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.CartoonAnger,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CartoonAngerParticle),
                blendState: BlendState.Additive,
                update: static (ref ParticleData p) =>
                {
                    ref CartoonAngerData custom = ref p.GetCustomData<CartoonAngerData>();
                    float scaleFactor = MathHelper.Lerp(0.7f, 1.3f, Sin01(MathHelper.TwoPi * p.Time / 27f + custom.RandomID));
                    p.Scale = Utils.Remap(p.Time, 0f, 30f, 0.01f, p.Init.Scale * scaleFactor);
                    p.Color = Color.Lerp(custom.StartingColor, custom.EndingColor, p.TimeRatio);
                    p.Opacity = Animators.MakePoly(3).OutFunction(p.LifetimeRatio);
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.CartoonAnger].Texture;
                    sb.DrawBetter(texture, p.Position, null, p.Color * p.Opacity, p.Rotation, texture.Size() / 2f, p.Scale, 0);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnCartoonAngerParticle(Vector2 position, int lifetime, float scale, float rotation, Color startingColor, Color endingColor)
    {
        ParticleData particle = new()
        {
            Position = position,
            Lifetime = lifetime,
            Scale = scale,
            Rotation = rotation,
            Color = startingColor,
            Opacity = 1f,
            Type = ParticleTypes.CartoonAnger,
            Width = 2,
            Height = 2
        };
        ref CartoonAngerData custom = ref particle.GetCustomData<CartoonAngerData>();
        custom.RandomID = Main.rand.Next(1000);
        custom.StartingColor = startingColor;
        custom.EndingColor = endingColor;
        SafeSpawn(particle);
    }
}
