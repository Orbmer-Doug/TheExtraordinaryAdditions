using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;

namespace TheExtraordinaryAdditions.Content.Rarities.Sparkles;

public class RaritySparkle : Behavior<RarityParticleInfo>
{
    public override string TexturePath => AssetRegistry.GetTexturePath(AdditionsTexture.CritSpark);
    public static readonly Texture2D Bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
    public RaritySparkle(Vector2 pos, Vector2 vel, int life, float scale, Color col)
    {
        Info.Position = pos;
        Info.Velocity = vel;
        Info.Lifetime = life;
        Info.Scale = scale;
        Info.DrawColor = col;
    }

    public override void Update()
    {
        Info.Opacity = MathHelper.SmoothStep(1, 0, Info.TimeRatio);
        Info.DrawColor.A = 0;
        Info.Rotation += Info.Velocity.Length() * .07f;
        Info.Scale = (1f - Info.TimeRatio) * Info.InitScale;
        Info.Velocity *= .92f;
    }

    public override void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line)
    {
        Vector2 bloomScale = Info.Texture.Size() / Bloom.Size() + new Vector2(.05f);
        sb.Draw(Bloom, position, null, Info.DrawColor * .12f * Info.Opacity, 0f, Bloom.Size() / 2, bloomScale, 0, 0f);
        sb.Draw(Info.Texture, position, null, Info.DrawColor * Info.Opacity, Info.Rotation, Info.Texture.Size() / 2, Info.Scale, 0, 0f);
    }
}
