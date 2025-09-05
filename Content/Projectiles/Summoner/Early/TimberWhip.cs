using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Content.Projectiles.Base;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Early;

public class TimberWhip : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TimberWhip);
    public override int SegmentSkip => 8;
    public override void Defaults()
    {
        Projectile.Size = new(140, 50);
    }

    public override void SafeAI()
    {
        Visuals();
    }

    public override void CrackEffects()
    {
        float rot = RandomRotation();
        for (int i = 0; i < 8; i++)
            ParticleRegistry.SpawnSparkParticle(Tip, (MathHelper.TwoPi * i / 8 + rot).ToRotationVector2() * 4f, 14, .45f, Color.AntiqueWhite);

        SoundID.Item153.Play(Tip, 1f, .14f);
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        if (index == (WhipPoints.Count - 1) && Completion.BetweenNum(.45f, .55f))
        {
            for (int i = 0; i < 12; i++)
            {
                ParticleRegistry.SpawnSparkParticle(pos + Main.rand.NextVector2Circular(4f, 4f), vel.RotatedByRandom(.3f) * Main.rand.NextFloat(4f, 7f),
                    Main.rand.Next(10, 20), Main.rand.NextFloat(.4f, .6f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(.4f, .6f)));
            }
        }

        Projectile.damage = (int)(Projectile.damage * .8f);
    }

    public override void ModifyNPCEffects(NPC target, ref NPC.HitModifiers modifiers, in Vector2 pos, in int index)
    {
        if (index == (WhipPoints.Count - 1) && Completion.BetweenNum(.45f, .55f))
            modifiers.SetCrit();
    }

    public void Visuals()
    {
        Projectile.scale = MathHelper.Lerp(.9f, 1.5f, GetLerpBump(0f, .4f, 1f, .6f, Completion)) * GetThin(GetCompletion());
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 10, 24);
        Rectangle seg1Frame = new(0, 24, 10, 16);
        Rectangle seg2Frame = new(0, 40, 10, 16);
        Rectangle seg3Frame = new(0, 56, 10, 18);
        Rectangle tipFrame = new(0, 74, 10, 14);

        int len = WhipPoints.Points.Length - 1;
        for (int i = 0; i < len; i++)
        {
            Vector2 pos = WhipPoints.Points[i];
            Vector2 next = WhipPoints.Points[i + 1];

            Rectangle frame;
            bool hilt = i == 0;
            bool tip = i == len - 1;
            bool shouldDraw = i % SegmentSkip == (SegmentSkip - 1);
            if (hilt || tip)
                shouldDraw = true;

            if (hilt)
                frame = hiltFrame;
            else if (i < (len / 3))
                frame = seg1Frame;
            else if (i < (len / 2))
                frame = seg2Frame;
            else if (i < (len - 1))
                frame = seg3Frame;
            else
                frame = tipFrame;

            if (shouldDraw)
            {
                Vector3 light = Lighting.GetSubLight(pos);
                Color color = Projectile.GetAlpha(new(light.X, light.Y, light.Z));
                float rotation = (next - pos).ToRotation() - MathHelper.PiOver2;
                Vector2 orig = frame.Size() / 2;
                SpriteEffects flip = Owner.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Main.spriteBatch.DrawBetter(texture, pos, frame, color, rotation, orig, tip ? Projectile.scale : 1f, flip);
            }
        }
    }
}