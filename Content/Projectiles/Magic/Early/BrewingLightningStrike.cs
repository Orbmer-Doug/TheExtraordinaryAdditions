using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class BrewingLightningStrike : ModProjectile, ILocalizedModType, IModType
{
    public bool HasPlayedSound;
    public ref float InitialVelocityAngle => ref Projectile.ai[0];
    public ref float BaseTurnAngleRatio => ref Projectile.ai[1];
    public ref float AccumulatedXMovementSpeeds => ref Projectile.ai[2];
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 22;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.MaxUpdates = 3;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Projectile.MaxUpdates * 45;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = Vector2.Zero;
        return false;
    }

    private float StoredY;
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 50);

        Projectile.frameCounter++;
        if (!HasPlayedSound)
        {
            StoredY = ModdedOwner.mouseWorld.Y;
            HasPlayedSound = true;
        }
        if (Projectile.Center.Y > StoredY)
            Projectile.tileCollide = true;

        Projectile.oldPos[1] = Projectile.oldPos[0];
        float adjustedTimeLife = Projectile.timeLeft / Projectile.MaxUpdates;
        Projectile.Opacity = GetLerpBump(0f, 8f, 45f, 40f, adjustedTimeLife);
        Projectile.scale = Projectile.Opacity;
        Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * Projectile.Opacity);

        if (Projectile.frameCounter > Projectile.extraUpdates * 2)
        {
            Projectile.frameCounter = 0;

            // Make the lightning branch off in seperate ways
            float originalSpeed = MathHelper.Min(20f, Projectile.velocity.Length());
            UnifiedRandom unifiedRandom = new((int)BaseTurnAngleRatio);
            int turnTries = 0;
            Vector2 newBaseDirection = -Vector2.UnitY;
            Vector2 potentialBaseDirection;
            float lightningTurnRandomnessFactor = 8f;
            do
            {
                BaseTurnAngleRatio = unifiedRandom.Next() % 100;
                potentialBaseDirection = (BaseTurnAngleRatio / 100f * MathHelper.TwoPi).ToRotationVector2();

                // Ensure that the new potential direction base is always moving upwards (this is supposed to be somewhat similar to a -UnitY + RotatedBy).
                potentialBaseDirection.Y = -Math.Abs(potentialBaseDirection.Y);

                bool canChangeLightningDirection = true;

                // Potential directions with very little Y speed should not be considered, because this
                // consequentially means that the X speed would be quite large.
                if (potentialBaseDirection.Y > -0.02f)
                    canChangeLightningDirection = false;

                // This mess of math basically encourages movement at the ends of an extraUpdate cycle,
                // discourages super frequenent randomness as the accumulated X speed changes get larger,
                // or if the original speed is quite large.
                if (Math.Abs(potentialBaseDirection.X * (Projectile.extraUpdates + 1) * 2f * originalSpeed + AccumulatedXMovementSpeeds) > Projectile.MaxUpdates * lightningTurnRandomnessFactor)
                    canChangeLightningDirection = false;

                // If the above checks were all passed, redefine the base direction of the lightning.
                if (canChangeLightningDirection)
                    newBaseDirection = potentialBaseDirection;

                turnTries++;
            }
            while (turnTries < 60);

            if (Projectile.velocity != Vector2.Zero)
            {
                AccumulatedXMovementSpeeds += newBaseDirection.X * (Projectile.extraUpdates + 1) * 2f * originalSpeed;
                Projectile.velocity = newBaseDirection.RotatedBy(InitialVelocityAngle + MathHelper.PiOver2) * originalSpeed;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
        }

        cache ??= new(50);
        cache.Update(Projectile.Center);
    }

    public float WidthFunction(float completionRatio)
    {
        return Convert01To010(completionRatio) * Projectile.scale * Projectile.width;
    }

    public Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return MulticolorLerp(Sin01(Projectile.identity / 3f + completionRatio.X * 20f + Main.GlobalTimeWrappedHourly * 1.1f),
            Color.Purple, Color.MediumPurple, Color.AntiqueWhite) * Projectile.Opacity * GetLerpBump(0f, .05f, .95f, .15f, completionRatio.X);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Vector2.Zero, 9, 2.5f, Color.Pink, Color.White, 1.4f, .2f);
        Projectile.damage = (int)(Projectile.damage * .985f);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += .25f;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 1);
            trail.DrawTrail(shader, cache.Points, 50);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}