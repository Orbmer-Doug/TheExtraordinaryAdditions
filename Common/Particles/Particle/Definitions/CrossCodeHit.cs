using Microsoft.Xna.Framework.Graphics;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

namespace TheExtraordinaryAdditions.Common.Particles;

public partial class ParticleRegistry
{
    public enum CrosscodeHitType
    {
        Small,
        Medium,
        Big
    }

    private struct CrosscodeHitData
    {
        public CrosscodeHitType Type;
        public CrossDiscHoldout.Element Element;
    }

    private readonly struct CrossCodeHitDefinition
    {
        static CrossCodeHitDefinition()
        {
            RegisterDefinition(new ParticleDefinition(
                type: ParticleTypes.CrossCodeHit,
                texture: AssetRegistry.GetTexture(AdditionsTexture.CrossCodeHit),
                blendState: BlendState.AlphaBlend,
                update: static (ref ParticleData p) =>
                {
                    ref CrosscodeHitData data = ref p.GetCustomData<CrosscodeHitData>();
                    int maxFrames = 0;

                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 6;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 5;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 7;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 7;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 6;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Medium:
                                    maxFrames = 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    maxFrames = 7;
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
                    ref CrosscodeHitData data = ref p.GetCustomData<CrosscodeHitData>();
                    Texture2D texture = TypeDefinitions[(byte)ParticleTypes.CrossCodeHit].Texture;

                    int x = 128 * p.Frame;
                    int y = 0;

                    switch (data.Element)
                    {
                        case CrossDiscHoldout.Element.Neutral:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 2;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Cold:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 9;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 10;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 11;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Heat:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 3;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 4;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 5;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Shock:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 6;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 7;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 8;
                                    break;
                            }
                            break;
                        case CrossDiscHoldout.Element.Wave:
                            switch (data.Type)
                            {
                                case CrosscodeHitType.Small:
                                    y = 128 * 12;
                                    break;
                                case CrosscodeHitType.Medium:
                                    y = 128 * 13;
                                    break;
                                case CrosscodeHitType.Big:
                                    y = 128 * 14;
                                    break;
                            }
                            break;
                    }

                    sb.DrawBetter(texture, p.Position, new Rectangle(x, y, 128, 128), p.Color, p.Rotation, new(64), 1f);
                },
                drawType: DrawTypes.Manual
                ));
        }
    }

    public static void SpawnCrossCodeHit(Vector2 position, CrosscodeHitType type, CrossDiscHoldout.Element element)
    {
        ParticleData particle = new()
        {
            Position = position,
            Velocity = Vector2.Zero,
            Rotation = RandomRotation(),
            Lifetime = 400,
            Scale = 1f,
            Color = Color.White,
            Type = ParticleTypes.CrossCodeHit,
        };
        ref CrosscodeHitData data = ref particle.GetCustomData<CrosscodeHitData>();
        data.Element = element;
        data.Type = type;

        SafeSpawn(particle);
    }
}
