using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class CyberneticSwing : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CyberneticSwing);

    public enum SwingState
    {
        /// <summary>
        /// L - L
        /// </summary>
        LightningPunch,

        /// <summary>
        /// L - R
        /// </summary>
        Parry,

        /// <summary>
        ///  R - R
        /// </summary>
        SMASH,

        /// <summary>
        /// R - L
        /// </summary>
        Uppercut,
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float Speed => Owner.GetTotalAttackSpeed(DamageClass.Melee);
    public int PunchTime => (int)(90 / Speed);
    public const int ReelTime = 40;
    public int ParryTime => (int)(20 / Speed);
    public int SmashTime => (int)(62 / Speed);
    public int UppercutTime => (int)(70 / Speed);

    public SwingState State
    {
        get => (SwingState)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }

    public bool HitTarget
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float Reel => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float Dist => ref Projectile.AdditionsInfo().ExtraAI[2];
    public ref float OldDist => ref Projectile.AdditionsInfo().ExtraAI[3];
    public bool MadeWave
    {
        get => Projectile.AdditionsInfo().ExtraAI[4] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[4] = value.ToInt();
    }

    public override void SetDefaults()
    {
        Projectile.width = 58;
        Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 9999;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.MaxUpdates = 3;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        if (this.RunLocal() && !Owner.Available())
        {
            Projectile.Kill();
            return;
        }

        if (Time == 0f)
        {
            Projectile.velocity = Center.SafeDirectionTo(ModdedOwner.mouseWorld);
            Projectile.netUpdate = true;
        }

        Owner.ChangeDir(Dir);
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);

        switch (State)
        {
            case SwingState.LightningPunch:
                DoState_LightningPunch();
                break;
            case SwingState.Parry:
                DoState_Parry();
                break;
            case SwingState.SMASH:
                DoState_SMASH();
                break;
            case SwingState.Uppercut:
                DoState_Uppercut();
                break;
        }

        Time++;
    }

    public void DoState_LightningPunch()
    {
        if (Time == 0f)
            AdditionsSound.etherealReleaseA.Play(Projectile.Center, 1.1f, 0f, .1f);

        float comp = InverseLerp(0f, PunchTime, Time);
        if (Reel >= ReelTime || comp >= 1f)
        {
            Projectile.Kill();
        }

        Dist = new PiecewiseCurve()
            .Add(Projectile.width, 500f, .5f, MakePoly(4f).OutFunction)
            .Add(500f, Projectile.width, 1f, MakePoly(3f).InFunction)
            .Evaluate(comp);

        if (HitTarget)
        {
            Dist = Animators.MakePoly(3f).InOutFunction.Evaluate(OldDist, Projectile.width, InverseLerp(0f, ReelTime, Reel));
            Reel++;
        }
        else
            OldDist = Dist;

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 20);
        points.Update(Projectile.Center);
        ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(1f, 6f), Main.rand.Next(40, 60), Main.rand.NextFloat(.4f, .7f), Color.Cyan);

        Projectile.Center = Center + PolarVector(Dist, Projectile.velocity.ToRotation());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Projectile.Opacity = InverseLerp(0f, 10f * Projectile.MaxUpdates, Time);
    }

    public const float ParryDist = 130f;
    public void DoState_Parry()
    {
        if (Time == 0f)
            SoundID.Item1.Play(Projectile.Center, .8f, -.2f);
        if (Projectile.Opacity <= 0f)
        {
            Projectile.Kill();
        }

        if (!CalUtils.HasCooldown(Owner, CyberneticParryCooldown.ID))
        {
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (!proj.active || !proj.hostile || proj.damage <= 1 || !(proj.velocity.Length() * (proj.extraUpdates + 1) > 1f))
                    continue;
                if (!Projectile.Center.IsInFieldOfView(Projectile.velocity.ToRotation(), proj.Center, MathHelper.PiOver4, ParryDist))
                    continue;
                if (HitTarget)
                    continue;

                AdditionsSound.harpoonStop.Play(Projectile.Center, .9f, 0f, .1f, 10, Name);
                ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 30, 0f, Vector2.One, 0f, 200f, Color.Cyan);
                float speed = proj.velocity.Length();
                proj.velocity = proj.SafeDirectionTo(ModdedOwner.mouseWorld) * speed;
                proj.ProjDamageMod().ParriedTimer = SecondsToFrames(3);
                proj.netUpdate = true;

                HitTarget = true;
            }
        }

        Projectile.Center = Center + PolarVector(30f, Projectile.velocity.ToRotation());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Center.AngleTo(Projectile.Center));
        Projectile.Opacity = InverseLerp(ParryTime, 0f, Time);
    }

    public void DoState_SMASH()
    {
        if (Time == 0f)
            SoundID.DD2_GhastlyGlaivePierce.Play(Projectile.Center, 1.2f, -.2f, .1f, null, 10, Name);

        float comp = InverseLerp(0f, SmashTime, Time);
        if (comp >= 1f)
            Projectile.Kill();

        if (Time > (SmashTime / 2) && !MadeWave)
        {
            if (this.RunLocal())
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
                Projectile.NewProj(Center + PolarVector(100f, dir.ToRotation()), dir * 10f, ModContent.ProjectileType<CyberPierce>(), (int)(Projectile.damage * .75f), 2f, Projectile.owner);
            }
            MadeWave = true;
            this.Sync();
        }

        OldDist = Dist;
        Dist = Projectile.velocity.ToRotation() + (MathHelper.PiOver2 + .2f) * Exp(2.2f).InOutFunction.Evaluate(-1f, 1f, comp) * Dir;
        Projectile.Center = Center + PolarVector(100f, Dist);
        Projectile.rotation = Dist + MathHelper.PiOver2;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Center.AngleTo(Projectile.Center));
        ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(1f, 6f),
            Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .7f), Color.DarkCyan);

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, OffsetFunct, 20);
        points.Update(Projectile.Center - Center);
        Projectile.Opacity = InverseLerp(SmashTime, SmashTime - (10f * Projectile.MaxUpdates), Time);
    }

    public void DoState_Uppercut()
    {
        if (Time == 0f)
            SoundID.DD2_GhastlyGlaivePierce.Play(Projectile.Center, 1.2f, .2f, .1f, null, 10, Name);

        float comp = MathF.Round(InverseLerp(0f, UppercutTime, Time), 2);
        if (comp >= 1f)
        {
            Projectile.Kill();
        }

        if (this.RunLocal())
        {
            if (comp >= .5f && !MadeWave)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = Center + PolarVector(100f, Projectile.velocity.ToRotation() + (MathHelper.PiOver2 - .2f) * MathHelper.Lerp(1f, -1f, InverseLerp(0, 3 - 1, i)) * Dir);
                    Projectile.NewProj(pos, Projectile.velocity * 18f, ModContent.ProjectileType<CyberDart>(), (int)(Projectile.damage * .33f), 0f, Projectile.owner);
                }
                MadeWave = true;
                this.Sync();
            }
        }

        OldDist = Dist;
        Dist = Projectile.velocity.ToRotation() + (MathHelper.PiOver2 - .2f) * Exp(2.2f).OutFunction.Evaluate(1f, -1f, comp) * Dir;
        Projectile.Center = Center + PolarVector(100f, Dist);
        Projectile.rotation = Dist - MathHelper.PiOver2;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Center.AngleTo(Projectile.Center));
        ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.rotation.ToRotationVector2() * Main.rand.NextFloat(1f, 6f),
    Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .7f), Color.DarkCyan);

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, OffsetFunct, 20);
        points.Update(Projectile.Center - Center);
        Projectile.Opacity = InverseLerp(SmashTime, SmashTime - (10f * Projectile.MaxUpdates), Time);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        switch (State)
        {
            case SwingState.LightningPunch:
                modifiers.Knockback += 10f;
                break;
            case SwingState.Parry:
                break;
            case SwingState.SMASH:
                modifiers.FinalDamage *= 2f;
                modifiers.ScalingArmorPenetration += 1f;
                break;
            case SwingState.Uppercut:
                modifiers.FinalDamage *= 2f;
                modifiers.ScalingArmorPenetration += 1f;
                break;
        }
    }

    public override bool? CanDamage() => State == SwingState.Parry ? false : (State == SwingState.LightningPunch && HitTarget) ? false : null;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (State == SwingState.LightningPunch)
        {
            if (this.RunLocal())
                Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CyberneticBlast>(), Projectile.damage / 2, Projectile.knockBack / 2, Projectile.owner);
            AdditionsSound.etherealSlam.Play(Projectile.Center, 1.4f, -.2f, .1f, 20, Name);
            ScreenShakeSystem.New(new(.5f, .3f), Projectile.Center);
            for (int i = 0; i < 30; i++)
            {
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, Main.rand.NextVector2Circular(7, 7),
                    Main.rand.Next(40, 70), Main.rand.NextFloat(1f, 1.8f), Color.Cyan.Lerp(Color.LightCyan, Main.rand.NextFloat(.2f, .5f)));
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(18, 20), Main.rand.NextFloat(70f, 100f), Color.LightCyan, Main.rand.NextFloat(.8f, 1f));
                ParticleRegistry.SpawnBloomLineParticle(Projectile.Center, Main.rand.NextVector2Circular(10f, 10f), Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .8f), Color.Cyan);
            }
        }

        if (State == SwingState.SMASH || State == SwingState.Uppercut)
        {
            for (int i = 0; i < 40; i++)
            {
                Vector2 vel = Projectile.rotation.ToRotationVector2().RotatedByRandom(.3f) * Main.rand.NextFloat(2f, 11f);

                if (i < 10)
                    ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.Center, vel, Main.rand.Next(30, 40), Main.rand.NextFloat(1.3f, 1.8f), Color.Cyan);
                ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .9f), Color.Cyan);
            }
            AdditionsSound.etherealHit5.Play(Projectile.Center, 1.8f, State == SwingState.Uppercut ? .1f : -.1f, .1f, 10, Name);
        }

        if (!HitTarget)
        {
            HitTarget = true;
        }
    }

    public override void OnKill(int timeLeft)
    {
        if (State == SwingState.Parry && !HitTarget && !CalUtils.HasCooldown(Owner, CyberneticParryCooldown.ID))
            CalUtils.AddCooldown(Owner, CyberneticParryCooldown.ID, SecondsToFrames(4));
    }

    public float WidthFunct(float c) => MathHelper.SmoothStep(Projectile.height, 0f, c);
    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opac = Projectile.Opacity;
        if (State == SwingState.SMASH || State == SwingState.Uppercut)
            opac *= InverseLerp(0.016f, 0.07f, MathF.Abs(MathHelper.WrapAngle(Dist - OldDist)));

        return MulticolorLerp(c.X, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan, Color.DarkSlateBlue) * Projectile.Opacity;
    }
    public SystemVector2 OffsetFunct(float c) => Center.ToNumerics();

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.SmoothFlame;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);
            shader.TrySetParameter("heatInterpolant", .8f);
            trail.DrawTrail(shader, points.Points, 120, false);
        }

        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;

        SpriteEffects effects = SpriteEffects.None;

        if (State == SwingState.LightningPunch)
        {
            effects = Projectile.direction == -Owner.gravDir ? SpriteEffects.FlipVertically : SpriteEffects.None;
            if (Owner.gravDir == -1 && Projectile.direction == -Owner.gravDir)
                effects |= SpriteEffects.FlipVertically;
        }

        if (State == SwingState.Parry)
        {
            effects = (Projectile.direction == 1 && Owner.gravDir == 1) ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            if (Owner.gravDir == -1 && Projectile.direction == -1)
                effects |= SpriteEffects.FlipHorizontally;
        }

        if (State == SwingState.SMASH)
        {
            if (Projectile.direction == -1)
                effects = SpriteEffects.FlipHorizontally;
        }

        if (State == SwingState.Uppercut)
        {
            effects = SpriteEffects.FlipVertically;
            if (Projectile.direction == -1)
                effects |= SpriteEffects.FlipHorizontally;
        }

        bool miss = (State == SwingState.Parry && CalUtils.HasCooldown(Owner, CyberneticParryCooldown.ID));
        Projectile.DrawProjectileBackglow((miss ? Color.Red : Color.Cyan) * .5f * Projectile.Opacity, 2f, 0, 8, effects);
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(miss ? Color.Red : Color.White), rotation, origin, Projectile.scale, effects, 0f);

        if (State == SwingState.Parry && !CalUtils.HasCooldown(Owner, CyberneticParryCooldown.ID))
        {
            ManagedShader shader = AssetRegistry.GetShader("ForcefieldLimited");
            shader.TrySetParameter("direction", Projectile.velocity.ToRotation());
            shader.TrySetParameter("angle", MathHelper.PiOver4);
            shader.TrySetParameter("color", Color.Cyan.ToVector4() * Projectile.Opacity);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.LinearWrap);

            float size = ParryDist;
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive, shader.Effect);
            shader.Render();
            Main.spriteBatch.Draw(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), ToTarget(Center - new Vector2(size / 2), new Vector2(size)), Color.White);
            Main.spriteBatch.ExitShaderRegion();
        }
        else
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}

public class CyberneticBlast : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public const int Lifetime = 40;
    public override void SetDefaults()
    {
        Projectile.Size = new(1);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.localNPCHitCooldown = 30;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public ref float Time => ref Projectile.ai[0];
    public int Radius
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public float Completion => InverseLerp(0f, Lifetime, Time);
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 40);

        Radius = (int)Animators.MakePoly(4f).OutFunction.Evaluate(0f, 100f, Completion);
        for (int i = 0; i < 40; i++)
            points.SetPoint(i, Projectile.Center + Vector2.One.RotatedBy(i / (float)(40 - 1) * (MathF.Tau + float.Epsilon)) * Radius);

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Utility.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);
    }

    public float WidthFunct(float c)
    {
        return Animators.MakePoly(3f).InFunction.Evaluate(20f, 0f, Completion);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return MulticolorLerp(Completion, Color.White, Color.Cyan, Color.DarkCyan) * InverseLerp(0f, 5f, Time) * Animators.MakePoly(2f).OutFunction.Evaluate(1f, 0f, Completion);
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1, SamplerState.LinearWrap);
            trail.DrawTrail(shader, points.Points, 100, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}

public class CyberDart : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(20);
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.friendly = Projectile.ignoreWater = Projectile.noEnchantmentVisuals =
            Projectile.usesLocalNPCImmunity = Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.MaxUpdates = 1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 15);
        points?.Update(Projectile.Center);

        Projectile.Opacity = InverseLerp(0f, 20f, Time) * InverseLerp(0f, 30f, Projectile.timeLeft);
        Projectile.scale = InverseLerp(0f, 30f, Projectile.timeLeft);
        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 750, false, true), out NPC target) && Projectile.penetrate > 0 && Time > 20)
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.Center.SafeDirectionTo(target.Center) * 45f, .3f);

        if (Projectile.penetrate <= 0)
        {
            Projectile.timeLeft = 30;
            if (points.Points.AllPointsEqual())
                Projectile.Kill();
        }
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.etherealSmallHit.Play(Projectile.Center, .7f, 0f, .2f);
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, Vector2.Zero, (int)Utils.Remap(i, 0, 20, 20, 50), Utils.Remap(i, 0, 20, 30f, 60f), Color.DeepSkyBlue);
            ParticleRegistry.SpawnTechyHolosquareParticle(Projectile.Center + Main.rand.NextVector2Circular(10, 10), -Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.2f, .5f), Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .8f), Color.Cyan);
        }
        Projectile.velocity *= 0;
    }

    public float WidthFunct(float c)
    {
        if (c < .3f)
            return MathHelper.Lerp(0f, Projectile.width, InverseLerp(0f, .3f, c)) * Projectile.scale;
        else
            return MathHelper.Lerp(Projectile.width, 0f, InverseLerp(.3f, 1f, c)) * Projectile.scale;
    }
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color.DeepSkyBlue * Utils.Remap(Projectile.Opacity, 0f, 1f, c.X, MathHelper.Clamp(c.X + 1, 0, 1));

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(15);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && !trail.Disposed && points != null)
            {
                ManagedShader shader = ShaderRegistry.BaseLaserShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1, SamplerState.AnisotropicWrap);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 2, SamplerState.AnisotropicWrap);
                trail.DrawTrail(shader, points.Points, 100, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}

public class CyberPierce : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(10);
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.friendly = Projectile.ignoreWater = Projectile.noEnchantmentVisuals = Projectile.usesLocalNPCImmunity = true;
        Projectile.penetrate = 4;
        Projectile.MaxUpdates = 5;
        Projectile.timeLeft = 100 * Projectile.MaxUpdates;
        Projectile.localNPCHitCooldown = 12 * Projectile.MaxUpdates;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public List<Vector2> points = [];
    public override void AI()
    {
        if (!Projectile.FinalExtraUpdate())
            return;

        Projectile.rotation = Projectile.velocity.ToRotation();
        Vector2 c1 = Projectile.Center + PolarVector(50f, Projectile.rotation - MathHelper.PiOver2);
        Vector2 c2 = Projectile.Center + PolarVector(80f, Projectile.rotation);
        Vector2 c3 = Projectile.Center + PolarVector(50f, Projectile.rotation + MathHelper.PiOver2);
        points = Animators.CatmullRomSpline([c1, c2, c3], 30);
        foreach (Vector2 pos in CollectionsMarshal.AsSpan(points))
        {
            ParticleRegistry.SpawnTechyHolosquareParticle(pos, pos.SafeDirectionTo(Projectile.Center - Projectile.rotation.ToRotationVector2() * 10f) * 4f,
                Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .9f), Color.LightSkyBlue, Main.rand.NextFloat(.6f, 1.1f), Main.rand.NextFloat(1f, 1.8f));
            ParticleRegistry.SpawnSparkParticle(pos, -Projectile.velocity * Main.rand.NextFloat(.1f, .2f), Main.rand.Next(20, 38), Main.rand.NextFloat(.4f, .6f), Color.Cyan);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => targetHitbox.CollisionFromPoints(points, 10);

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.velocity *= .9f;
        Projectile.damage = (int)(Projectile.damage * .8f);
    }
}