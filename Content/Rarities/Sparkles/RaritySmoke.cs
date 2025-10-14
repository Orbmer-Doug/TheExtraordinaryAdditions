using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Rarities.Sparkles;

public class RaritySmoke : Behavior<RarityParticleInfo>
{
    public override string TexturePath => AssetRegistry.GetTexturePath(AdditionsTexture.NebulaGas2);
    public float MaxScale;
    public RaritySmoke(Vector2 pos, Vector2 vel, int life, float maxScale, Color col)
    {
        Info.Position = pos;
        Info.Velocity = vel;
        Info.Lifetime = life;
        Info.DrawColor = col;
        Info.Rotation = RandomRotation();
        MaxScale = maxScale;
    }
    public override void Update()
    {
        Info.Rotation += Info.Velocity.Length() / 10;
        Info.Velocity *= .8f;
        Info.Opacity = Info.Scale = GetLerpBump(0f, .6f, 1f, .4f, Info.TimeRatio) * MaxScale;
    }
    public override bool UseAdditive => true;
    public override void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line)
    {
        sb.Draw(Info.Texture, position, null, Info.DrawColor * Info.Opacity, Info.Rotation,
            Info.Texture.Size() / 2, Info.Scale * .5f, 0, 0f);
    }
}
