using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class ExsanguinationProj : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ExsanguinationProj);
    public override int SegmentSkip => 3;
    public override void Defaults()
    {
        Projectile.Size = new(600, 250);
    }

    public static readonly int TotalEmbedTime = SecondsToFrames(5);
    public bool AbleToHit
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float ClickTimer => ref Projectile.Additions().ExtraAI[0];
    public bool Embedded
    {
        get => Projectile.Additions().ExtraAI[1] == 1f;
        set => Projectile.Additions().ExtraAI[1] = value.ToInt();
    }

    public OptimizedPrimitiveTrail Line2;
    public NPC target;
    public ref float SavedCompletion => ref Projectile.Additions().ExtraAI[2];
    public ref float EmbedTime => ref Projectile.Additions().ExtraAI[3];

    public ref float LineBrightness => ref Projectile.Additions().ExtraAI[4];

    public override bool? CanHitNPC(NPC target)
    {
        if (Embedded)
            return AbleToHit ? null : false;

        return null;
    }

    public override void SafeAI()
    {
        if (Line2 == null || Line2._disposed)
            Line2 = new(LineWidth2, LineColor2, null, Samples);

        if (Time == 0f)
            LineBrightness = .2f;

        if (LineBrightness > .2f)
            LineBrightness -= .05f;

        if (ClickTimer > 0f)
            ClickTimer--;

        if (Embedded)
        {
            OverrideWhipPoints = true;
            PauseTimer = true;

            if (ClickTimer <= 0 && Modded.MouseLeft.JustPressed)
            {
                Projectile.ResetLocalNPCHitImmunity();
                AbleToHit = true;
                ClickTimer = 4f;
            }

            Projectile.velocity = Projectile.SafeDirectionTo(target.Center);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() - MathHelper.PiOver2);

            if (EmbedTime > TotalEmbedTime || target.life <= 0 || target == null || target.active == false)
            {
                Projectile.damage = 0;
                Completion = SavedCompletion;
                OverrideWhipPoints = PauseTimer = false;
                target = null;
                Embedded = false;
            }

            EmbedTime++;
        }
        else
        {
            if (Completion.BetweenNum(.2f, .8f))
            {
                for (int i = 0; i < 2; i++)
                    StygainEnergy.Spawn(Tip, OutwardVel.RotatedBy(MathHelper.PiOver2 * Owner.direction * Owner.gravDir).RotatedByRandom(.1f) * Main.rand.NextFloat(1f, 6f), Main.rand.NextFloat(40f, 55f));
            }
        }
    }

    public override void OverridePoints()
    {
        WhipPoints.SetPoints(Projectile.Center.GetLaserControlPoints(target.Center, Samples));
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        AbleToHit = false;

        if (target.life > Projectile.damage)
        {
            if (Embedded)
            {
                AdditionsSound.SwordSlice.Play(pos, .8f, 0f, .2f, 10);

                LineBrightness = 1f;

                Owner.Heal(Main.rand.Next(1, 3));
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Vector2.One * 105f, Vector2.Zero, 30, Color.Crimson);
                for (int i = 0; i < 20; i++)
                {
                    Vector2 cir = Main.rand.NextVector2Circular(12f, 12f);
                    if (i % 2f == 0f)
                    {
                        ParticleRegistry.SpawnGlowParticle(pos, cir, Main.rand.Next(15, 25), Main.rand.NextFloat(20f, 50f), Color.Crimson);
                    }
                    ParticleRegistry.SpawnBloodParticle(pos, cir, Main.rand.Next(30, 50), Main.rand.NextFloat(.7f, 1f), Color.Crimson);
                }
            }
            else
            {
                SavedCompletion = Completion;
                Projectile.ownerHitCheck = false;
                this.target = target;
                Embedded = true;
            }
        }
    }

    public override Color LineColor(SystemVector2 completion, Vector2 position)
    {
        if (completion.X > 0.99f)
            return Color.Transparent;

        return Color.IndianRed * LineBrightness * .5f * MakePoly(3).OutFunction(1 - completion.X);
    }

    public Color LineColor2(SystemVector2 completion, Vector2 position)
    {
        float progress = MakePoly(3).OutFunction(1 - completion.X);
        return Color.Lerp(Color.DarkRed, Color.Crimson, MakePoly(3).InFunction(1 - progress)) * LineBrightness * progress;
    }

    public override float LineWidth(float completion)
    {
        return 30f;
    }

    public static float LineWidth2(float completion) => 3 * Main.rand.NextFloat(0.55f, 1.45f);

    public override void DrawLine()
    {
        if (Line != null && Line2 != null)
        {
            ManagedShader beam = ShaderRegistry.EnlightenedBeam;
            beam.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 8f);
            beam.TrySetParameter("repeats", 2f);
            beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ShadowTrail), 1, SamplerState.LinearWrap);
            beam.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 2, SamplerState.LinearWrap);

            Line.DrawTrail(beam, WhipPoints.Points);
            Line2.DrawTrail(beam, WhipPoints.Points);
        }
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 14, 26);
        Rectangle seg1Frame = new(0, 26, 14, 16);
        Rectangle seg2Frame = new(0, 42, 14, 16);
        Rectangle seg3Frame = new(0, 58, 14, 18);
        Rectangle tipFrame = new(0, 76, 14, 16);

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