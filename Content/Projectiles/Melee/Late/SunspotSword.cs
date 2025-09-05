using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class SunspotSword : BaseIdleHoldoutProjectile, IHasScreenShader
{
    public override int AssociatedItemID => ModContent.ItemType<Sunspot>();
    public override int IntendedProjectileType => ModContent.ProjectileType<SunspotSword>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Sunspot);
    public const int MaxHeight = 242;
    public const int Points = 200;
    public List<Vector2> cache;
    public TrailPoints trailPoints = new(10);
    public OptimizedPrimitiveTrail trail;
    public ManualTrailPoints points = new(Points);
    public OptimizedPrimitiveTrail sword;

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
        Projectile.ownerHitCheck = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public Vector2 Tip => Projectile.Center + PolarVector(122, Projectile.rotation - PiOver2);
    public Vector2 Base => Projectile.Center + PolarVector(-28, Projectile.rotation - PiOver2);

    public override bool ShouldDie() => false;
    public override void SafeAI()
    {
        if (Time == 0f)
        {
            Vector2 mouseVel = Center.SafeDirectionTo(Mouse);
            Projectile.velocity = mouseVel;
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
        }

        if (!HasShader)
            InitializeShader();
        UpdateShader();

        if (trail == null || trail._disposed)
            trail = new(TrailWidthFunct, TrailColorFunct, TrailOffsetFunct, Points);
        if (sword == null || sword._disposed)
            sword = new(c => 22f, (c, pos) => Color.OrangeRed, null, Points);

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

        // Rotation shenanigans
        if (this.RunLocal())
        {
            float mouseDistance = Center.Distance(Mouse);
            float distRatio = Utils.GetLerpValue(0f, 360f, mouseDistance, true);
            float aimResponsiveness = 0.035f + 0.3f * (float)Math.Pow(distRatio, .33f);
            float newRotation = Projectile.rotation.AngleLerp(Owner.AngleTo(Mouse) + PiOver2, aimResponsiveness);
            Projectile.rotation = newRotation;
            if (Projectile.rotation != Projectile.oldRot[1])
                this.Sync();
        }

        float towards = Projectile.rotation - PiOver2;
        Projectile.velocity = towards.ToRotationVector2();
        Projectile.Center = Center + PolarVector(120f, towards);

        // Owner values
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, towards);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, towards);
        Owner.heldProj = Projectile.whoAmI;

        Projectile.Opacity = MakePoly(2.7f).InFunction(FadeInterpolant);
        Projectile.direction = Owner.direction;
        Projectile.height = (int)Utils.Remap(FadeTimer, 0f, FadeTime, 0f, MaxHeight);
        Projectile.width = (int)Utils.Remap(FadeTimer, 0f, FadeTime, 0f, 28);

        // Visuals
        HandleDamage();

        if (MathF.Abs(WrapAngle(Projectile.rotation - Projectile.oldRot[1])) > .4f && Projectile.soundDelay == 0 && Time > 6f)
        {
            AdditionsSound.BraveSwingMedium.Play(Projectile.Center, .6f, 0f, .2f);
            Projectile.soundDelay = 12;
        }
        ManageCaches();

        Time++;
    }

    private void HandleDamage()
    {
        float num = WrapAngle(Projectile.rotation) + Pi;
        float oldRotationAdjusted = WrapAngle(Projectile.oldRot[1]) + Pi;
        float deltaAngle = Math.Abs(WrapAngle(num - oldRotationAdjusted));

        AngularDamageFactor = Lerp(AngularDamageFactor, deltaAngle, 0.08f);

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
        Owner.fullRotation = 0f;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Base, Tip, Projectile.width);
    }

    public override bool? CanDamage() => Projectile.scale >= 1f;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(ModContent.BuffType<PlasmaIncineration>(), SecondsToFrames(3));
        if (AngularDamageFactor > 0.1f)
        {
            Vector2 start = target.Hitbox.ClosestPointInRect(Tip);
            for (int i = 0; i < 45; i++)
            {
                Vector2 vel = trailPoints.Points[^4].SafeDirectionTo(trailPoints.Points[^1]).RotatedByRandom(.4f) * Main.rand.NextFloat(2f, 10f);
                int life = Main.rand.Next(30, 60);
                float scale = Main.rand.NextFloat(.5f, .9f);
                Color col = Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(.3f, .6f));
                ParticleRegistry.SpawnHeavySmokeParticle(start, vel, life, scale, col, 2f);
                ParticleRegistry.SpawnGlowParticle(start, vel * .7f, life, scale * 60f, col, 1.6f);
            }
            AdditionsSound.SwordSliceShort.Play(start, .7f, 0f, .1f, 20);
        }
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Base, Tip, Projectile.width, DelegateMethods.CutTiles);
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
        Shader.TrySetParameter("intensity", Projectile.Opacity * .25f);
        Shader.TrySetParameter("screenPos", Base - Main.screenPosition);
        Shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        Shader.TrySetParameter("radius", (550f * Projectile.scale) / Main.screenWidth);
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
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() * .5f;
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale);

        DrawTrail();
        DrawBlade();

        return false;
    }

    private void ManageCaches()
    {
        trailPoints.Update(Vector2.Lerp(Base, Tip, .45f) - Center);
        points.SetPoints(Base.GetLaserControlPoints(Tip, 50));
    }

    public Color TrailColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opacity = InverseLerp(0.016f, 0.07f, MathF.Abs(WrapAngle(Projectile.rotation - Projectile.oldRot[1])));
        return Color.OrangeRed * opacity * Projectile.Opacity;
    }

    public SystemVector2 TrailOffsetFunct(float c) => Center.ToNumerics();
    public static float TrailWidthFunct(float c) => 150f;

    private void DrawBlade()
    {
        void draw()
        {
            if (sword != null)
            {
                ManagedShader shader = AssetRegistry.GetShader("SunspotBlade");
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise2), 1, SamplerState.LinearWrap);
                shader.TrySetParameter("appearanceInterpolant", Projectile.Opacity);
                sword.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);
    }

    private void DrawTrail()
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = AssetRegistry.GetShader("SunspotTrail");
                shader.TrySetParameter("flip", Math.Sign(WrapAngle(Projectile.rotation - Projectile.oldRot[1])) > 0);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FluidPerlin), 1, SamplerState.LinearWrap);
                trail.DrawTrail(shader, trailPoints.Points, 200, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);
    }
}