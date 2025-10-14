using Microsoft.Xna.Framework.Graphics;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    public enum CrosscodeBollType
    {
        DieWallSmall,
        Die,
        DieWallBig,
        Trail,
    }

    private struct CrosscodeBollData
    {
        public CrosscodeBollType Type;
        public CrossDiscHoldout.Element Element;
    }

    private readonly struct CrossCodeBollDefinition
    {
        static CrossCodeBollDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.CrossCodeBoll,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CrossCodeBoll),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref CrosscodeBollData data = ref p.GetCustomData<CrosscodeBollData>();
                    int maxFrames = 0;

                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 4;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 7;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 5;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 6;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 4;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    maxFrames = 5;
                                    break;
                                case CrosscodeBollType.Die:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    maxFrames = 6;
                                    break;
                                case CrosscodeBollType.Trail:
                                    maxFrames = 4;
                                    break;
                            }
                            break;
                    }

                    if (p.Time % 4 == 3)
                        p.Frame++;
                    if (p.Frame >= maxFrames)
                        p.Time = p.Lifetime;
                },
                draw: static (ref ParticleData p, SpriteBatch sb) =>
                {
                    ref CrosscodeBollData data = ref p.GetCustomData<CrosscodeBollData>();
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.CrossCodeBoll].Texture;

                    int x = 48 * p.Frame;
                    int y = 0;
                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 2;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 7;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 8;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 9;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 10;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 3;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 4;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 5;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 6;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 11;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 12;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 13;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 14;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeBollType.DieWallSmall:
                                    y = 48 * 15;
                                    break;
                                case CrosscodeBollType.Die:
                                    y = 48 * 16;
                                    break;
                                case CrosscodeBollType.DieWallBig:
                                    y = 48 * 17;
                                    break;
                                case CrosscodeBollType.Trail:
                                    y = 48 * 18;
                                    break;
                            }
                            break;
                    }

                    sb.DrawBetter(texture, p.Position, new Rectangle(x, y, 48, 48), p.Color, p.Rotation, new(24), 1f);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnCrossCodeBoll(Vector2 position, float rotation, CrosscodeBollType type, CrossDiscHoldout.Element element)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Rotation = rotation,
            Lifetime = 400,
            Scale = 1f,
            Color = Color.White,
            Type = ParticleTypes.CrossCodeBoll,
        };
        ref CrosscodeBollData data = ref particle.GetCustomData<CrosscodeBollData>();
        data.Element = element;
        data.Type = type;

        SafeSpawn(particle);
    }
}
