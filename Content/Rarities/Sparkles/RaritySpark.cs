using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.Sparkles;

public class RaritySpark : Behavior<RarityParticleInfo>
{
    public override string TexturePath => AssetRegistry.GetTexturePath(AdditionsTexture.Gleam);
    public RaritySpark(Vector2 pos, Vector2 vel, int life, float scale, Color col)
    {
        Info.Position = pos;
        Info.Velocity = vel;
        Info.Lifetime = life;
        Info.Scale = scale;
        Info.DrawColor = col;
    }
    public override void Update()
    {
        Info.Scale *= 0.97f;
        Info.Opacity = MathF.Pow(MathHelper.SmoothStep(1, 0, Info.TimeRatio), .1f);
        Info.DrawColor.A = 0;

        Info.Rotation = Info.Velocity.ToRotation() + MathHelper.PiOver2;
        Info.Velocity *= .96f;
    }
    public override void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line)
    {
        Vector2 orig = Info.Texture.Size() / 2;
        sb.Draw(Info.Texture, position, null, Info.DrawColor * .15f * Info.Opacity, Info.Rotation, orig, new Vector2(.5f, 1.4f) * Info.Scale * 2f, 0, 0f);
        sb.Draw(Info.Texture, position, null, Info.DrawColor.Lerp(Color.White, .1f) * .5f * Info.Opacity, Info.Rotation, orig, new Vector2(.4f, 1.2f) * Info.Scale * 1.5f, 0, 0f);
        sb.Draw(Info.Texture, position, null, Info.DrawColor.Lerp(Color.White, .2f) * Info.Opacity, Info.Rotation, orig, new Vector2(.3f, 1f) * Info.Scale, 0, 0f);
    }
}
