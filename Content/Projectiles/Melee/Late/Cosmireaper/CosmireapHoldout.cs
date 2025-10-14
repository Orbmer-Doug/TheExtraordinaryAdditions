using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Cosmireaper;

public class CosmireapHoldout : ModProjectile
{
    #region Definitions
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Cosmireaper_Proj);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public enum States
    {
        Sweep,
        Acceleration,
        Impact,
        Force,
    }

    public int Direction => Projectile.velocity.X.NonZeroSign();
    public const float MaxSwingTime = 30f;
    public const int ChargeupTime = 120;
    public const float ThrowOutTime = 105f;
    public const float ThrowOutDistance = 1040f;
    public const float SnapPoint = 0.45f;
    public const float RetractionPoint = 0.6f;
    private const float TimeBeforeTeleport = 45f;
    public float FadeoutCompletion => InverseLerp(0f, TimeBeforeTeleport - 15f, Timer);
    public float SnapTimer => ThrowTimer / ThrowOutTime < SnapPoint ? 0 : (ThrowTimer / ThrowOutTime - SnapPoint) / (1f - SnapPoint);
    public float ThrowTimer => ThrowOutTime - Projectile.timeLeft;
    public float RetractionTimer => ThrowTimer / ThrowOutTime < RetractionPoint ? 0 : (ThrowTimer / ThrowOutTime - RetractionPoint) / (1f - RetractionPoint);
    public float SwingTimer => MaxSwingTime - Projectile.timeLeft;
    internal float ArmAnticipationMovement => MakePoly(2).OutFunction(ChargeProgress) * -(MathHelper.PiOver2 + .4f);
    internal float SwingRatio => MakePoly(4).InOutFunction.Evaluate(SwingTimer, 0f, MaxSwingTime, -(MathHelper.PiOver2 + .4f), MathHelper.PiOver2 + .56f);
    internal float ReleaseRatio => MakePoly(5).InFunction.Evaluate(Timer, 0f, MaxSwingTime, -(MathHelper.PiOver2 + .4f), 0f);
    public float ThrowCompletion => (ThrowOutTime - Projectile.timeLeft) / ThrowOutTime;
    internal float ThrowCurve() => new PiecewiseCurve()
        .Add(0f, 1f, SnapPoint, Circ.OutFunction)
        .Add(1f, 1f, RetractionPoint, MakePoly(1).InOutFunction)
        .Add(1f, 0f, 1f, MakePoly(4.5f).OutFunction).Evaluate(ThrowCompletion);


    public float ChargeProgress => InverseLerp(0f, ChargeupTime, Timer);
    public States State
    {
        get => (States)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public ref float Timer => ref Projectile.ai[1];
    public ref float Counter => ref Projectile.ai[2];

    public ref float CurrentVelocity => ref Projectile.AdditionsInfo().ExtraAI[0];
    public bool Returning
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }

    public bool HasHitTarget
    {
        get => Projectile.AdditionsInfo().ExtraAI[2] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }

    public bool Released
    {
        get => Projectile.AdditionsInfo().ExtraAI[3] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[3] = value.ToInt();
    }

    public bool ChargeComplete
    {
        get => Projectile.AdditionsInfo().ExtraAI[4] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[4] = value.ToInt();
    }

    public bool ReleasedInit
    {
        get => Projectile.AdditionsInfo().ExtraAI[5] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[5] = value.ToInt();
    }

    public bool ImpactInit
    {
        get => Projectile.AdditionsInfo().ExtraAI[6] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[6] = value.ToInt();
    }

    public ref float NPCIndex => ref Projectile.AdditionsInfo().ExtraAI[7];
    public NPC Target => Main.npc[(int)NPCIndex];
    public ref float TotalTime => ref Projectile.AdditionsInfo().ExtraAI[8];

    private Vector2 dir;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(dir);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        dir = reader.ReadVector2();
    }

    public RotatedRectangle Rect => new(Projectile.TopLeft, new(Projectile.width, Projectile.height), Projectile.rotation + Correction);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 6;
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        Main.projFrames[Type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 92;
        Projectile.height = 118;
        Projectile.penetrate = -1;
        Projectile.Opacity = 0f;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.timeLeft = 99999;
        Projectile.noEnchantmentVisuals = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
    }
    #endregion

    #region AI
    public override bool? CanHitNPC(NPC target)
    {
        if (!Released)
            return false;

        if (State == States.Sweep)
            return null;

        if (State == States.Acceleration)
            return null;

        if (State == States.Impact)
            return Timer > MaxSwingTime ? null : false;

        if (State == States.Force)
            return ThrowCompletion > .5f ? null : false;

        return false;
    }

    public float Correction => .89f * Projectile.velocity.X.NonZeroSign();
    public override void AI()
    {
        // ONLY die if player stops using while reeling
        if ((!Owner.channel || !Owner.Available()) && ChargeProgress < 1f && !Released)
        {
            Projectile.Kill();
            return;
        }
        TotalTime++;
        Owner.heldProj = Projectile.whoAmI;
        Projectile.Opacity = InverseLerp(0f, 10f, TotalTime);

        // Set reeling times
        if (Owner.channel)
        {
            Vector2 runePos = Rect.Center + (PolarVector(26f * Direction, Projectile.rotation - Correction) + PolarVector(16f, Projectile.rotation - Correction - MathHelper.PiOver2)) * Direction;

            // Allow changing of modes
            if (this.RunLocal() && Modded.SafeMouseRight.JustPressed)
            {
                Projectile.frame = (Projectile.frame + 1) % 4;

                ParticleRegistry.SpawnSparkleParticle(runePos, Vector2.Zero, 10, Main.rand.NextFloat(1.7f, 2.8f), Color.White, Color.DarkViolet, 1.5f, Main.rand.NextFloat(-.1f, .2f));

                AdditionsSound.HeatTail.Play(Projectile.Center, 1.2f, 0f, .1f, 1, Name);

                State += 1;
                this.Sync();
            }
            if (State > States.Force)
                State = 0;

            if (Projectile.ai[0] < 0)
                State = 0;
            Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

            if (this.RunLocal())
            {
                Projectile.velocity = center.SafeDirectionTo(Modded.mouseWorld);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
            Projectile.spriteDirection = (int)(Projectile.velocity.X.NonZeroSign() == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None);
            float armRotation = Projectile.velocity.ToRotation() + ArmAnticipationMovement * Projectile.velocity.X.NonZeroSign();

            float num = armRotation * Owner.gravDir;
            Projectile.Center = center + PolarVector(80f, num) * Owner.gravDir;

            Projectile.rotation = armRotation + Correction * Owner.gravDir;
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
            Owner.SetCompositeArmFront(true, 0, Projectile.rotation - MathHelper.PiOver2);

            // Ready for attacks
            if (ChargeProgress == 1f && ChargeComplete == false)
            {
                for (int i = 0; i < 25; i++)
                {
                    ParticleRegistry.SpawnSquishyPixelParticle(runePos, Main.rand.NextVector2Circular(8f, 8f),
                        Main.rand.Next(80, 100), Main.rand.NextFloat(1.2f, 1.8f), Color.BlueViolet, Color.Violet, 5, false, false, .14f);
                }
                AdditionsSound.MagicSwing.Play(runePos);
                ChargeComplete = true;
                this.Sync();
            }

            // Indicators
            if (ChargeProgress == 1f)
            {
                switch (State)
                {
                    case States.Acceleration:
                        {
                            if (Timer % 2f == 1f)
                            {
                                Vector2 pos = Rect.RandomPoint();
                                Color color = Color.Lerp(Color.DarkOliveGreen, Color.OliveDrab, Main.rand.NextFloat());
                                float size = Main.rand.NextFloat(.4f, .6f);
                                int life = Main.rand.Next(18, 25);
                                Vector2 vel = -Vector2.UnitY.RotatedByRandom(.25f) * Main.rand.NextFloat(5f, 15f);
                                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, size, color, color * 1.9f);
                            }

                            break;
                        }
                    case States.Impact:
                        {
                            Vector2 pos = Rect.RandomPoint();
                            Vector2 shootVelocity = RandomVelocity(3f, 1f, 4f);
                            Color col1 = Color.DarkRed;
                            Color col2 = Color.MediumVioletRed;
                            float scale = Main.rand.NextFloat(40f, 65f);
                            ParticleRegistry.SpawnCloudParticle(pos, shootVelocity, col2, col1, Main.rand.Next(40, 50), scale, .8f);

                            break;
                        }
                    case States.Force:
                        {
                            if (Timer % 3f == 2f)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    Vector2 randPos = runePos + Main.rand.NextVector2CircularLimited(140f, 140f, .5f, 1.1f);
                                    Vector2 velocity = randPos.SafeDirectionTo(runePos + Projectile.velocity) * Main.rand.NextFloat(7f, 9f);
                                    Color color = Color.Lerp(Color.Goldenrod, Color.Yellow, Main.rand.NextFloat(.2f, 1f));
                                    float scale = Main.rand.NextFloat(.3f, .5f);
                                    int life = Main.rand.Next(24, 35);

                                    ParticleRegistry.SpawnSparkParticle(randPos, velocity, life, scale, color, false, false, runePos);
                                }
                            }

                            break;
                        }
                    case States.Sweep:
                        {
                            if (Timer % 8f == 7f)
                            {
                                Vector2 pos = Projectile.RandAreaInEntity();
                                ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, 40, RandomRotation(), new(1f, Main.rand.NextFloat(.75f, 1f)), 0f, 50f, Color.BlueViolet);
                            }

                            break;
                        }
                }
            }

            Timer++;
            return;
        }
        Projectile.spriteDirection = (int)(Projectile.velocity.X.NonZeroSign() == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None);

        if (ReleasedInit == false)
        {
            Timer = Counter = 0f;
            Released = true;
            ReleasedInit = true;
            this.Sync();
        }

        // Kill if too far
        if (Vector2.Distance(Projectile.Center, Owner.Center) > Main.LogicCheckScreenWidth * 2)
            Projectile.Kill();

        switch (State)
        {
            case States.Acceleration:
                {
                    DoPhaseAcceleration();
                    break;
                }
            case States.Impact:
                {
                    DoPhaseImpact();
                    break;
                }
            case States.Force:
                {
                    DoPhaseForce();
                    break;
                }
            case States.Sweep:
                {
                    DoPhaseSweep();
                    break;
                }
        }
        Timer++;
    }

    internal void DoPhaseAcceleration()
    {
        if (Timer == 0f)
        {
            Owner.mount.Dismount(Owner);
            Owner.RemoveAllGrapplingHooks();

            CurrentVelocity = 40f;
            Projectile.tileCollide = true;
            if (this.RunLocal())
                Projectile.velocity = Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * 30f;

            AdditionsSound.etherealSwordSwoosh.Play(Projectile.Center, 2f, 0f, .2f);
            this.Sync();
        }
        CurrentVelocity *= .99f;
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * CurrentVelocity, .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.Opacity = InverseLerp(9.5f, 14f, CurrentVelocity);

        // Spawn trails on the sides
        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = i % 2 == 1 ? Rect.Top : Rect.Bottom;

            ParticleRegistry.SpawnSparkParticle(pos, -Projectile.velocity * .4f, 12, Main.rand.NextFloat(.4f, .8f), Color.BlueViolet);
            ParticleRegistry.SpawnSparkleParticle(pos, -Projectile.velocity.RotatedByRandom(.25f) * .6f, 20, Main.rand.NextFloat(.6f, .8f), Color.Blue, Color.Cyan);
        }

        bool worldEdge = Projectile.Center.X < 1000f || Projectile.Center.Y < 1000f || Projectile.Center.X > Main.maxTilesX * 16 - 1000 || Projectile.Center.Y > Main.maxTilesY * 16 - 1000;
        if (CurrentVelocity < 9.5f || worldEdge)
        {
            Owner.fullRotation = 0;
            Projectile.Kill();
            Projectile.netUpdate = true;
            return;
        }

        // Hold out hand
        Owner.SetFrontHandBetter(0, Owner.AngleTo(Projectile.Center));
        Owner.SetDummyItemTime(2);

        // Rotate and set the player to it
        Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.008f;
        Owner.Center = Projectile.Center;
        Owner.fullRotationOrigin = Owner.Center - Owner.position;
        Owner.fullRotation = Projectile.rotation;
        Owner.direction = Projectile.direction;
        Owner.bodyFrame.Y = Owner.bodyFrame.Height;
    }

    internal void DoPhaseImpact()
    {
        if (HasHitTarget)
        {
            if (Target is null || Target.active == false)
                Projectile.Kill();

            if (Timer < TimeBeforeTeleport - 15f)
            {
                Projectile.rotation += .2f * Direction * InverseLerp(TimeBeforeTeleport - 15f, 0f, Timer);
                if (this.RunLocal())
                    Projectile.velocity *= .9f;
                Projectile.Opacity = 1f - FadeoutCompletion;
                Projectile.friendly = false;
            }
            else if (Timer == TimeBeforeTeleport)
            {
                Projectile.penetrate = 1;
                Projectile.Opacity = 1f;
                Projectile.friendly = true;
                Projectile.Center = Target.Center + Target.Size * .5f + Main.rand.NextVector2CircularEdge(700f, 700f);
                AdditionsSound.etherealNuhUh.Play(Projectile.Center, 1f, 0f, .2f);
                if (this.RunLocal())
                    Projectile.NewProj(Projectile.Center, Projectile.SafeDirectionTo(Target.Center + Target.velocity), ModContent.ProjectileType<Cosmiportal>(), 0, 0f, Projectile.owner);
                Projectile.extraUpdates = 2;
                this.Sync();
            }
            else if (Timer > TimeBeforeTeleport)
            {
                if (this.RunLocal())
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center + Target.velocity) * 25f, .5f);
                    if (Projectile.velocity != Projectile.oldVelocity)
                        this.Sync();
                }
                Projectile.rotation += .3f * Direction;
            }
        }
        else
        {
            if (Timer < MaxSwingTime)
            {
                Owner.SetDummyItemTime(2);
                Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
                Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
                float armRotation = Projectile.velocity.ToRotation() + ReleaseRatio * Projectile.velocity.X.NonZeroSign();
                float num = armRotation * Owner.gravDir;
                Projectile.Center = center + PolarVector(80f, num) * Owner.gravDir;
                Projectile.rotation = armRotation + Correction * Owner.gravDir;
                Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
            }
            else if (Timer >= MaxSwingTime && ImpactInit == false)
            {
                Projectile.timeLeft = 300;
                NPCIndex = 0;
                if (this.RunLocal())
                    Projectile.velocity = Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * 30f;
                AdditionsSound.etherealThrow.Play(Projectile.Center, 1.1f);
                ImpactInit = true;
                this.Sync();
            }
            else
            {
                if (Counter % 3f == 2f)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Color color = Color.Lerp(Color.Lerp(Color.Violet, Color.DarkViolet, Main.rand.NextFloat()), Color.Crimson, Main.rand.NextFloat(.3f, .8f));
                        int life = Main.rand.Next(20, 30);
                        float scale = Main.rand.NextFloat(.4f, .6f);
                        Vector2 pos = Projectile.RandAreaInEntity();
                        Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.25f) * Main.rand.NextFloat(4f, 20f);
                        ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * Main.rand.NextFloat(.75f, 1f), life, scale, color, 1f, true);
                    }
                }
                Projectile.rotation += .25f * Direction;
            }
        }
        Counter++;
    }

    internal void DoPhaseForce()
    {
        if (Timer == 0f)
        {
            after ??= new(ProjectileID.Sets.TrailCacheLength[Type], () => Projectile.Center);
            Returning = false;
            if (this.RunLocal())
                dir = Owner.SafeDirectionTo(Owner.Additions().mouseWorld);
            Projectile.timeLeft = (int)ThrowOutTime + 1;

            AdditionsSound.etherealRelease.Play(Projectile.Center, 1.4f, 0f, .2f);
            this.Sync();
        }

        Projectile.Center = Owner.Center + dir * Projectile.scale * 10 + dir * ThrowOutDistance * ThrowCurve();
        after?.UpdateFancyAfterimages(new(Projectile.Center, Projectile.scale * Vector2.One, Projectile.Opacity, Projectile.rotation,
            (SpriteEffects)Projectile.spriteDirection, 0, 3, 3f, Projectile.ThisProjectileTexture().Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame), false, -.1f));

        // Hold hand out to projectile, cause chains
        Owner.SetFrontHandBetter(0, Owner.AngleTo(Projectile.Center));
        Owner.SetDummyItemTime(2);

        // Face the projectile
        Owner.ChangeDir(Owner.direction = !(Projectile.Center.X < Owner.Center.X) ? 1 : -1);

        if (ThrowCompletion > .5f)
        {
            Returning = true;

            int type = ModContent.ProjectileType<LaceratedSpace>();
            if (Counter == 0f)
            {
                if (this.RunLocal())
                    Projectile.NewProj(Projectile.Center, Owner.Center.SafeDirectionTo(Projectile.Center), type, Projectile.damage / 2, 0f, Owner.whoAmI);
                AdditionsSound.etherealSplit.Play(Projectile.Center, 2f, -.1f);
                Counter = 1f;
                this.Sync();
            }

            Projectile laceration = null;
            if (Utility.FindProjectile(out laceration, type, Owner.whoAmI))
            {
                laceration.ai[0] = 1f;
                laceration.As<LaceratedSpace>().Start = Owner.Center + dir * ThrowOutDistance;
                laceration.As<LaceratedSpace>().End = Projectile.Center;
                laceration.netUpdate = true;

                Rectangle hitbox = Projectile.Hitbox;
                if (hitbox.Intersects(Owner.Hitbox))
                {
                    laceration.ai[0] = 0f;
                    laceration.netUpdate = true;
                    Projectile.Kill();
                }

                Projectile.rotation = Projectile.AngleTo(Owner.Center) - MathHelper.PiOver2;
                Projectile.extraUpdates = 2;
                this.Sync();
            }
        }
        else
        {
            Projectile.rotation += .25f;
        }
    }

    internal void DoPhaseSweep()
    {
        if (Timer == 0f)
        {
            AdditionsSound.smallswing.Play(Projectile.Center, 1.1f);
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        if (Projectile.direction == -1)
            Projectile.rotation += MathHelper.Pi;
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);

        float armRotation = Projectile.velocity.ToRotation() + SwingRatio * Projectile.velocity.X.NonZeroSign();
        float num = armRotation * Owner.gravDir;
        Projectile.Center = center + PolarVector(80f, num) * Owner.gravDir;

        Projectile.rotation = armRotation + Correction * Owner.gravDir;
        Owner.SetCompositeArmFront(true, 0, Projectile.rotation - MathHelper.PiOver2);
        Owner.SetDummyItemTime(2);
        Projectile.Opacity = InverseLerp(0f, 9f, Projectile.timeLeft);

        if (Projectile.timeLeft > MaxSwingTime)
            Projectile.timeLeft = (int)MaxSwingTime;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Rect.Intersects(targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        switch (State)
        {
            case States.Acceleration:
                {
                    (Main.rand.NextBool() ? AdditionsSound.etherealSharpImpact : AdditionsSound.etherealSharpImpactB).Play(Projectile.Center, 1.2f, -.2f, .1f, 1, Name);
                    if (Owner.immuneTime <= 10)
                    {
                        Owner.immuneNoBlink = true;
                        Owner.immuneTime = 10;
                    }

                    Vector2 splatterDirection = Projectile.velocity / 3;
                    for (int i = 0; i < 70; i++)
                    {
                        Vector2 sparkVelocity = splatterDirection.RotatedByRandom(0.45) * Main.rand.NextFloat(.6f, 1.2f);
                        sparkVelocity.Y += Main.rand.NextFloat(-2f, 2f);
                        if (Main.netMode != NetmodeID.Server)
                        {
                            // Create the center
                            ShaderParticleRegistry.SpawnCosmicParticle(target.Center + Utils.NextVector2Circular(Main.rand, 30f, 30f), Utils.NextVector2Circular(Main.rand, 3f, 3f), 60f);

                            // Creates a seam
                            float scale = MathHelper.Lerp(24f, 84f, Convert01To010(i / 70f));

                            // Set the position
                            Vector2 position = target.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(-40f, 90f, i / 19f);

                            // Create the spike
                            ShaderParticleRegistry.SpawnCosmicParticle(position, sparkVelocity, scale);
                        }
                    }

                    break;
                }
            case States.Impact:
                {
                    if (!HasHitTarget)
                    {
                        Projectile.friendly = false;
                        Timer = 0f;
                    }
                    if (HasHitTarget)
                    {
                        AdditionsSound.etherealMagicBlast.Play(Projectile.Center, 3.5f, -.2f, .1f);
                        ScreenShakeSystem.New(new(18f, .8f, ScreenShake.DefaultRange * 4f), Projectile.Center);
                        Projectile.NewProj(Projectile.position, Vector2.Zero, ModContent.ProjectileType<NovaBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

                        int amount = 200;
                        for (int i = 0; i < amount; i++)
                        {
                            Vector2 veloc = Projectile.velocity.RotatedByRandom(Main.rand.NextFloat(.34f, .55f)) * Main.rand.NextFloat(.1f, 1.4f);
                            if (Main.netMode != NetmodeID.Server)
                            {
                                // Create the center
                                ShaderParticleRegistry.SpawnCosmicParticle(target.Center + Utils.NextVector2Circular(Main.rand, 30f, 30f), Utils.NextVector2Circular(Main.rand, 9f, 9f), 60f);

                                // Creates a seam
                                float scale = MathHelper.Lerp(25f, 95f, i / amount);

                                // Create the spike
                                ShaderParticleRegistry.SpawnCosmicParticle(target.Center, veloc, scale * 3);
                            }
                        }
                    }
                    break;
                }
            case States.Force:
                {
                    if (Returning == true)
                        ScreenShakeSystem.New(new(.6f, .3f, 1600f), Projectile.Center);
                    break;
                }
            case States.Sweep:
                {
                    AdditionsSound.etherealSmallHit.Play(Projectile.Center, 1.7f, 0f, .14f);

                    if (Main.netMode != NetmodeID.Server)
                    {
                        for (int i = 0; i < 60; i++)
                            ShaderParticleRegistry.SpawnCosmicParticle(target.Center + Utils.NextVector2Circular(Main.rand, 40f, 40f), Utils.NextVector2Circular(Main.rand, 3f, 3f), 70f);
                    }
                    break;
                }
        }


        if (!HasHitTarget)
        {
            NPCIndex = target.whoAmI;
            HasHitTarget = true;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        switch (State)
        {
            case States.Acceleration:
                {
                    modifiers.FinalDamage *= 2;
                    modifiers.NonCritDamage *= 1.2f;
                    break;
                }
            case States.Impact:
                {
                    modifiers.ScalingArmorPenetration += 1f;
                    modifiers.DefenseEffectiveness *= 0f;
                    break;
                }
            case States.Force:
                {
                    ref float returning = ref Projectile.AdditionsInfo().ExtraAI[4];
                    if (Returning == true)
                    {
                        modifiers.FinalDamage *= 2f;
                    }
                    break;
                }
            case States.Sweep:
                {
                    modifiers.FinalDamage *= 2.75f;
                    break;
                }
        }
    }
    #endregion

    #region Drawing
    private static readonly Texture2D chainTexture = AssetRegistry.GetTexture(AdditionsTexture.ReaperChain);
    public void DrawChain()
    {
        float opacity = RetractionTimer < 0.5 ? 1 : (RetractionTimer - 0.5f) / 0.5f;

        Vector2 Shake = RetractionTimer > 0 ? Vector2.Zero : Vector2.One.RotatedByRandom(MathHelper.TwoPi) * (1f - SnapTimer) * 2f;

        int dist = (int)Vector2.Distance(Owner.Center, Projectile.Center) / 16;
        Vector2[] points = new Vector2[dist + 1];
        points[0] = Owner.Center;
        points[dist] = Projectile.Center;

        for (int i = 1; i < dist + 1; i++)
        {
            Rectangle frame = new(0, 0 + 18 * (i % 2), 12, 18);
            Vector2 positionAlongLine = Vector2.Lerp(Owner.Center, Projectile.Center, i / (float)dist);
            points[i] = positionAlongLine + Shake * (float)Math.Sin(i / (float)dist * MathHelper.Pi);

            float rotation = (points[i] - points[i - 1]).ToRotation() - MathHelper.PiOver2;
            float yScale = Vector2.Distance(points[i], points[i - 1]) / frame.Height;
            Vector2 scale = new(1, yScale);

            Color chainLightColor = Lighting.GetColor((int)points[i].X / 16, (int)points[i].Y / 16);

            Vector2 origin = new(frame.Width / 2, frame.Height);
            Main.EntitySpriteDraw(chainTexture, points[i] - Main.screenPosition, frame, chainLightColor * opacity * 0.7f, rotation, origin, scale, SpriteEffects.None, 0);
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * 0.5f;

        if (State == States.Force && Released)
        {
            after?.DrawFancyAfterimages(texture, [Color.DarkViolet, Color.BlueViolet, Color.Violet], Projectile.Opacity);
        }

        // aura activities
        Vector2 offsets = new Vector2(0f, Projectile.gfxOffY) - Main.screenPosition;
        Vector2 drawStartOuter = offsets + Projectile.Center;
        float charge = InverseLerp(0f, ChargeupTime, TotalTime);
        Vector2 spinPoint = -Vector2.UnitY * 5f * charge;
        float time = Main.GlobalTimeWrappedHourly * 1.5f;
        float rotation = MathHelper.TwoPi * time / 3f;
        float opacity = .9f * charge;

        for (int i = 0; i < 6; i++)
        {
            Vector2 spinStart = drawStartOuter + Utils.RotatedBy(spinPoint, (double)(rotation - MathHelper.Pi * i / 3f), default);
            Color glowAlpha = Projectile.GetAlpha(ColorSwap(Color.BlueViolet, Color.Violet, 6f) * Projectile.Opacity) * Projectile.Opacity;
            Main.spriteBatch.Draw(texture, spinStart, frame, glowAlpha * opacity, Projectile.rotation, origin, Projectile.scale * 1.14f, (SpriteEffects)Projectile.spriteDirection, 0f);
        }

        // Draw the scythe
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, (SpriteEffects)Projectile.spriteDirection, 0f);

        if (State == States.Force && Released)
            DrawChain();

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        Owner.fullRotation = 0f;
    }
    #endregion
}