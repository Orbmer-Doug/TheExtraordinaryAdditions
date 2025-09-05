using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Content.Projectiles.Base;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class AttorcoppeProjectile : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SpiderWhipProjectile);

    public bool Second
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public override int SegmentSkip => 3;

    public override void Defaults()
    {
        Projectile.Size = new(400, 250);
    }

    public override void SafeAI()
    {
        if (Completion.BetweenNum(.2f, .8f))
        {
            Dust.NewDustPerfect(Tip, DustID.Venom, Main.rand.NextVector2Circular(5f, 5f), 0, default, Main.rand.NextFloat(.5f, .9f)).noGravity = true;

            if (Main.rand.NextBool(3))
            {
                Vector2 pos = WhipPoints.Points[Main.rand.Next(0, WhipPoints.Count - 2)];
                Dust.NewDustPerfect(pos, DustID.Web, OutwardVel, 0, default, Main.rand.NextFloat(.4f, .8f)).noGravity = true;
            }
        }
    }

    public override void CrackEffects()
    {
        if (!Second)
        {
            float rot = RandomRotation();
            for (int i = 0; i < 8; i++)
                ParticleRegistry.SpawnSparkParticle(Tip, (MathHelper.TwoPi * i / 8 + rot).ToRotationVector2() * 7f, 18, .58f, Color.Violet);
        }
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        if (index == (WhipPoints.Count - 1))
            target.AddBuff(BuffID.Venom, 180);
        Projectile.damage = (int)(Projectile.damage * 0.9f);
    }

    public override void ModifyNPCEffects(NPC target, ref NPC.HitModifiers modifiers, in Vector2 pos, in int index)
    {
        if (index == (WhipPoints.Count - 1) && Completion.BetweenNum(.45f, .55f))
            modifiers.SetCrit();
    }

    public override float GetTheta(float t)
    {
        if (Second)
            return -base.GetTheta(t);

        return base.GetTheta(t);
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 10, 24);
        Rectangle seg1Frame = new(0, 24, 10, 18);
        Rectangle seg2Frame = new(0, 42, 10, 16);
        Rectangle seg3Frame = new(0, 58, 10, 16);
        Rectangle tipFrame = new(0, 74, 10, 18);

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