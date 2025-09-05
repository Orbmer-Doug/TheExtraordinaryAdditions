using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Rarities.Sparkles;

public class RarityDroplet : Behavior<RarityParticleInfo>
{
    public override string TexturePath => AssetRegistry.GetTexturePath(AdditionsTexture.DropletTexture);
    public RarityDroplet(Vector2 pos, Vector2 vel, int life, float scale, Color color)
    {
        Info.Position = pos;
        Info.Velocity = vel;
        Info.Lifetime = life;
        Info.Scale = scale;
        Info.DrawColor = color;
        Info.Opacity = 1f;
    }
    public override void Update()
    {
        Info.Scale = MathF.Pow(MathHelper.SmoothStep(1, 0, Info.TimeRatio), .4f);
        Info.Opacity *= .98f;
        Info.Rotation = Info.Velocity.ToRotation() - MathHelper.PiOver2;
        Info.DrawColor.A = 0;
    }
    public override void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line)
    {
        sb.Draw(Info.Texture, position, null, Info.DrawColor * Info.Opacity * .4f,
            Info.Rotation, Info.Texture.Size() / 2, Info.Scale / 3 * 1.2f, 0, 0f);

        sb.Draw(Info.Texture, position, null, Info.DrawColor * Info.Opacity, Info.Rotation, Info.Texture.Size() / 2, Info.Scale / 3, 0, 0f);
    }
}
