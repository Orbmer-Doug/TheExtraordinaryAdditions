using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class RendedStarHoldout : BaseIdleHoldoutProjectile, IHasScreenShader
{
    public override int AssociatedItemID => ModContent.ItemType<RendedStar>();
    public override int IntendedProjectileType => ModContent.ProjectileType<RendedStarHoldout>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RendedStar);

    public LoopedSoundInstance slot;

    public const int MaxHeight = 500;
    public const int Points = 200;
    public List<Vector2> cache;
    public ManualTrailPoints points = new(Points);
    public OptimizedPrimitiveTrail trail;

    public float RotationDifference => CurrentRotation - (Projectile.rotation + MathHelper.PiOver2);
    private Vector2 ControlPointSmall => Projectile.Center - PolarVector(MaxHeight * .1f * Projectile.Opacity, Projectile.rotation + MathHelper.PiOver2);
    private Vector2 ControlPointMed => Projectile.Center - PolarVector(MaxHeight * .3f, Projectile.rotation + MathHelper.PiOver2 - RotationDifference);
    private Vector2 ControlPoint => Projectile.Center - PolarVector(MaxHeight * .7f, Projectile.rotation + MathHelper.PiOver2);
    private Vector2 EndCurvePoint => Projectile.Center - PolarVector(MaxHeight * .85f, CurrentRotation - (RotationDifference * MathHelper.PiOver4));
    private Vector2 CurrentPoint => Projectile.Center - PolarVector(MaxHeight, CurrentRotation);

    public ref float Time => ref Projectile.ai[0];

    public float AngularDamageFactor
    {
        get
        {
            return Projectile.ai[1];
        }
        set
        {
            Projectile.ai[1] = value;
        }
    }

    public ref float RotationUpdate => ref Projectile.ai[2];

    /// <summary>
    /// Lags behind the leading rotation (<see cref="Projectile.rotation"/>)
    /// </summary>
    public ref float CurrentRotation => ref Projectile.Additions().ExtraAI[0];

    public ref float FadeTimer => ref Projectile.Additions().ExtraAI[1];
    internal const int FadeTime = 40;
    public float FadeInterpolant => InverseLerp(0f, FadeTime, FadeTimer);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = Points;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false;
    }

    public override void WriteExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.rotation);
    }
    public override void GetExtraAI(BinaryReader reader)
    {
        Projectile.rotation = reader.ReadSingle();
    }

    public override void Defaults()
    {
        Projectile.DamageType = DamageClass.Melee;
        Projectile.width = Projectile.height = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 13;
    }

    public override bool ShouldDie() => false;
    public override void SafeAI()
    {
        if (Time == 0f)
        {
            Vector2 mouseVel = Center.SafeDirectionTo(Mouse);
            Projectile.velocity = mouseVel;
            Projectile.rotation = CurrentRotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.netUpdate = true;
        }

        if (trail == null || trail._disposed)
            trail = new(c => 174f * 2f, (c, pos) => Lighting.GetColor(pos.ToTileCoordinates()), OffsetFunct, Points);

        if (base.ShouldDie())
        {
            if (FadeTimer > 0)
                FadeTimer--;
            if (FadeTimer <= 0)
                Projectile.Kill();
        }
        else
        {
            if (FadeTimer < FadeTime)
                FadeTimer++;
            Projectile.timeLeft = FadeTime;
        }

        if (this.RunLocal())
        {
            // Rotation shenanigans
            float rotDifference = ((Projectile.rotation + MathHelper.PiOver2 - CurrentRotation) % MathHelper.TwoPi + 9.42f) % MathHelper.TwoPi - MathHelper.Pi;
            CurrentRotation = MathHelper.Lerp(CurrentRotation, CurrentRotation + rotDifference, RotationUpdate);

            float rotDifference2 = ((Center.SafeDirectionTo(Mouse).ToRotation() + MathHelper.PiOver2 - Projectile.rotation) % MathHelper.TwoPi + 9.42f) % MathHelper.TwoPi - MathHelper.Pi;
            Projectile.rotation = MathHelper.Lerp(Projectile.rotation, Projectile.rotation + rotDifference2, RotationUpdate);
            if (Projectile.rotation != Projectile.oldRot[1])
                this.Sync();
        }

        RotationUpdate = .18f;

        // Update fire sounds
        slot ??= LoopedSoundManager.CreateNew(new(AdditionsSound.HeavyFireLoop, () => .47f * Projectile.scale), () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => Projectile.active);
        slot?.Update(Projectile.Center);

        float towards = Projectile.rotation - MathHelper.PiOver2;
        Projectile.velocity = towards.ToRotationVector2();
        Projectile.Center = Center + PolarVector(MakePoly(5f).InFunction(FadeInterpolant) * 50f, towards);

        // Owner values
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, towards);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, towards);
        Owner.heldProj = Projectile.whoAmI;

        Projectile.scale = Projectile.Opacity = MakePoly(2.7f).InFunction(FadeInterpolant);
        Projectile.direction = Owner.direction;
        Projectile.height = (int)Utils.Remap(FadeTimer, 0f, FadeTime, 0f, MaxHeight);

        // Visuals
        Vector2 pos = ControlPointSmall + PolarVector(Main.rand.NextFloat(-30f, 30f) * Projectile.Opacity, towards + (Main.rand.NextBool().ToDirectionInt() * MathHelper.PiOver2))
            + PolarVector(Main.rand.NextFloat(0f, 20f) * Projectile.Opacity, towards);
        Vector2 vel = ControlPointSmall.SafeDirectionTo(pos);
        Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red.Lerp(Color.OrangeRed, .3f), Color.OrangeRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);

        if (Main.rand.NextBool())
        {
            int life = Main.rand.Next(30, 50);
            float scale = Main.rand.NextFloat(.4f, .9f);
            Color col = Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(.4f, .7f));

            ParticleRegistry.SpawnGlowParticle(pos, RandomVelocity(1f, 1f, 3f), life, scale * 20f * Projectile.scale, col, 1.8f);
        }
        if (Main.rand.NextBool(7))
        {
            ParticleRegistry.SpawnSparkParticle(pos, vel * Main.rand.NextFloat(8f, 12f), Main.rand.Next(72, 120), Main.rand.NextFloat(.3f, .5f) * Projectile.scale, fireColor.Lerp(Color.White, .4f), true, true);
        }
        for (int i = 0; i < 3; i++)
        {
            ParticleRegistry.SpawnHeavySmokeParticle(ControlPointSmall + Main.rand.NextVector2Circular(6, 6), vel, Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .5f) * Projectile.scale, fireColor);
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * Main.rand.NextFloat(1f, 3f), Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .8f) * Projectile.scale, fireColor, 2f);
        }

        HandleDamage();
        ManageCaches();
        Time++;
    }

    private void HandleDamage()
    {
        float num = MathHelper.WrapAngle(Projectile.rotation) + MathHelper.Pi;
        float oldRotationAdjusted = MathHelper.WrapAngle(Projectile.oldRot[1]) + MathHelper.Pi;
        float deltaAngle = Math.Abs(MathHelper.WrapAngle(num - oldRotationAdjusted));

        AngularDamageFactor = MathHelper.Lerp(AngularDamageFactor, deltaAngle, 0.08f);

        float speedDamageScalar = 0.166f + (float)Math.Log(AngularDamageFactor / ((float)Math.PI / 30f) + 1.5f, 3.0);
        int damageWithChargeAndStats = Owner.GetWeaponDamage(Owner.HeldItem, false);
        float sizeDamageScalar = 1f;
        Projectile.damage = (int)(damageWithChargeAndStats * speedDamageScalar * sizeDamageScalar);
    }

    public override bool PreKill(int timeLeft)
    {
        ReleaseShader();
        return base.PreKill(timeLeft);
    }

    public override void OnKill(int timeLeft)
    {
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, c => 120f);
    }

    public override bool? CanDamage() => Projectile.scale >= 1f;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(ModContent.BuffType<PlasmaIncineration>(), SecondsToFrames(6));
        if (AngularDamageFactor > 0.1f)
        {
            AdditionsSound.etherealHit1.Play(target.Center, 1.2f, 0f, .16f);
            for (int i = 0; i < 12; i++)
                ShaderParticleRegistry.SpawnMoltenParticle(target.RotHitbox().RandomPoint(), 90f * AngularDamageFactor);
        }
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Projectile.Center, CurrentPoint, 70, DelegateMethods.CutTiles);
    }

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("HeatDistortionFilter");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        Shader.TrySetParameter("intensity", Projectile.scale * .5f);
        Shader.TrySetParameter("screenPos", ControlPointSmall - Main.screenPosition);
        Shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        Shader.TrySetParameter("radius", (750f * Projectile.scale) / Main.screenWidth);
        Shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("HeatDistortionFilter", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }
    public bool IsEntityActive() => Projectile.active;

    public override bool PreDraw(ref Color lightColor)
    {
        if (!HasShader)
            InitializeShader();

        UpdateShader();

        DrawFlare();

        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() * .5f;
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White, Projectile.rotation, orig, Projectile.scale);

        return false;
    }

    private void ManageCaches()
    {
        cache = [];
        Vector2 offset = PolarVector(6f * Projectile.scale, Projectile.rotation - MathHelper.PiOver2);
        BezierCurves curve = new([Projectile.Center + offset, ControlPointSmall + offset, ControlPointMed + offset, ControlPoint + offset, EndCurvePoint + offset, CurrentPoint + offset]);
        cache = curve.GetPoints(Points);
        cache.Reverse();

        points.SetPoints(cache);
    }

    public SystemVector2 OffsetFunct(float c)
    {
        SystemVector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero).ToNumerics();
        SystemVector2 normal = new(-dir.Y, dir.X);
        return normal * MathF.Sin(c * MathHelper.Pi + Time / 8f) * 45f * MathHelper.SmoothStep(.5f, 0f, c);
    }

    private void DrawFlare()
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader fire = AssetRegistry.GetShader("Emblazed");
                fire.TrySetParameter("NoiseOffset", Vector2.One * Main.GameUpdateCount * 0.02f + Vector2.One * (0.003f * .6f));
                fire.TrySetParameter("brightness", 20);
                fire.TrySetParameter("MainScale", Projectile.scale);
                fire.TrySetParameter("CenterPoint", new Vector2(0.5f, 1f));
                fire.TrySetParameter("TrailDirection", new Vector2(0, -1));
                fire.TrySetParameter("width", 0.85f);
                fire.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 0.15f);
                fire.TrySetParameter("distort", 0.4f);
                fire.TrySetParameter("progMult", 3.7f);
                fire.TrySetParameter("startColor", Color.White.ToVector3());
                fire.TrySetParameter("endColor", Color.OrangeRed.ToVector3());
                fire.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 1);

                trail.DrawTrail(fire, points.Points, 500);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles, BlendState.Additive);
    }
}