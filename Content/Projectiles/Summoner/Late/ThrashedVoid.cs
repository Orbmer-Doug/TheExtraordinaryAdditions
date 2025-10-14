using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class ThrashedVoid : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ThrashedVoid);

    public override void Defaults()
    {
        Projectile.Size = new(620, 400);
    }

    public override int SegmentSkip => 4;

    public int SwingDir
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public override void SafeAI()
    {
        if (Time == 0f)
        {
            Projectile.velocity = Projectile.velocity.RotatedByRandom(.6f);
            SwingDir = Main.rand.NextFromList(-1, 1);
        }

        if (Trail == null || Trail.Disposed)
            Trail = new(TrailWidth, TrailColor, null, Samples);

        points.Update(Tip);

        if (Main.rand.NextBool(8) && Completion.BetweenNum(.3f, .8f))
        {
            Vector2 pos = WhipPoints.Points[Main.rand.Next(0, WhipPoints.Count - 1)];
            Vector2 vel = OutwardVel * Main.rand.NextFloat(100f, 400f);
            ParticleRegistry.SpawnLightningArcParticle(pos, vel, Main.rand.Next(4, 10), Main.rand.NextFloat(.4f, .8f), Color.DarkViolet.Lerp(Color.Purple, Main.rand.NextFloat(.4f, .6f)));
        }
    }

    public override void CrackEffects()
    {
        if (this.RunLocal())
            Projectile.NewProj(Tip, Vector2.Zero, ModContent.ProjectileType<VoidBlast>(), Projectile.damage / 4, Projectile.knockBack, Owner.whoAmI);
        AdditionsSound.commandoBlast2.Play(Tip, 1.1f, -.1f, .1f);
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        Projectile.damage = (int)(Projectile.damage * .85f);
        target.AddBuff(ModContent.BuffType<VoidDebuff>(), SecondsToFrames(4));
    }

    public override Color LineColor(SystemVector2 completion, Vector2 position)
    {
        return Color.BlueViolet;
    }
    public override float LineWidth(float completion)
    {
        return 8f * MathHelper.SmoothStep(1f, .5f, completion);
    }

    public static Color TrailColor(SystemVector2 completion, Vector2 position)
    {
        return new Color(21 + (int)(10 * completion.X), 3, 51 + (int)(10 * completion.X)) * completion.X;
    }

    public static float TrailWidth(float completion)
    {
        return MathHelper.SmoothStep(1f, 0f, completion) * 25f;
    }

    public OptimizedPrimitiveTrail Trail;
    public TrailPoints points = new(10);
    public override void DrawLine()
    {
        if (Trail != null)
        {
            ManagedShader shader = ShaderRegistry.EnlightenedBeam;
            shader.TrySetParameter("time", Main.GameUpdateCount * .02f);
            shader.TrySetParameter("repeats", 12f);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakLightning), 1, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 2, SamplerState.LinearWrap);
            Trail.DrawTrail(shader, points.Points);
        }

        if (Line != null)
        {
            ManagedShader fire = ShaderRegistry.SpecialLightningTrail;
            fire.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise2), 1);
            Line.DrawTrail(fire, WhipPoints.Points);
        }
    }

    public override float GetTheta(float t)
    {
        if (SwingDir == -1)
            return -base.GetTheta(t);
        return base.GetTheta(t);
    }

    public override void DrawSegments()
    {
        void bloom()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowHarsh);

            for (int i = 0; i < WhipPoints.Count; i++)
            {
                Vector2 pos = WhipPoints.Points[i];
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(pos, new Vector2(150f) * InverseLerp(0f, WhipPoints.Count, i)), null, Color.Purple, 0f, tex.Size() / 2);
            }
        }
        PixelationSystem.QueueTextureRenderAction(bloom, PixelationLayer.OverProjectiles, BlendState.Additive);

        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 14, 26);
        Rectangle seg1Frame = new(0, 26, 14, 18);
        Rectangle seg2Frame = new(0, 44, 14, 16);
        Rectangle seg3Frame = new(0, 60, 14, 16);
        Rectangle tipFrame = new(0, 76, 14, 18);

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