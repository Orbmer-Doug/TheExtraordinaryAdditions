using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.TesselesticMeltdownProj;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class TheTesselesticMeltdown : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TesselesticMeltdown);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 176;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 9999;
        Projectile.netImportant = true;
    }

    public RotatedRectangle Rect()
    {
        return new(36, Projectile.Center, Projectile.Center + PolarVector(TesselesticMeltdownProj.staffLength, Projectile.rotation - MathHelper.PiOver4));
    }
    public Vector2 TipOfStaff => Rect().Top;
    public int Dir => Boss.Direction;

    public State CurrentState
    {
        get => (State)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public ref float StateTime => ref Projectile.ai[1];
    public ref float OverallTime => ref Projectile.ai[2];
    public BeamState SubState
    {
        get => (BeamState)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = (float)value;
    }
    public LoopedSound slot;

    public override void SafeAI()
    {
        if (Boss.Cleave_FadeTimer <= 0f)
        {
            Projectile.Opacity = InverseLerp(0f, 20f, OverallTime);
        }
        else
        {
            Projectile.Opacity = InverseLerp(30f, 0f, Boss.Cleave_FadeTimer);
            if (Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }
        }

        switch (CurrentState)
        {
            case State.Idle:
                Behavior_Idle();
                break;
            case State.Barrage:
                Behavior_Barrage();
                break;
        }

        SoundStyle style = AssetRegistry.GetSound(AdditionsSound.ElectricityContinuous);
        slot ??= new LoopedSound(style, () => CurrentState == State.Barrage && new ProjectileAudioTracker(Projectile).IsActiveAndInGame());
        slot.Update(() => Projectile.Center, () => .67f, () => 0f);

        Projectile.timeLeft = 2;
        OverallTime++;
    }

    private void Behavior_Idle()
    {
        Vector2 pos = Boss.RotatedHitbox.Center + Vector2.UnitX * (140f * -Boss.Direction) + Vector2.UnitY * 22f * MathF.Sin(Main.GlobalTimeWrappedHourly);
        Projectile.velocity = Vector2.UnitX * -Boss.Direction;
        Projectile.Center = Vector2.Lerp(Projectile.Center, pos + Vector2.UnitY * staffLength / 2, .6f);
        Projectile.rotation = Projectile.rotation.AngleLerp(-MathHelper.PiOver4, .1f);

        if (OverallTime % 4f == 3f)
        {
            ParticleRegistry.SpawnLightningArcParticle(Rect().RandomPoint(), Main.rand.NextVector2Circular(100f, 100f), Main.rand.Next(12, 20), .6f, Color.Cyan);
        }

        StateTime = 0f;
    }

    private void Behavior_Barrage()
    {
        if (StateTime > 35f && OverallTime % 6 == 5)
        {
            int type = ModContent.ProjectileType<HonedTesselesticLightning>();
            HonedTesselesticLightning tess = Main.projectile[Projectile.NewProj(TipOfStaff, Projectile.velocity,
                type, Projectile.damage, Projectile.knockBack, Projectile.owner)].As<HonedTesselesticLightning>();
            tess.End = Rect().Top + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.25f) * Main.rand.NextFloat(400f, 900f);
            tess.Projectile.netUpdate = true;
        }

        Vector2 dir = Projectile.Center.SafeDirectionTo(Target.Center);
        float amt = Utils.Remap(Projectile.velocity.Distance(dir), 0f, 1f, .1f, .2f);
        Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, dir, amt);

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Projectile.Center = Vector2.Lerp(Projectile.Center, Boss.RightHandPosition, Utils.Remap(StateTime, 0f, 50f, .02f, .5f));
        StateTime++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        bool flip = Dir == -1;

        Vector2 origin;
        float off;
        SpriteEffects fx;
        if (flip)
        {
            origin = new Vector2(0, tex.Height);

            off = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(tex.Width, tex.Height);

            off = MathHelper.PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.TesselesticMeltdown_Glowmask);

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
            Projectile.rotation + off, origin, Projectile.scale, fx, 0f);
        return false;
    }
}

public class HonedTesselesticLightning : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int Life = 30;
    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }

    public ref float Time => ref Projectile.ai[0];
    public Vector2 End { get; set; }
    public float Completion => Animators.MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            points = new(100);
            points.SetPoints(GetBoltPoints(Projectile.Center, End, 10f, 7f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity.BetweenNum(0f, .05f))
            Projectile.Kill();

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * .4f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c) => 40f * InverseLerp(1.5f, 0f, c) * Projectile.Opacity;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(Completion, Color.White, Color.Cyan) * Projectile.Opacity;
    public ManualTrailPoints points;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1);
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}