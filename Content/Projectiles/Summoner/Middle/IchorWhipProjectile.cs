using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class IchorWhipProjectile : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.IchorWhipp);
    public override int TipSize => 18;
    public override int SegmentSkip => 3;

    public override void SafeAI()
    {
        if (Completion.BetweenNum(.2f, .8f) && Main.rand.NextBool(2))
        {
            Vector2 vel = OutwardVel.RotatedByRandom(.1f) * Main.rand.NextFloat(3f, 5f);
            float scale = Main.rand.NextFloat(.9f, 1.3f);
            Dust.NewDustPerfect(Tip, DustID.Ichor, vel, 0, default, scale).noGravity = true;
        }
        Lighting.AddLight(Tip, Color.Gold.ToVector3() * Convert01To010(GetCompletion()));

        Projectile.scale = MathHelper.Lerp(0.5f, 1.5f, GetLerpBump(0f, .4f, 1f, .6f, Completion)) * GetThin(GetCompletion());
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        for (int i = 0; i < 12; i++)
        {
            Vector2 veloc = vel.RotatedByRandom(.4f) * Main.rand.NextFloat(5f, 9f) * Convert01To010(GetCompletion());
            ParticleRegistry.SpawnSquishyPixelParticle(pos, veloc,
                Main.rand.Next(70, 90), Main.rand.NextFloat(.8f, 1.5f), Color.Gold, Color.Yellow, 3, true, true);
        }

        target.AddBuff(BuffID.Ichor, (int)(SecondsToFrames(4) * Convert01To010(GetCompletion())));
        Projectile.damage = (int)(Projectile.damage * .8f);
    }

    public override Color LineColor(SystemVector2 completion, Vector2 position)
    {
        return Color.Lerp(Color.Gold, Color.Goldenrod, completion.X);
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 10, 26);
        Rectangle seg1Frame = new(0, 26, 10, 16);
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

        void glow()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Vector2 orig = tex.Size() / 2;
            float completion = Convert01To010(GetCompletion());
            for (int i = 0; i < 2; i++)
            {
                Rectangle targ = ToTarget(Tip, new Vector2(30 + (i * 10)) * completion);
                Main.spriteBatch.DrawBetterRect(tex, targ, null, Color.Gold * completion, 0f, orig);
            }
        }
        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.OverProjectiles, BlendState.Additive);
    }
}