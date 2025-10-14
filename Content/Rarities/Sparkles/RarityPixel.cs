using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Rarities.Sparkles;

public class RarityPixel : Behavior<RarityParticleInfo>
{
    public override string TexturePath => AssetRegistry.GetTexturePath(AdditionsTexture.Pixel);
    public static readonly Texture2D Bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
    public Color BloomColor;
    public Vector2? HomeIn;
    public Vector2[] oldPos = new Vector2[10];
    public byte TrailCount;
    public RarityPixel(Vector2 pos, Vector2 vel, int life, float scale, Color col, Color bloom, Vector2? home = null, byte trail = 0)
    {
        Info.Position = pos;
        Info.Velocity = InitVel = vel;
        Info.Lifetime = life;
        Info.Scale = scale;
        Info.DrawColor = col;
        Info.Opacity = 1f;
        BloomColor = bloom;
        HomeIn = home;
        TrailCount = trail;
        if (TrailCount > 0)
        {
            int num = TrailCount;

            if (num != oldPos.Length)
                Array.Resize(ref oldPos, num);
            for (int i = 0; i < oldPos.Length; i++)
                oldPos[i] = Vector2.Zero;
        }
    }
    private float Delay;
    private float Timer;
    private Vector2 InitVel;
    public override void Update()
    {
        if (Info.TimeRatio > .7f)
        {
            Info.Scale *= .9f;
            Info.Opacity *= .92f;
        }

        if (TrailCount > 0)
        {
            for (int j = oldPos.Length - 1; j > 0; j--)
            {
                oldPos[j] = oldPos[j - 1];
            }
            oldPos[0] = Info.Position;
        }

        if (HomeIn != null)
        {
            Info.Velocity = Vector2.Lerp(Info.Velocity, Info.Position.SafeDirectionTo(HomeIn.Value) * 5f, .2f);
            if (Info.Position.WithinRange(HomeIn.Value, 10f))
                Info.Time = Info.Lifetime;
        }
        else
        {
            Info.Velocity = InitVel.VelEqualTrig(MathF.Cos, 20f, .4f, ref Delay, ref Timer);
            Info.Velocity *= .96f;
        }

        Info.Rotation += Info.Velocity.Length() / 5;
    }
    public override bool UseAdditive => true;
    public override void Draw(SpriteBatch sb, Vector2 position, DrawableTooltipLine line)
    {
        sb.Draw(Bloom, position, null, BloomColor * Info.Opacity * .3f, Info.Rotation, Bloom.Size() / 2, Info.Scale / 4, 0, 0f);

        if (TrailCount > 0)
        {
            for (int i = 0; i < oldPos.Length; i++)
            {
                Vector2 old = oldPos[i];
                float completion = 1f - InverseLerp(0f, oldPos.Length, i);
                position = new Vector2(line.X, line.Y) + line.Font.MeasureString(line.Text) * 0.5f + old;
                sb.Draw(Info.Texture, position, null, Info.DrawColor * Info.Opacity * completion,
                    Info.Rotation, Info.Texture.Size() / 2, Info.Scale * 6 * completion, 0, 0f);
            }
        }
        else
            sb.Draw(Info.Texture, position, null, Info.DrawColor * Info.Opacity, Info.Rotation, Info.Texture.Size() / 2, Info.Scale * 6, 0, 0f);
    }
}
