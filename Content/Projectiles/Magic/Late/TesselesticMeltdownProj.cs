using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

//TODO
public class TesselesticMeltdownProj : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<TesselesticMeltdown>();
    public override int IntendedProjectileType => ModContent.ProjectileType<TesselesticMeltdownProj>();

    public override string Texture => AssetRegistry.GennedTextures.TesselesticMeltdown.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.NeedsUUID[Type] = true;
    }

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 176;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
    }

    public const float StaffLength = 218f;
    public ref float Heat => ref Owner.GetModPlayer<TesselesticPlayer>().Heat;

    public enum State
    {
        Idle,
        Barrage,
        Beam
    }

    private State CurrentState
    {
        get => (State) Projectile.ai[0];
        set => Projectile.ai[0] = (float) value;
    }

    private ref float Time => ref Projectile.ai[1];
    private ref float OverallTime => ref Projectile.ai[2];

    public bool Released
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public bool Overuse
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }

    public enum BeamState
    {
        Reel,
        Cast,
    }

    private BeamState SubState
    {
        get => (BeamState) Projectile.AdditionsInfo().ExtraAI[2];
        set => Projectile.AdditionsInfo().ExtraAI[2] = (float) value;
    }

    public RotatedRectangle Rect()
    {
        return new(36, Projectile.Center,
            Projectile.Center + PolarVector(StaffLength, Projectile.rotation - MathHelper.PiOver4));
    }

    public Vector2 TipOfStaff => Rect().Top;
    public int Dir => Projectile.velocity.X.NonZeroSign();

    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, 20f, OverallTime);

        switch (CurrentState)
        {
            case State.Idle:
                Behavior_Idle();
                break;
            case State.Barrage:
                if (!Modded.SafeMouseLeft.Current)
                    CurrentState = State.Idle;
                Behavior_Barrage();
                break;
            case State.Beam:
                Behavior_Beam();
                break;
        }

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && !Overuse && CurrentState != State.Beam)
            CurrentState = State.Barrage;
        if (this.RunLocal() && SubState != BeamState.Cast && Modded.SafeMouseRight.Current &&
            CurrentState != State.Beam)
            CurrentState = State.Beam;

        slot ??= LoopedSoundManager.CreateNew(new(AssetRegistry.GennedSounds.ElectricityContinuous, () => .67f),
            () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => CurrentState == State.Barrage);
        slot?.Update(Projectile.Center);

        if (CurrentState != State.Idle)
            Time++;

        OverallTime++;
    }

    private void Behavior_Idle()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Vector2 pos = Owner.MountedCenter + Vector2.UnitX * (40f * Owner.direction) +
                      Vector2.UnitY * 12f * MathF.Sin(Main.GlobalTimeWrappedHourly);
        Projectile.Center = Vector2.Lerp(Projectile.Center, pos + Vector2.UnitY * StaffLength / 2, .6f);
        Projectile.rotation = Projectile.rotation.AngleLerp(-MathHelper.PiOver4, .1f);

        if (Owner.AdditionsMisc().GlobalTimer % 4 == 3)
            ParticleRegistry.SpawnLightningArcParticle(Rect().RandomPoint(), Main.rand.NextVector2Circular(100f, 100f),
                Main.rand.Next(12, 20), .6f, HeatColor);

        if (SubState != BeamState.Reel)
            SubState = BeamState.Reel;
        if (Heat > 0)
            Heat -= .1f;
        if (Heat <= 0)
            Overuse = false;

        Time = 0f;
    }

    public LoopedSoundInstance slot;
    public const int LightningWait = 50;
    public float HeatInterpolant => InverseLerp(0f, LightningWait, Heat);
    public Color HeatColor => Color.Lerp(Color.DeepSkyBlue, Color.Red, HeatInterpolant);

    private void Behavior_Barrage()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity,
                Owner.MountedCenter.SafeDirectionTo(Modded.MouseWorld), .4f);
            Projectile.netUpdate = true;
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Projectile.Center = Vector2.Lerp(Projectile.Center,
            Center - PolarVector(StaffLength / 2, Projectile.rotation - MathHelper.PiOver4),
            Utils.Remap(Time, 0f, 20f, .2f, .7f));

        int wait = (int) MathHelper.Lerp(6, 2, HeatInterpolant);

        if (Time % wait == (wait - 1))
        {
            if (this.RunLocal())
            {
                int type = ModContent.ProjectileType<TesselesticLightning>();
                Vector2 pos = Modded.MouseWorld.ClampOutCircle(Center, StaffLength / 2).ClampInCircle(Center, 1000f);
                TesselesticLightning tess = Main.projectile[Projectile.CreateProj(TipOfStaff, Projectile.velocity,
                        type, Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f, pos.X, pos.Y)]
                    .As<TesselesticLightning>();
                tess.MainColor = HeatColor;
            }

            this.Sync();
        }

        if (Time % (wait * 3) == (wait * 3 - 1))
        {
            Item.CheckManaBetter(Owner, 2, true);
            Heat++;
            this.Sync();
        }

        if (HeatInterpolant >= 1f)
        {
            ParticleRegistry.SpawnPulseRingParticle(TipOfStaff, Vector2.Zero, 20, 0f, Vector2.One, 0f, 190f, Color.Red);
            for (int i = 0; i < 30; i++)
            {
                ParticleRegistry.SpawnBloomLineParticle(TipOfStaff, Main.rand.NextVector2CircularEdge(12f, 12f),
                    Main.rand.Next(10, 20), Main.rand.NextFloat(.2f, .4f), Color.Red);
                ParticleRegistry.SpawnBloomPixelParticle(TipOfStaff, Main.rand.NextVector2Circular(9f, 9f),
                    Main.rand.Next(30, 40), Main.rand.NextFloat(.5f, .8f), HeatColor, Color.IndianRed, null, 1.2f, 4);
            }

            AssetRegistry.GennedSounds.MediumExplosion.Play(TipOfStaff, 1.2f, -.1f, 0f, 4, Name);
            AssetRegistry.GennedSounds.PowerDown.Play(TipOfStaff, 1f, -.1f, 0f, 4, Name);
            ScreenShakeSystem.New(new(.3f, .4f), TipOfStaff);

            Overuse = true;
            CurrentState = State.Idle;
            this.Sync();
        }

        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetBackHandBetter(0, Projectile.rotation - MathHelper.PiOver4);

        Owner.itemAnimation = Owner.itemTime = 2;
        Owner.ChangeDir(Projectile.direction);
    }

    public Vector2 offset;
    public override void WriteExtraAI(BinaryWriter writer) => writer.WriteVector2(offset);
    public override void GetExtraAI(BinaryReader reader) => offset = reader.ReadVector2();

    public static readonly int ReelTime = SecondsToFrames(1.8f);
    public static readonly int OutTime = SecondsToFrames(.4f);
    public float ReelCompletion => InverseLerp(0f, ReelTime, Time);
    public float OutCompletion => InverseLerp(0f, OutTime, Time);

    private void Behavior_Beam()
    {
        if (Time == 0f)
        {
            SubState = BeamState.Reel;
            this.Sync();
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld),
                SubState == BeamState.Cast ? .3f : .7f);
            Projectile.netUpdate = true;
        }

        float dist = 0f;
        switch (SubState)
        {
            case BeamState.Reel:

                dist = MakePoly(4f).InOutFunction.Evaluate(-20f, -100f, ReelCompletion);

                if (this.RunLocal() && !Modded.SafeMouseRight.Current && ReelCompletion >= 1f &&
                    SubState != BeamState.Cast)
                {
                    AssetRegistry.GennedSounds.VirtueAttack.Play(Projectile.Center, 1.3f, 0f, .1f);
                    if (this.RunLocal())
                        Projectile.CreateProj(Rect().Top, Projectile.velocity.SafeNormalize(Vector2.Zero),
                            ModContent.ProjectileType<TesselesticBeam>(),
                            Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f, Projectile.whoAmI);

                    Time = 0f;
                    SubState = BeamState.Cast;
                    this.Sync();
                }
                else if (this.RunLocal() && !Modded.SafeMouseRight.Current && ReelCompletion < 1f)
                {
                    CurrentState = State.Idle;
                }

                break;
            case BeamState.Cast:
                if (!FindProjectile(out _, ModContent.ProjectileType<TesselesticBeam>(), Owner.whoAmI))
                    CurrentState = State.Idle;

                dist = MakePoly(5f).OutFunction.Evaluate(-100f, 0f, OutCompletion);
                break;
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Projectile.Center = Vector2.Lerp(Projectile.Center,
            Center + PolarVector(dist, Projectile.rotation - MathHelper.PiOver4), .8f);

        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver4);

        Owner.itemAnimation = Owner.itemTime = 2;
        Owner.ChangeDir(Projectile.direction);
    }

    public override bool? CanDamage()
    {
        if (CurrentState == State.Beam && Released)
            return null;
        return false;
    }

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

        Texture2D glow = AssetRegistry.GennedTextures.TesselesticMeltdown_Glowmask;

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
            Projectile.rotation + off, origin, Projectile.scale, fx, 0f);

        for (int i = 0; i < 8; i++)
        {
            Vector2 pos = (MathHelper.TwoPi * i / 8).ToRotationVector2() * 2f;
            Main.spriteBatch.Draw(glow, Projectile.Center + pos - Main.screenPosition, null,
                HeatColor with { A = 0 } * HeatInterpolant * Projectile.Opacity,
                Projectile.rotation + off, origin, Projectile.scale, fx, 0f);
        }

        return false;
    }
}

public class TesselesticBeam : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public static readonly int BeamTime = SecondsToFrames(1.5f);
    public static readonly int CollapseTime = SecondsToFrames(.25f);

    public static readonly int LaserExpandTime = SecondsToFrames(.3f);

    public static readonly int Lifetime = BeamTime + CollapseTime + LaserExpandTime;

    public const int MaxLength = 3000;

    public int Time
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public ref float LaserLength => ref Projectile.ai[1];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = MaxLength;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 60;
        Projectile.timeLeft = Lifetime;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.alpha = 255;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 5;
    }

    public override void AI()
    {
        TesselesticMeltdownProj proj = Main.projectile[(int) Projectile.ai[2]].As<TesselesticMeltdownProj>();
        if (proj != null && proj.Projectile.active && proj.Projectile.owner == Projectile.owner)
        {
            Projectile.Center = proj.Rect().Top;
            Projectile.velocity = proj.Projectile.velocity.SafeNormalize(Vector2.Zero);
        }

        Projectile.rotation = Projectile.velocity.ToRotation();

        Time++;

        float lifeInterpolant = InverseLerp(0f, Lifetime, Time);

        float laserExpandEnd = (float) (LaserExpandTime) / Lifetime;
        float beamEnd = (float) (LaserExpandTime + BeamTime) / Lifetime;
        float collapseEnd = 1f;

        LaserLength = new PiecewiseCurve()
            .Add(0f, MaxLength, laserExpandEnd, MakePoly(3f).OutFunction)
            .AddStall(MaxLength, beamEnd)
            .Add(MaxLength, 0f, collapseEnd, MakePoly(3f).InOutFunction)
            .Evaluate(lifeInterpolant);

        Projectile.scale = new PiecewiseCurve()
            .Add(0f, 1f, laserExpandEnd, MakePoly(5f).OutFunction)
            .AddStall(1f, beamEnd)
            .Add(1f, 0f, collapseEnd, MakePoly(3f).InOutFunction)
            .Evaluate(lifeInterpolant);

        trailPoints.SetPoints(
            Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity * LaserLength, 100));

        if (trail == null || trail.Disposed)
            trail = new(LaserWidthFunction, LaserColorFunction, null, 100);

        if (Time > 0 && Time < Lifetime - 10)
        {
            Vector2 vel = Projectile.velocity.RotatedByRandom(Main.rand.NextFloat(.34f, .42f)) *
                          Main.rand.NextFloat(150f, 980f);
            ParticleRegistry.SpawnLightningArcParticle(Projectile.Center, vel, Main.rand.Next(10, 20), 1f,
                Color.DeepSkyBlue);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength,
            Projectile.Size.Length());
    }

    public override bool ShouldUpdatePosition() => false;

    public float LaserWidthFunction(float _) => Projectile.width * Projectile.scale;

    public Color LaserColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float colorInterpolant = Sin01(Main.GlobalTimeWrappedHourly * -3.2f + completionRatio.X * 23f);
        return Color.Lerp(Color.Cyan * 1.2f, Color.DeepSkyBlue, colorInterpolant * 0.67f) *
               MathHelper.SmoothStep(1f, .8f, completionRatio.X);
    }

    public Trail trail;
    public TrailPoints trailPoints = new(100);

    public override bool PreDraw(ref Color lightColor)
    {
        void drawBeam()
        {
            if (trail != null && !trail.Disposed)
            {
                ManagedShader beam = AssetRegistry.GennedShaders.BaseLaserShader;
                beam.TrySetParameter("heatInterpolant", 2f);
                beam.SetTexture(AssetRegistry.GennedTextures.CrackedNoise, 0);
                beam.SetTexture(AssetRegistry.GennedTextures.FlameMap2, 1);
                beam.SetTexture(AssetRegistry.GennedTextures.SuperWavyPerlin, 2);
                trail.DrawTrail(beam, trailPoints.Points);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(drawBeam, PixelationLayer.OverPlayers);

        return false;
    }
}

public sealed class TesselesticPlayer : ModPlayer
{
    public float Heat;
    public override void UpdateDead() => Heat = 0;
}
