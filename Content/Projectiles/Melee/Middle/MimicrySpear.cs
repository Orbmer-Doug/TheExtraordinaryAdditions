using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class MimicrySpear : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Mimicry);
    public override void SetDefaults()
    {
        Projectile.width = 66;
        Projectile.height = 76;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = 1000;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public ref float Time => ref Projectile.ai[0];
    public bool Start
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Offset => ref Projectile.ai[2];
    public ref float CurrentStab => ref Projectile.Additions().ExtraAI[0];

    public const float timeReeling = 90f;
    public const float timeOut = 10f;
    public const float timeStab = timeReeling + timeOut;
    public const float totalTime = timeStab + 10f;
    public override void AI()
    {
        if (this.RunLocal() && (Owner.dead || !Owner.active || (!Modded.MouseRight.Current && Time < timeReeling && !Start) || Projectile.Opacity <= 0f))
        {
            Projectile.Kill();
            return;
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.itemAnimation = Owner.itemTime = Projectile.timeLeft = 2;
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());

        if (Modded.MouseRight.Current && !Start)
        {
            if (this.RunLocal())
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Owner.Additions().mouseWorld), .5f);

            Offset = MakePoly(3).InOutFunction.Evaluate(55f, 31f, InverseLerp(0f, timeReeling, Time));
            Projectile.friendly = false;
        }
        if (this.RunLocal() && !Modded.MouseRight.Current && !Start && Time >= timeReeling)
        {
            StabEffects();
            Time = 0f;
            Projectile.friendly = true;
            Start = true;
            Projectile.netUpdate = true;
            Projectile.netSpam = 0;
        }
        if (Start)
        {
            if (CurrentStab >= 3 && Time >= (timeOut * 2))
            {
                Projectile.friendly = false;
                Projectile.Opacity -= .05f;
            }
            else if (Time < timeOut)
            {
                Offset = Circ.OutFunction.Evaluate(15f, 115f, InverseLerp(0f, timeOut, Time));
            }
            else if (Time >= timeOut && CurrentStab < 3)
            {
                if (Time >= (timeOut * 2))
                {
                    Projectile.friendly = true;

                    if (this.RunLocal())
                        Projectile.velocity = Owner.SafeDirectionTo(Owner.Additions().mouseWorld);

                    // Update the hitbox for visuals
                    Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
                    Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + PolarVector(Offset * Projectile.Opacity, Projectile.velocity.ToRotation());

                    StabEffects();
                    Time = 0f;
                }
                else
                {
                    Offset = MakePoly(3).InFunction.Evaluate(115f, 15f, InverseLerp(timeOut, timeOut * 2, Time));
                    Projectile.friendly = false;
                }
            }
        }
        Time++;

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation());

        // Glue to player
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + PolarVector(Offset * Projectile.Opacity, Projectile.velocity.ToRotation());
    }

    public void StabEffects()
    {
        Projectile.ResetLocalNPCHitImmunity();
        CurrentStab++;

        if (Main.dedServ)
            return;

        for (int i = 0; i < 20; i++)
        {
            Vector2 pos = new RotatedRectangle(35f, Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight).RandomPoint();
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(6f, 11f);
            int life = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.4f, .7f);
            Color color = Color.Crimson.Lerp(Color.Red, Main.rand.NextFloat(.2f, .8f));
            ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale * 50f, color, Main.rand.NextFloat(.7f, 1f));
            ParticleRegistry.SpawnBloomLineParticle(pos, vel, life - 9, scale + .1f, color);
        }

        AdditionsSound.MimicrySwing.Play(Projectile.Center, 2f, .3f);
        ScreenShakeSystem.New(new(.4f, .3f), Projectile.Center);
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight, 16f);
    }

    public override bool? CanHitNPC(NPC target)
    {
        return Start ? null : false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.Defense *= 0f;
        modifiers.DefenseEffectiveness *= 0f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (CheckLinearCollision(Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight, target.Hitbox, out Vector2 start, out Vector2 end))
        {
            for (int i = 0; i < 25; i++)
            {
                ParticleRegistry.SpawnGlowParticle(start, Projectile.velocity.RotatedByRandom(.12f) * Main.rand.NextFloat(6f, 12f), 40, .5f, Color.DarkRed);
                ParticleRegistry.SpawnBloomLineParticle(start, Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(10f, 18f), Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .5f), Color.Crimson);

                Dust.NewDustPerfect(start + Main.rand.NextVector2Circular(5f, 5f), DustID.Blood, Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(4f, 10f), 0, default, Main.rand.NextFloat(.5f, 1f));
            }
            ParticleRegistry.SpawnBloodStreakParticle(start, Projectile.velocity, Main.rand.Next(24, 34), Main.rand.NextFloat(.3f, .5f), Color.Crimson);

            if (Projectile.numHits <= 0)
                Owner.Heal(10);

            MimicrySlash.TargetObliteration(target);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();

        float interpolant = !Start ? InverseLerp(0f, timeReeling, Time) : Projectile.scale;
        Vector2 offsets = new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition;
        Vector2 drawStartOuter = offsets + Projectile.Center + Projectile.velocity;
        Vector2 spinPoint = -Vector2.UnitY * 3f * interpolant * Projectile.scale;
        float rotation = Main.GlobalTimeWrappedHourly;
        float opacity = .85f * interpolant;

        float rotOff;
        SpriteEffects fx;
        if (MathF.Cos(Projectile.rotation - MathHelper.PiOver4) < 0f)
        {
            rotOff = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            rotOff = MathHelper.PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }

        const int amt = 8;
        for (int i = 0; i < amt; i++)
        {
            Vector2 spinStart = drawStartOuter + Utils.RotatedBy(spinPoint, (double)(rotation - (float)Math.PI * i / (amt / 2)), default);
            Color glowAlpha = Projectile.GetAlpha(Color.Crimson * Projectile.Opacity);
            glowAlpha.A = (byte)Projectile.alpha;
            Main.spriteBatch.Draw(tex, spinStart, null, glowAlpha * opacity * Projectile.Opacity, Projectile.rotation + rotOff, tex.Size() / 2, Projectile.scale * 1.25f, fx, 0f);
        }

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, Projectile.rotation + rotOff, tex.Size() / 2, Projectile.scale, fx, 0f);

        return false;
    }
}
