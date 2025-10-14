using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;


namespace TheExtraordinaryAdditions.Content.Rarities.Sparkles;

public class RarityHolosquare : Behavior<RarityParticleInfo>
{
    public override string TexturePath => AssetRegistry.GetTexturePath(AdditionsTexture.TechyHolosquare);
    public int Variant;
    public Rectangle TechFrame;
    public float Strength;
    public override bool UseAdditive => true;
    public RarityHolosquare(Vector2 pos, Vector2 vel, int life, float scale, Color color, float opacity = 1f, float strength = 1.5f)
    {
        Info.Position = pos;
        Info.Velocity = vel;
        Info.Lifetime = life;
        Info.Scale = scale;
        Info.DrawColor = color;
        Info.Opacity = opacity;
        Info.Rotation = RandomRotation();
        Strength = strength;

        Variant = Main.rand.Next(6);
        switch (Variant)
        {
            case 0:
                TechFrame = new Rectangle(8, 0, 6, 6);
                break;
            case 1:
                TechFrame = new Rectangle(6, 8, 10, 6);
                break;
            case 2:
                TechFrame = new Rectangle(4, 16, 14, 8);
                break;
            case 3:
                TechFrame = new Rectangle(2, 26, 18, 10);
                break;
            case 4:
                TechFrame = new Rectangle(2, 38, 18, 8);
                break;
            case 5:
                TechFrame = new Rectangle(6, 48, 12, 12);
                break;
        }
    }
    public override void Update()
    {
        if (Info.Time < 3f)
            Info.Velocity *= 1.2f;
        else
            Info.Velocity *= .975f;

        float completion = GetLerpBump(0f, .1f, 1f, .7f, Info.TimeRatio);
        Info.Scale = completion * Info.InitScale;
        Info.Opacity = completion * Strength;

        Info.Rotation = Info.Velocity.ToRotation();
    }
    public override void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line = null)
    {
        DrawChromaticAberration(Vector2.UnitX.RotatedBy(Info.Rotation, default), Strength, delegate (Vector2 offset, Color colorMod)
        {
            sb.Draw(Info.Texture, position + offset, (Rectangle?)TechFrame,
                Info.DrawColor.MultiplyRGB(colorMod) * Info.Opacity, Info.Rotation, TechFrame.Size() / 2f, new Vector2(Info.Scale, 1f), 0, 0f);
        });
    }
}