using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
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
    public ref float InitialVelocityAngle => ref Projectile.ai[0];
    public ref float BaseTurnAngleRatio => ref Projectile.ai[1];
    public ref float AccumulatedXMovementSpeeds => ref Projectile.ai[2];
    public ref float StoredY => ref Projectile.Additions().ExtraAI[0];
    public bool Init
    {
        get => Projectile.Additions().ExtraAI[1] == 1;
        set => Projectile.Additions().ExtraAI[1] = value.ToInt();
    }
    
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(22);
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

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.tileCollide);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.tileCollide = reader.ReadBoolean();
    }

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 50);

        Projectile.frameCounter++;
        if (!Init)
        {
            if (this.RunLocal())
            {
                StoredY = ModdedOwner.mouseWorld.Y;
            }
            Init = true;
            this.Sync();
        }
        if (Projectile.Center.Y > StoredY && !Projectile.tileCollide)
        {
            Projectile.tileCollide = true;
            this.Sync();
        }

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

                potentialBaseDirection.Y = -Math.Abs(potentialBaseDirection.Y);

                bool canChangeLightningDirection = true;

                if (potentialBaseDirection.Y > -0.02f)
                    canChangeLightningDirection = false;

                if (Math.Abs(potentialBaseDirection.X * (Projectile.extraUpdates + 1) * 2f * originalSpeed + AccumulatedXMovementSpeeds) > Projectile.MaxUpdates * lightningTurnRandomnessFactor)
                    canChangeLightningDirection = false;

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

    public TrailPoints cache = new(50);
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