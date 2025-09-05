using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class HellsToothpickHeld : ModProjectile
{
    public enum State
    {
        Charge,
        Stab
    }
    private State CurrentState
    {
        get => (State)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public int Timer
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public ref float Held => ref Projectile.ai[2];

    public Item Item => Owner.HeldItem;
    public float CollisionWidth => 100f * Projectile.scale;

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HellsToothpick);

    public override void SetDefaults()
    {
        Projectile.width = 24;
        Projectile.height = 100;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 9999;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.MaxUpdates = 3;
        Projectile.localNPCHitCooldown = 8 * Projectile.MaxUpdates;
        Projectile.noEnchantmentVisuals = true;
        Projectile.ownerHitCheck = true;
    }

    public Player Owner => Main.player[Projectile.owner];

    public override void AI()
    {
        switch (CurrentState)
        {
            case State.Charge:
                BehaviorCharge();
                break;
            case State.Stab:
                BehaviorStab();
                break;
        }
        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

        float armPointingDirection = Owner.itemRotation;
        if (Owner.direction < 0)
            armPointingDirection += (float)Math.PI;
        Owner.SetFrontHandBetter(0, armPointingDirection);

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * Item.height * .5f;
        Lighting.AddLight(pos, Color.Lerp(Color.OrangeRed, Color.Red, (float)MathF.Sin(Main.GlobalTimeWrappedHourly * 2f)).ToVector3() * 1.2f);

        Timer++;
    }

    public int WaitUntilMax = SecondsToFrames(5);
    private void BehaviorCharge()
    {
        if (Held < WaitUntilMax)
            Held++;

        float animationCompletion = Utils.GetLerpValue(0f, WaitUntilMax, Held, true);
        Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * Item.height * .5f;

        if (Held == WaitUntilMax && Projectile.localAI[1] == 0f)
        {
            SoundEngine.PlaySound(SoundID.Item73, Projectile.position);

            float offsetAngle = RandomRotation();
            int amount = 16;
            for (int i = 0; i < amount; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / amount + offsetAngle).ToRotationVector2() * 2f;
                ParticleRegistry.SpawnGlowParticle(pos, shootVelocity, 60, 40f, Color.OrangeRed);
            }
            Projectile.localAI[1] = 1f;
        }
        if (Held >= WaitUntilMax && Timer % 2f == 0f)
        {
            Vector2 vel = Vector2.UnitY.RotatedByRandom(.15) * -Main.rand.NextFloat(1f, 3f);
            if (Main.rand.NextBool())
            {
                ParticleRegistry.SpawnGlowParticle(pos, vel, 25, 30.8f, Color.OrangeRed);
            }
            ParticleRegistry.SpawnGlowParticle(pos, vel, 40, Main.rand.NextFloat(24.2f, 32.4f), Color.OrangeRed, .5f);
        }

        if (this.RunLocal())
        {
            float aimInterpolant = Utils.GetLerpValue(5f, 25f, Owner.Distance(Owner.Additions().mouseWorld), true);
            Vector2 oldVelocity = Projectile.velocity;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.SafeDirectionTo(Owner.Additions().mouseWorld), aimInterpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Vector2 reel = Vector2.UnitY.RotatedBy(Projectile.rotation) * animationCompletion * 15f;
        Projectile.Center = Owner.MountedCenter + (Projectile.velocity * (Item.height * .5f)) + reel;

        if (!Owner.channel && CurrentState == State.Charge)
        {
            AdditionsSound.FireWhoosh1.Play(Projectile.Center, 1.1f, 0f, .2f, 0);
            CurrentState = State.Stab;
            Timer = 0;
            if (this.RunLocal())
                Projectile.velocity *= 18.4f;
            this.Sync();
        }
    }

    private void BehaviorStab()
    {
        const float StabDuration = 60f; // Defining the duration the projectile will exist in frames

        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.Zero); // Store attack direction
        float progress = GetLerpBump(0f, .4f, 1f, .6f, Projectile.timeLeft / StabDuration);

        // Move the projectile from the min to the max and back, using SmoothStep for easing the movement
        Projectile.Center = Owner.MountedCenter + Vector2.SmoothStep(Projectile.velocity * 30f, Projectile.velocity * 100f, progress);

        // Fade out the projectile toward the end
        Projectile.Opacity = Projectile.scale = InverseLerp(0f, .2f, Projectile.timeLeft / StabDuration);

        if (Projectile.timeLeft > StabDuration && this.RunLocal())
        {
            Projectile.timeLeft = (int)StabDuration;

            float animationCompletion = Utils.GetLerpValue(0f, WaitUntilMax, Held, true);
            float value = Utils.Remap(Held, 0f, WaitUntilMax, 1f, 4f, true);

            Vector2 vel = Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * 5f * value;

            Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * Item.height * .68f;
            int type = ModContent.ProjectileType<HellFlame>();
            int damage = (int)(Projectile.damage * value);
            int flame = Projectile.NewProj(pos, vel, type, damage, Projectile.knockBack, Projectile.owner);
            if (Main.projectile[flame].whoAmI.WithinBounds(Main.maxProjectiles))
            {
                Main.projectile[flame].ai[0] = animationCompletion;
                Main.projectile[flame].penetrate = Held <= 60 ? 1 : (int)(Held / 60f);
            }
        }
    }

    public override bool? CanHitNPC(NPC target) => CurrentState == State.Stab ? null : false;
    public override bool ShouldUpdatePosition() => false;

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Vector2 start = Projectile.Center;
        Vector2 end = start + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 10f;
        Utils.PlotTileLine(start, end, CollisionWidth, DelegateMethods.CutTiles);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 start = Projectile.Center;
        Vector2 end = start + Projectile.velocity * 6f;
        float useless = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref useless);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 origin = tex.Size() * .5f;
        SpriteEffects fx = Projectile.direction.ToSpriteDirection();
        Projectile.DrawProjectileBackglow(Color.Lerp(Color.OrangeRed, Color.Orange, Sin01(Main.GlobalTimeWrappedHourly)), Held / 80f, (byte)(90 * Projectile.Opacity), 8, fx);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, lightColor, Projectile.rotation, origin, Projectile.scale, fx);
        return false;
    }
}