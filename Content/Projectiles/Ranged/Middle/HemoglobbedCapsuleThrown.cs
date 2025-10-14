using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class HemoglobbedCapsuleThrown : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HemoglobbedCapsule);

    public enum BehaviorState
    {
        Aim,
        Fire
    }

    public BehaviorState CurrentState
    {
        get => (BehaviorState)Projectile.ai[0];
        set => Projectile.ai[0] = (int)value;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public ref float Time => ref Projectile.ai[1];

    public ref float CrimsonFormInterpolant => ref Projectile.ai[2];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 13;
    }

    public override void SetDefaults()
    {
        Projectile.width = 48;
        Projectile.height = 48;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.Opacity = 1f;
        Projectile.timeLeft = 14400;
        Projectile.penetrate = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 7;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.CritChance = 0;
    }

    public override void AI()
    {
        after ??= new(7, () => Projectile.Center);

        switch (CurrentState)
        {
            case BehaviorState.Aim:
                DoBehavior_Aim();
                break;
            case BehaviorState.Fire:
                DoBehavior_Fire();
                break;
        }

        Projectile.SetAnimation(13, 6);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 120, 4, 1f,
            AssetRegistry.GetTexture(AdditionsTexture.HemoglobbedCapsule).Frame(1, Main.projFrames[Type], 0, Projectile.frame), false, .15f));
        Time++;
    }

    public void DoBehavior_Aim()
    {
        Item heldItem = Owner.HeldItem;
        if (this.RunLocal() && (Owner.dead || !Owner.active || Owner.noItems || Owner.CCed || heldItem is null))
        {
            Projectile.Kill();
            return;
        }

        const int shootDelay = 90;
        float animationCompletion = InverseLerp(0f, shootDelay, Time);

        if (this.RunLocal())
        {
            float aimInterpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(Modded.mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Modded.mouseWorld), aimInterpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        float frontArmRotation = Projectile.rotation + MathHelper.PiOver2 - animationCompletion * Owner.direction * 0.64f;
        if (Owner.direction == 1)
            frontArmRotation += MathHelper.Pi;

        Owner.SetFrontHandBetter(0, frontArmRotation + MathHelper.PiOver2);
        Projectile.Center = Owner.Center + (frontArmRotation + MathHelper.PiOver2).ToRotationVector2() * 37f;
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);

        float interpolant = MathF.Sin(MathHelper.Pi * animationCompletion * 2.5f);
        CrimsonFormInterpolant = Animators.MakePoly(6f).InFunction(interpolant);

        if (animationCompletion < 0.99f && CrimsonFormInterpolant >= 0.99f)
        {
            for (int t = 0; t < 80; t++)
            {
                Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(40f, 40f);
                Vector2 vel = pos.SafeDirectionTo(Projectile.Center);
                Dust.NewDustPerfect(pos, DustID.CrimsonTorch, vel * Main.rand.NextFloat(5f, 7f), 0, default, Main.rand.NextFloat(2f, 3f)).noGravity = true;
            }

            SoundID.DD2_BetsyFireballShot.Play(Projectile.Center, .87f, -.38f);
        }

        // Create magic stuff when ready to fire
        if (Time == shootDelay)
        {
            SoundID.Item4.Play(Owner.Center, 1f, -.3f);

            int dustCount = 20;
            float angularOffset = Projectile.velocity.ToRotation();
            for (int i = 0; i < dustCount; i++)
            {
                Dust blood = Dust.NewDustPerfect(Projectile.Center, 267);
                blood.fadeIn = 1f;
                blood.noGravity = true;
                blood.alpha = 100;
                blood.color = Color.Lerp(Color.Red, Color.White, Main.rand.NextFloat(0.3f));
                if (i % 4 == 0)
                {
                    blood.velocity = angularOffset.ToRotationVector2() * 10.2f;
                    blood.scale = 3.3f;
                }
                else if (i % 2 == 0)
                {
                    blood.velocity = angularOffset.ToRotationVector2() * 7.8f;
                    blood.scale = 2.9f;
                }
                else
                {
                    blood.velocity = angularOffset.ToRotationVector2() * 5.4f;
                    blood.scale = 1.6f;
                }
                angularOffset += MathHelper.TwoPi / dustCount;
                blood.velocity += Projectile.velocity * Main.rand.NextFloat(0.5f);
            }
        }

        if (!Owner.channel)
        {
            Owner.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
            if (Time >= shootDelay)
            {
                Projectile.velocity *= heldItem.shootSpeed;
                for (int i = 0; i < 30; i++)
                {
                    Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(40f, 40f);
                    Vector2 vel = Projectile.velocity * Main.rand.NextFloat(.1f, .5f);
                    Dust.NewDustPerfect(pos, DustID.Blood, vel * Main.rand.NextFloat(1f, 1.2f), 0, default, Main.rand.NextFloat(2f, 3f)).noGravity = true;
                }
                SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Pitch = -.3f, Volume = 1.1f }, Projectile.Center);
                CurrentState = BehaviorState.Fire;
                Time = 0f;
                Projectile.netUpdate = true;

                return;
            }

            Projectile.Kill();
        }
    }

    public void DoBehavior_Fire()
    {
        if (Projectile.timeLeft > 200)
            Projectile.timeLeft = 200;

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 400, true, true), out NPC npc))
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(npc.Center) * 14f, .13f);

        Projectile.VelocityBasedRotation();
        Projectile.Opacity = InverseLerp(0f, 20f, Projectile.timeLeft);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 50; i++)
        {
            ParticleRegistry.SpawnDustParticle(Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.width / 2),
                Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(30, 40), Main.rand.NextFloat(.7f, 1.1f), Color.Crimson, Main.rand.NextFloat(-.1f, .1f), false, true, true);
            ParticleRegistry.SpawnBloodParticle(Projectile.RandAreaInEntity(), Main.rand.NextVector2Circular(10f, 10f), Main.rand.Next(30, 50), Main.rand.NextFloat(.7f, 1.2f), Color.DarkRed);
        }

        AdditionsSound.Rapture.Play(Projectile.Center, 1.2f, -.1f);
        ScreenShakeSystem.New(new(.6f, .6f, 3000f), Projectile.Center);
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.UnitY, ModContent.ProjectileType<LesserBloodBeacon>(), Projectile.damage / 2, 0f);
    }

    public override bool? CanDamage() => CurrentState == BehaviorState.Aim ? false : null;
    
    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Color baseColor = Projectile.GetAlpha(lightColor);

        Texture2D orbTexture = Projectile.ThisProjectileTexture();
        Rectangle frame = orbTexture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.Black, Color.DarkRed * 1.5f, Color.DarkRed], Projectile.Opacity);

        float backglowWidth = CrimsonFormInterpolant * 7f;
        if (backglowWidth <= 0.5f)
            backglowWidth = 0f;

        Color backglowColor = Color.IndianRed;
        backglowColor = Color.Lerp(backglowColor, Color.Red, Utils.GetLerpValue(0.7f, 1f, CrimsonFormInterpolant, true) * 0.56f) * 0.4f;
        backglowColor.A = 20;

        for (int i = 0; i < 10; i++)
        {
            Vector2 drawOffset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * backglowWidth;
            Main.spriteBatch.Draw(orbTexture, drawPosition + drawOffset, frame, backglowColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
        }

        Main.spriteBatch.Draw(orbTexture, drawPosition, frame, baseColor, Projectile.rotation, origin, Projectile.scale, direction, 0f);
        return false;
    }
}
