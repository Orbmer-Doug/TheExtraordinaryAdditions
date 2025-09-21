using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;

public class ObsidianMaceProj : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ObsidianMaceProj);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

    public enum MaceState
    {
        Spinning,
        LaunchingForward,
        Retracting,
        ForcedRetracting,
        Ricochet,
        Dropping
    }

    public ref float Time => ref Projectile.ai[0];
    public MaceState State
    {
        get => (MaceState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }
    public float Speed => Owner.GetTotalAttackSpeed(DamageClass.Melee);
    public ref float Spin => ref Projectile.Additions().ExtraAI[0];
    public int InitDir
    {
        get => (int)Projectile.Additions().ExtraAI[1];
        set => Projectile.Additions().ExtraAI[1] = value;
    }
    public bool Init
    {
        get => Projectile.Additions().ExtraAI[2] == 1f;
        set => Projectile.Additions().ExtraAI[2] = value.ToInt();
    }
    public int CollisionCounter
    {
        get => (int)Projectile.Additions().ExtraAI[3];
        set => Projectile.Additions().ExtraAI[3] = value;
    }

    public const int LaunchTimeLimit = 15;
    public float LaunchSpeed => 25f * Speed;
    public float MaxLaunchLength => 800f;
    public float RetractAcceleration => 3f * Speed;
    public float MaxRetractSpeed => 30f * Speed;
    public float ForcedRetractAcceleration => 6f * Speed;
    public float MaxForcedRetractSpeed => 40f * Speed;
    public const float TotalSpeedUpTime = 80f;

    public const int DefaultHitCooldown = 10;
    public const int SpinHitCooldown = 10;
    public const int MovingHitCooldown = 10;
    public const int RicochetTimeLimit = LaunchTimeLimit + 15;
    public float LaunchRange => LaunchSpeed * LaunchTimeLimit;
    public float MaxDroppedRange => LaunchRange + 160f;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(32, 30);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 4;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        if (!Owner.Available() || Vector2.Distance(Projectile.Center, Owner.Center) > 900f)
        {
            Projectile.Kill();
            return;
        }

        if (this.RunLocal() && Main.mapFullscreen)
        {
            Projectile.Kill();
            return;
        }

        if (!Init)
        {
            after ??= new(10, () => Projectile.Center);
            InitDir = Center.SafeDirectionTo(Modded.mouseWorld).X.NonZeroSign();
            Init = true;
            this.Sync();
        }

        Vector2 mountedCenter = Owner.MountedCenter;
        bool shouldOwnerHitCheck = false;

        Projectile.localNPCHitCooldown = DefaultHitCooldown;
        Projectile.tileCollide = State != MaceState.Spinning;

        switch (State)
        {
            case MaceState.Spinning:
                {
                    shouldOwnerHitCheck = true;
                    if (this.RunLocal())
                    {
                        Vector2 unitVectorTowardsMouse = mountedCenter.DirectionTo(Modded.mouseWorld).SafeNormalize(Vector2.UnitX * Owner.direction);
                        Owner.ChangeDir(InitDir);

                        if (!Owner.channel)
                        {
                            SoundID.Item1.Play(Projectile.Center, .9f, -.4f);
                            State = MaceState.LaunchingForward;
                            Time = 0f;
                            Projectile.velocity = unitVectorTowardsMouse * LaunchSpeed + Owner.velocity;
                            Projectile.Center = mountedCenter;
                            this.Sync();
                            Projectile.ResetLocalNPCHitImmunity();
                            Projectile.localNPCHitCooldown = MovingHitCooldown;
                            break;
                        }
                    }

                    Spin = (Spin + (Utils.Remap(Time, 0f, TotalSpeedUpTime / Speed, 1f, 4f * Speed))) % TotalSpeedUpTime;
                    float theta = Utils.Remap(Spin, InitDir < 0f ? TotalSpeedUpTime : 0f, InitDir < 0f ? 0f : TotalSpeedUpTime, 0f, MathHelper.TwoPi);
                    Projectile.Center = Owner.GetFrontHandPositionImproved() + Utility.GetPointOnRotatedEllipse(150f, 80f, InitDir == -1 ? MathHelper.Pi : 0f, theta);

                    if (Main.rand.NextBool())
                        ParticleRegistry.SpawnGlowParticle(Projectile.Center, (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(2f, 5f),
                            Main.rand.Next(30, 50), Main.rand.NextFloat(20f, 40f), Color.DarkViolet, .6f);

                    Projectile.localNPCHitCooldown = SpinHitCooldown; // set the hit speed to the spinning hit speed
                    Time++;
                    this.Sync();
                    break;
                }

            case MaceState.LaunchingForward:
                {
                    bool shouldSwitchToRetracting = Time++ >= LaunchTimeLimit;
                    shouldSwitchToRetracting |= Projectile.Distance(mountedCenter) >= MaxLaunchLength;
                    if (Owner.controlUseItem)
                    {
                        State = MaceState.Dropping;
                        Time = 0f;
                        Projectile.netUpdate = true;
                        Projectile.velocity *= 0.2f;
                        break;
                    }

                    if (shouldSwitchToRetracting)
                    {
                        State = MaceState.Retracting;
                        Time = 0f;
                        Projectile.netUpdate = true;
                        Projectile.velocity *= 0.3f;
                    }

                    Owner.ChangeDir((Owner.Center.X < Projectile.Center.X).ToDirectionInt());
                    Projectile.localNPCHitCooldown = MovingHitCooldown;

                    Vector2 pos = Projectile.RandAreaInEntity();
                    Vector2 vel = -Projectile.velocity * Main.rand.NextFloat(.1f, .4f);
                    Color col = Color.Violet.Lerp(Color.DarkViolet, Main.rand.NextFloat());
                    ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(30, 50), Main.rand.NextFloat(.5f, 1.1f), col, Color.White.Lerp(Color.Purple, .4f), null, 1.4f, 3);
                    break;
                }

            case MaceState.Retracting:
                {
                    Vector2 unitVectorTowardsOwner = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
                    if (Projectile.Distance(mountedCenter) <= MaxRetractSpeed)
                    {
                        Projectile.Kill();
                        return;
                    }

                    if (Owner.controlUseItem)
                    {
                        State = MaceState.Dropping;
                        Time = 0f;
                        Projectile.netUpdate = true;
                        Projectile.velocity *= 0.2f;
                    }
                    else
                    {
                        Projectile.velocity *= 0.98f;
                        Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsOwner * MaxRetractSpeed, RetractAcceleration);
                        Owner.ChangeDir((Owner.Center.X < Projectile.Center.X).ToDirectionInt());
                    }
                    break;
                }

            case MaceState.ForcedRetracting:
                {
                    Projectile.tileCollide = false;
                    Vector2 unitVectorTowardsOwner = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
                    if (Projectile.Distance(mountedCenter) <= MaxForcedRetractSpeed)
                    {
                        Projectile.Kill();
                        return;
                    }

                    Projectile.velocity *= 0.98f;
                    Projectile.velocity = Projectile.velocity.MoveTowards(unitVectorTowardsOwner * MaxForcedRetractSpeed, ForcedRetractAcceleration);
                    Vector2 target = Projectile.Center + Projectile.velocity;
                    Vector2 value = mountedCenter.DirectionFrom(target).SafeNormalize(Vector2.Zero);
                    if (Vector2.Dot(unitVectorTowardsOwner, value) < 0f)
                    {
                        Projectile.Kill();
                        return;
                    }

                    Owner.ChangeDir((Owner.Center.X < Projectile.Center.X).ToDirectionInt());
                    break;
                }

            case MaceState.Ricochet:
                if (Time++ >= RicochetTimeLimit)
                {
                    State = MaceState.Dropping;
                    Time = 0f;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.localNPCHitCooldown = MovingHitCooldown;
                    Projectile.velocity.Y += 0.6f;
                    Projectile.velocity.X *= 0.95f;
                    Owner.ChangeDir((Owner.Center.X < Projectile.Center.X).ToDirectionInt());
                }
                break;

            case MaceState.Dropping:
                if (!Owner.controlUseItem || Projectile.Distance(mountedCenter) > MaxDroppedRange)
                {
                    State = MaceState.ForcedRetracting;
                    Time = 0f;
                    Projectile.netUpdate = true;
                }
                else
                {
                    Projectile.velocity.Y += 0.8f;
                    Projectile.velocity.X *= 0.95f;
                    Owner.ChangeDir((Owner.Center.X < Projectile.Center.X).ToDirectionInt());
                }
                break;
        }

        Projectile.direction = (Projectile.velocity.X > 0f).ToDirectionInt();
        Projectile.spriteDirection = Projectile.direction;
        Projectile.ownerHitCheck = shouldOwnerHitCheck;
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 0, 0f, null, true, .3f));

        bool freeRotation = State == MaceState.Ricochet || State == MaceState.Dropping;
        if (freeRotation)
        {
            if (Projectile.velocity.Length() > 1f)
                Projectile.rotation = Projectile.velocity.ToRotation() + Projectile.velocity.X * 0.1f; // skid
            else
                Projectile.rotation += Projectile.velocity.X * 0.1f; // roll
        }
        else
        {
            Vector2 vectorTowardsOwner = Projectile.DirectionTo(mountedCenter).SafeNormalize(Vector2.Zero);
            Projectile.rotation = vectorTowardsOwner.ToRotation() + MathHelper.PiOver2;
        }

        Projectile.timeLeft = 2;
        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);

        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, freeRotation ? Center.AngleTo(Projectile.Center) : Projectile.rotation + MathHelper.PiOver2);
    }

    public override bool? CanDamage() => Projectile.Opacity >= 1f ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 velocity = Projectile.velocity;

        // The steps after this are not relevant for spinning
        // Also prevent massive damage by dropping on top of enemy
        if (State == MaceState.Spinning || State == MaceState.Dropping || State == MaceState.Ricochet)
            return;

        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Projectile.velocity, ModContent.ProjectileType<ObsidianPow>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        State = MaceState.Ricochet;

        // Has hit target and rebound off it
        float bounceFactor = 0.2f;
        if (State == MaceState.LaunchingForward || State == MaceState.Ricochet)
        {
            bounceFactor = 0.4f;
        }
        if (State == MaceState.LaunchingForward)
            Projectile.position -= velocity;

        Projectile.velocity.X = (0f - Projectile.velocity.X) * bounceFactor;
        CollisionCounter += 1;

        Projectile.velocity.Y = (0f - Projectile.velocity.Y) * bounceFactor;
        CollisionCounter += 1;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        int defaultLocalNPCHitCooldown = 10;
        int impactIntensity = 0;
        Vector2 velocity = Projectile.velocity;

        float bounceFactor = 0.2f;

        if (State == MaceState.LaunchingForward || State == MaceState.Ricochet)
            bounceFactor = 0.4f;

        if (State == MaceState.Dropping)
            bounceFactor = 0f;

        if (oldVelocity.X != Projectile.velocity.X)
        {
            if (Math.Abs(oldVelocity.X) > 4f)
            {
                impactIntensity = 1;
            }

            Projectile.velocity.X = (0f - oldVelocity.X) * bounceFactor;
            CollisionCounter += 1;
        }

        if (oldVelocity.Y != Projectile.velocity.Y)
        {
            if (Math.Abs(oldVelocity.Y) > 4f)
            {
                impactIntensity = 1;
            }

            Projectile.velocity.Y = (0f - oldVelocity.Y) * bounceFactor;
            CollisionCounter += 1;
        }

        if (State == MaceState.LaunchingForward)
        {
            State = MaceState.Ricochet;
            impactIntensity = 2;
            Projectile.netUpdate = true;

            for (int i = 0; i < 24; i++)
            {
                ParticleRegistry.SpawnSparkParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.5f) * Main.rand.NextFloat(.5f, .9f),
                    Main.rand.Next(24, 34), Main.rand.NextFloat(.5f, .9f), new(25, 35, 58));
            }

            Projectile.localNPCHitCooldown = defaultLocalNPCHitCooldown;
            Projectile.position -= velocity;
        }

        if (impactIntensity > 0 && !Main.dedServ)
        {
            for (int i = 0; i < impactIntensity; i++)
            {
                Collision.HitTiles(Projectile.position, velocity, Projectile.width * 2, Projectile.height * 2);
            }

            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            Projectile.netUpdate = true;
        }

        // Force retraction if stuck on tiles while retracting
        if (State != MaceState.Spinning && State != MaceState.Ricochet && State != MaceState.Dropping && CollisionCounter >= 10f)
        {
            State = MaceState.ForcedRetracting;
            Projectile.netUpdate = true;
        }

        return false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (State == MaceState.Spinning)
        {
            modifiers.SourceDamage *= 1.2f;
        }

        else if (State == MaceState.LaunchingForward || State == MaceState.Retracting)
        {
            modifiers.SourceDamage *= 2f;
        }

        modifiers.HitDirectionOverride = (Owner.Center.X < target.Center.X).ToDirectionInt();

        if (State == MaceState.Spinning)
        {
            modifiers.Knockback *= 0.45f;
        }

        else if (State == MaceState.Dropping)
        {
            modifiers.Knockback *= 0.5f;
        }
    }

    public FancyAfterimages after;
    public void DrawChain()
    {
        Vector2 hand = Owner.GetFrontHandPositionImproved();
        Texture2D chain = AssetRegistry.GetTexture(AdditionsTexture.ObsidianChain);
        Texture2D chain2 = AssetRegistry.GetTexture(AdditionsTexture.ObsidianChainAlt);

        float chainHeightAdjustment = 0f;

        Vector2 chainOrigin = chain.Size() / 2f;
        Vector2 chainDrawPosition = Projectile.Center;
        Vector2 projDirToHand = hand.MoveTowards(chainDrawPosition, 4f) - chainDrawPosition;
        Vector2 normalized = projDirToHand.SafeNormalize(Vector2.Zero);
        float chainSegmentLength = chain.Height + chainHeightAdjustment;
        float chainRotation = normalized.ToRotation() + MathHelper.PiOver2;
        int chainCount = 0;
        float chainLengthRemainingToDraw = projDirToHand.Length() + chainSegmentLength / 2f;

        while (chainLengthRemainingToDraw > 0f)
        {
            Color chainDrawColor = Lighting.GetColor((int)chainDrawPosition.X / 16, (int)(chainDrawPosition.Y / 16f));

            Texture2D chainTextureToDraw = chain;
            if (chainCount >= 4)
            {
            }
            else
            {
                // Close to the ball
                chainTextureToDraw = chain2;
                chainDrawColor = Color.White;
            }

            Main.spriteBatch.Draw(chainTextureToDraw, chainDrawPosition - Main.screenPosition, null, chainDrawColor, chainRotation, chainOrigin, 1f, SpriteEffects.None, 0f);

            chainDrawPosition += normalized * chainSegmentLength;
            chainCount++;
            chainLengthRemainingToDraw -= chainSegmentLength;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawChain();
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.DarkViolet], Projectile.Opacity);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }
}

public class ObsidianPow : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(90);
        Projectile.friendly = Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 4;
        Projectile.timeLeft = 2;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Melee;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Projectile.ai[0] == 0f)
        {
            SoundID.DD2_ExplosiveTrapExplode.Play(Projectile.Center, .9f, -.2f, .1f, null, 10, Name);
            for (int i = 0; i < 30; i++)
            {
                Vector2 vel = NextVector2Ellipse(90f * .3f, 90f, Projectile.velocity.ToRotation());
                ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel * .2f, Main.rand.Next(30, 40), Main.rand.NextFloat(.5f, 1f), Color.Violet);
            }
            for (int i = 0; i < 3; i++)
                ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 30, Projectile.velocity.ToRotation(), new(.3f, 1f), 0f, 90f, Color.DarkViolet);

            Projectile.ai[0] = 1f;
        }
    }
}