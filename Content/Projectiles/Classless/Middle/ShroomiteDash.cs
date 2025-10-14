using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class ShroomiteDash : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.penetrate = 1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 50;

        Projectile.netImportant = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float DashTime => ref Projectile.ai[1];
    private bool IsDashing
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    private bool HitEnemy
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    private float trailWidth = 1;

    public const float MaxCharge = 120f;
    public const float TimeDashing = 20f;
    public float Completion => InverseLerp(0f, MaxCharge, Time);
    public float DashCompletion => InverseLerp(0f, TimeDashing, DashTime);
    public float Spread => 1.1f * (1f - Animators.BezierEase(Completion)) * 0.98f;
    public Player Owner => Main.player[Projectile.owner];

    public override void AI()
    {
        Vector2 mouse = Owner.Additions().mouseWorld;
        Time++;

        if (!HitEnemy)
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 10);

        if ((this.RunLocal() && AdditionsKeybinds.SetBonusHotKey.JustReleased) && Completion >= 1f && !IsDashing)
        {
            SoundEngine.PlaySound(SoundID.DD2_JavelinThrowersAttack with { Volume = 2f, Pitch = -.3f }, Owner.Center);
            IsDashing = true;
            this.Sync();
        }
        else if ((this.RunLocal() && AdditionsKeybinds.SetBonusHotKey.Current) && !IsDashing)
        {
            Projectile.velocity = Projectile.Center.SafeDirectionTo(mouse);
            if (Completion == 1f && Projectile.localAI[0] == 0f)
            {
                AdditionsSound.HeatTail.Play(Projectile.Center, .8f, .2f);
                Projectile.localAI[0] = 1f;
            }
            if (Completion >= 1f && Main.rand.NextBool(5))
                ParticleRegistry.SpawnBloomPixelParticle(Owner.RotHitbox().RandomPoint(), -Vector2.UnitY * Main.rand.NextFloat(3f, 6f), Main.rand.Next(20, 40), Main.rand.NextFloat(.1f, .2f), new(85, 89, 225), Color.DarkBlue, null, .5f);
            IsDashing = false;
            this.Sync();
        }
        else if (!IsDashing && AdditionsKeybinds.SetBonusHotKey.Current == false)
            Projectile.Kill();

        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.timeLeft = 2;
        Owner.direction = Projectile.direction;

        if (IsDashing)
        {
            DashTime++;

            if (!HitEnemy)
            {
                if (this.RunLocal())
                {
                    float rotationstrength = MathHelper.Pi / 40f * Animators.MakePoly(3f).InFunction(DashCompletion);
                    float currentRotation = Projectile.velocity.ToRotation();
                    float idealRotation = Owner.MountedCenter.SafeDirectionTo(mouse).ToRotation();

                    Projectile.velocity = currentRotation.AngleTowards(idealRotation, rotationstrength).ToRotationVector2();
                    this.Sync();
                }

                float velocityPower = Convert01To010(DashCompletion);
                Vector2 newVelocity = Projectile.velocity * 50f * (.2f + velocityPower);
                Owner.velocity = newVelocity;

                cache ??= new(10);
                cache.Update(Owner.RotHitbox().Bottom + Projectile.velocity.SafeNormalize(Vector2.Zero));
            }
            else
            {
                trailWidth *= 0.93f;

                if (trailWidth > 0.05f)
                    trailWidth -= 0.05f;
                else
                    trailWidth = 0;
            }

            Owner.Additions().LungingDown = true;
            float correction = (Owner.direction == -1 ? MathHelper.Pi : 0f);
            Owner.fullRotation = Projectile.rotation + correction + (MathHelper.PiOver2 * -Owner.direction);
            Owner.fullRotationOrigin = Owner.Center - Owner.position;

            if (DashTime > TimeDashing)
                Projectile.Kill();
        }
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (IsDashing)
            return null;
        
        return false;
    }

    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Kablooey
        if (!HitEnemy)
        {
            Owner.velocity = -Owner.velocity * .2f;

            Vector2 pos = Projectile.Center;
            ParticleRegistry.SpawnPulseRingParticle(pos, -Projectile.velocity, 30, Projectile.rotation, new(.5f, 1f), 0f, 500f, Color.CornflowerBlue);
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = -Projectile.velocity.RotatedByRandom(.65f) * Main.rand.NextFloat(10f, 20f);
                int life = Main.rand.Next(45, 80);
                float scale = Main.rand.NextFloat(.5f, 1f);
                ParticleRegistry.SpawnGlowParticle(pos, vel * 3f, life, scale, Color.CornflowerBlue);
                ParticleRegistry.SpawnMistParticle(pos, vel, scale * 2, Color.CornflowerBlue, Color.DarkBlue, 180);
            }

            if (this.RunLocal())
                Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<ShroomiteDashImpact>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            AdditionsSound.VirtueAttack.Play(Projectile.Center, 1.2f, .1f);
            ScreenShakeSystem.New(new(.7f, .5f), Projectile.Center);

            HitEnemy = true;
        }
    }

    public override void OnKill(int timeLeft)
    {
        Owner.Additions().LungingDown = false;
        Owner.fullRotation = 0f;
    }

    public float WidthFunct(float c) => OptimizedPrimitiveTrail.PyriformWidthFunct(c, 80f * trailWidth, 1f, .2f, .8f);
    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        Color col = Color.Lerp(Color.Lerp(new Color(85, 89, 225), new Color(66, 180, 216), DashCompletion), new Color(99, 155, 255), c.X) * MathHelper.SmoothStep(1f, 0f, c.X);
        col *= 1f - DashCompletion;
        return col * trailWidth;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        if (!IsDashing)
        {
            ManagedShader effect = ShaderRegistry.SpreadTelegraph;
            effect.TrySetParameter("centerOpacity", 1.7f);
            effect.TrySetParameter("mainOpacity", (float)Math.Sqrt(Completion) * 2f);
            effect.TrySetParameter("halfSpreadAngle", Spread / 2f);
            effect.TrySetParameter("edgeColor", Color.DodgerBlue.ToVector3());

            effect.TrySetParameter("centerColor", Color.DodgerBlue.ToVector3());
            effect.TrySetParameter("edgeBlendLength", 0.1f);
            effect.TrySetParameter("edgeBlendStrength", 5f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, effect.Effect, Main.GameViewMatrix.TransformationMatrix);
            Texture2D invis = AssetRegistry.InvisTex;
            Main.EntitySpriteDraw(invis, Owner.Center - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(invis.Width / 2f, invis.Height / 2f), 700f, 0, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        else
        {
            void draw()
            {
                if (trail == null || cache == null)
                    return;

                ManagedShader shader = ShaderRegistry.EnlightenedBeam;

                shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 4f);
                shader.TrySetParameter("repeats", 12f);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Streak), 1, SamplerState.LinearWrap);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 2, SamplerState.LinearWrap);

                trail.DrawTrail(shader, cache.Points);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }
        return false;
    }
}
