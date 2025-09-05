using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Debuff;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SolarBrandSword : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<SolarBrand>();
    public override int IntendedProjectileType => ModContent.ProjectileType<SolarBrandSword>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SolarBrand);

    public const int MaxHeight = 242;
    public const int Points = 200;
    public List<Vector2> cache;
    public TrailPoints trailPoints = new(10);
    public OptimizedPrimitiveTrail trail;

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
        Main.projFrames[Type] = 8;
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

    public Vector2 Tip => Projectile.Center + PolarVector(98, Projectile.rotation - PiOver2);
    public Vector2 Base => Projectile.Center + PolarVector(-55, Projectile.rotation - PiOver2);

    public override bool ShouldDie() => false;
    public override void SafeAI()
    {
        if (Time == 0f)
        {
            if (this.RunLocal())
                Projectile.velocity = Center.SafeDirectionTo(Mouse);
            Projectile.rotation = Projectile.velocity.ToRotation() + PiOver2;
            this.Sync();
        }

        if (trail == null || trail._disposed)
            trail = new(TrailWidthFunct, TrailColorFunct, TrailOffsetFunct, Points);

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
            float newRotation = Projectile.rotation.AngleLerp(Center.AngleTo(Mouse) + PiOver2, aimResponsiveness);
            Projectile.rotation = newRotation;
            if (Projectile.rotation != Projectile.oldRot[1])
                this.Sync();
        }

        float towards = Projectile.rotation - PiOver2;
        Projectile.velocity = towards.ToRotationVector2();
        Projectile.Center = Center + PolarVector(90, towards);

        // Owner values
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, towards - PiOver2);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, towards - PiOver2);
        Owner.heldProj = Projectile.whoAmI;

        Projectile.Opacity = MakePoly(2.7f).InFunction(FadeInterpolant);
        Projectile.direction = Owner.direction;
        Projectile.height = (int)Utils.Remap(FadeTimer, 0f, FadeTime, 0f, MaxHeight);
        Projectile.width = (int)Utils.Remap(FadeTimer, 0f, FadeTime, 0f, 28);

        // Visuals
        Projectile.SetAnimation(8, 6);
        Lighting.AddLight(Base, Color.OrangeRed.ToVector3() * Projectile.Opacity);
        HandleDamage();

        if (MathF.Abs(WrapAngle(Projectile.rotation - Projectile.oldRot[1])) > .4f && Projectile.soundDelay == 0 && Time > 6f)
        {
            Vector2 vel = Projectile.velocity * 15f;
            if (this.RunLocal())
            {
                Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<SolarBrandSparks>(), (int)(Projectile.damage * 0.2f), 0f, Owner.whoAmI, 0f, 0f, 0f);
                Projectile.NewProj(Projectile.Center, vel * 2f, ModContent.ProjectileType<SolarBrandSparks>(), (int)(Projectile.damage * 0.35f), 0f, Owner.whoAmI, 0f, 0f, 0f);
            }

            for (int i = 0; i < 25; i++)
            {
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.1f, .3f), Main.rand.Next(20, 35), Main.rand.NextFloat(.4f, .8f), Color.OrangeRed);
            }

            SoundID.DD2_BetsyFireballShot.Play(Projectile.Center, 1.2f, 0f, .2f);

            Projectile.soundDelay = 12;
        }

        trailPoints.Update(Vector2.Lerp(Base, Tip, .5f) - Center);

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
            for (int i = 0; i < 25; i++)
            {
                Vector2 vel = trailPoints.Points[^4].SafeDirectionTo(trailPoints.Points[^1]).RotatedByRandom(.3f) * Main.rand.NextFloat(2f, 5f);
                int life = Main.rand.Next(30, 60);
                float scale = Main.rand.NextFloat(.5f, .9f);
                Color col = Color.OrangeRed;
                ParticleRegistry.SpawnHeavySmokeParticle(start, vel, life, scale, col, 2f);
                ParticleRegistry.SpawnGlowParticle(start, vel * .7f, life, scale * 60f, col, 1.6f);
            }
            SoundID.DD2_BetsyFireballImpact.Play(start, 1.2f, .3f, .1f);
        }
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Base, Tip, Projectile.width, DelegateMethods.CutTiles);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawTrail();
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        Vector2 orig = frame.Size() * .5f;
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Color.White * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale);

        return false;
    }

    public Color TrailColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opacity = InverseLerp(0.016f, 0.07f, MathF.Abs(WrapAngle(Projectile.rotation - Projectile.oldRot[1])));
        return Color.OrangeRed * opacity * Projectile.Opacity;
    }

    public SystemVector2 TrailOffsetFunct(float c) => Center.ToNumerics();
    public static float TrailWidthFunct(float c) => 120f;

    private void DrawTrail()
    {
        void draw()
        {
            if (trail != null)
            {
                ManagedShader shader = ShaderRegistry.SwordRipShader;
                shader.TrySetParameter("flip", Math.Sign(WrapAngle(Projectile.rotation - Projectile.oldRot[1])) > 0);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SwordSlashTexture), 1, SamplerState.LinearWrap);
                trail.DrawTrail(shader, trailPoints.Points, 200, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.HeldProjectiles);
    }
}