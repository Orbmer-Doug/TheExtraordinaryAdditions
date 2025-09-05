using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public sealed partial class StygainHeart : ModNPC
{
    public void DoAttack_MoonBarrage(NPC npc, Player target, ref float attackTimer, bool inPhase2)
    {
        Vector2 hoverDestination = target.Center + new Vector2(target.velocity.X, -(Main.getGoodWorld ? 420f : 300f) + target.velocity.Y);

        npc.velocity = Vector2.SmoothStep(npc.velocity, npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(npc.Distance(hoverDestination), 32f), .5f);
        npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.07f, 0.08f);

        npc.damage = 0;

        int shootCycleTime = 61;
        int eyeCount = DifficultyBasedValue(15, 16, 18, 20, 24, 26);
        int moonCount = Main.getGoodWorld ? 5 : 3;
        float moonSpeed = DifficultyBasedValue(10.5f, 10.2f, 10f, 9.6f, 9.2f, 8.5f);
        float wrappedTime = attackTimer % shootCycleTime;
        ref float count = ref NPC.AdditionsInfo().ExtraAI[2];

        if (inPhase2 && HasDoneBloodBeacon == true)
        {
            moonCount += 1;
            moonSpeed -= 1.2f;
        }

        float offsetAngle = RandomRotation();
        if (wrappedTime == shootCycleTime - 1f)
        {
            for (int i = 0; i < moonCount; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / moonCount + offsetAngle).ToRotationVector2() * moonSpeed;
                npc.NewNPCProj(npc.Center, shootVelocity, ModContent.ProjectileType<BloodMoonlet>(), BloodBeaconDamage / 2, 10f);
            }
            for (int i = 0; i < eyeCount; i++)
            {
                Vector2 shootVelocity = (MathHelper.TwoPi * i / eyeCount + offsetAngle).ToRotationVector2() * 4f;
                npc.NewNPCProj(npc.Center, shootVelocity, ModContent.ProjectileType<WrithingEyeball>(), RadialEyesDamage, 0f);
            }

            SoundID.NPCHit18.Play(npc.Center, 1f, -.1f, .2f);
            SoundID.Item28.Play(npc.Center, 1.4f, -.1f, .1f);

            count++;
            npc.netUpdate = true;
        }

        if (count >= 5)
            SelectNextAttack();
    }

    public static float BarrierSize => DifficultyBasedValue(700f, 600f, 500f, 480f, 470f, 460f);
    public void DoAttack_DartCyclone(NPC npc, Player target, ref float attackTimer)
    {
        int dartShootTime = SecondsToFrames(6);
        int dartShootDelay = SecondsToFrames(1f);
        int dartBurstReleaseRate = DifficultyBasedValue(12, 10, 8, 7, 7, 7);
        int dartShootCount = DifficultyBasedValue(8, 10, 11, 12, 12, 12);
        float dartShootSpeed = DifficultyBasedValue(2.5f, 3f, 3.2f, 3.5f, 3.8f, 3.9f);
        float totalTime = dartShootTime + dartShootDelay + HemoglobTelegraph.TeleTime;

        // Hover near the target.
        if (attackTimer < dartShootDelay)
        {
            Vector2 hoverDestination = target.Center + new Vector2((npc.Center.X > target.Center.X).ToDirectionInt() * 400f, -70f);
            float distanceToDestination = npc.Distance(hoverDestination);
            Vector2 idealVelocity = npc.SafeDirectionTo(hoverDestination) * MathHelper.Min(distanceToDestination, 30f);
            npc.SimpleFlyMovement(Vector2.Lerp(idealVelocity, (hoverDestination - npc.Center) * 0.15f, Utils.GetLerpValue(280f, 540f, distanceToDestination, true)), 0.5f);
        }
        else
        {
            npc.velocity = Vector2.SmoothStep(npc.velocity, Vector2.Zero, .4f);
        }

        // Set the rotations
        npc.spriteDirection = (target.Center.X > npc.Center.X).ToDirectionInt();
        npc.rotation = npc.rotation.AngleLerp(npc.velocity.X * 0.14f, 0.19f);

        // Create the telegraph
        if (attackTimer == dartShootDelay)
        {
            npc.NewNPCProj(npc.Center, Vector2.Zero, ModContent.ProjectileType<BloodMoonlet>(), BloodBeaconDamage, 10f, -1, 0f, 1f);
            npc.NewNPCProj(npc.Center, Vector2.Zero, ModContent.ProjectileType<HemoglobTelegraph>(), 0, 0f, -1);
            npc.netUpdate = true;
        }

        // Charge energy while waiting
        if (attackTimer.BetweenNum(dartShootDelay, dartShootDelay + HemoglobTelegraph.TeleTime))
        {
            Vector2 pos = npc.Center + Main.rand.NextVector2Circular(200f, 200f);
            Vector2 vel = RandomVelocity(2f, 4f, 10f);
            int time = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.4f, .8f);
            ParticleRegistry.SpawnSparkParticle(pos, vel, time, scale, Color.Crimson, false, false, npc.Center);
        }

        // Start the barrier
        if (attackTimer == dartShootDelay + HemoglobTelegraph.TeleTime)
        {
            npc.NewNPCProj(npc.Center, Vector2.Zero, ModContent.ProjectileType<HemoglobBarrier>(), BloodBeaconDamage, 0f, -1, npc.whoAmI, 0f, 0f, 0f, target.whoAmI);
            npc.netUpdate = true;
        }

        // Begin releasing darts
        bool startFiring = attackTimer.BetweenNum(dartShootDelay + HemoglobTelegraph.TeleTime, totalTime, true);
        bool releaseRate = attackTimer % dartBurstReleaseRate == dartBurstReleaseRate - 1f;

        if (Main.netMode != NetmodeID.MultiplayerClient && startFiring && releaseRate)
        {
            float shootOffsetAngle = 3f * MathHelper.Pi * (attackTimer - (dartShootDelay)) / (dartShootTime);
            for (int i = 0; i < dartShootCount; i++)
            {
                Vector2 dartVelocity = (MathHelper.TwoPi * i / dartShootCount + shootOffsetAngle).ToRotationVector2() * dartShootSpeed * .5f;
                int dart = ModContent.ProjectileType<BloodRay>();
                npc.NewNPCProj(npc.Center, dartVelocity, dart, BulletTwirlDamage, 0f, -1, 0f, npc.whoAmI, target.whoAmI);
                ParticleRegistry.SpawnBloomLineParticle(npc.Center, dartVelocity.RotatedByRandom(.1f) * Main.rand.NextFloat(1f, 4f), Main.rand.Next(25, 40), Main.rand.NextFloat(.3f, .5f), Color.DarkRed);
            }
            AdditionsSound.etherealHit4.Play(npc.Center, 1.4f, 0f, .2f, 50);
        }

        if (attackTimer > totalTime)
            SelectNextAttack();
    }

    public void DoAttack_BloodBeacon(Player target)
    {
        ref float releaseCounter = ref NPC.AdditionsInfo().ExtraAI[1];
        ref float beaconLengthInterpolant = ref NPC.AdditionsInfo().ExtraAI[2];

        NPC.damage = 0;
        NPC.defense = NPC.defDefense + 55;

        // Times
        int beaconLife = BloodBeacon.Lifetime;
        const int riseTime = 140;
        const int waitTime = 40;
        const int beaconFadeIn = 118;
        const int settleWait = 140;

        int preparingTime = riseTime + waitTime + beaconFadeIn;
        bool preparing = AttackTimer < preparingTime;

        int attackingTime = preparingTime + beaconLife;
        bool attacking = AttackTimer < attackingTime;

        int settlingTime = attackingTime + settleWait + beaconFadeIn;
        bool settling = AttackTimer < settlingTime;

        bool finished = AttackTimer >= settlingTime;

        beaconLengthInterpolant = InverseLerp(riseTime + waitTime, preparingTime, AttackTimer) * InverseLerp(settlingTime, settlingTime - beaconFadeIn, AttackTimer);

        // Balance
        int spearRelease = 28;
        int spearSpacing = DifficultyBasedValue(260, 220, 180, 170, 160, 150);
        int moonRelease = DifficultyBasedValue(SecondsToFrames(2.8f), SecondsToFrames(2.3f), SecondsToFrames(2.1f), SecondsToFrames(1.8f), SecondsToFrames(1.6f), SecondsToFrames(1.3f));
        int starRelease = 16;

        // Movement
        int side = (NPC.Center.X > target.Center.X).ToDirectionInt();
        if (AttackTimer < riseTime)
        {
            Vector2 dest = target.Center + new Vector2(480f * side, 0f);
            NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(dest) * MathF.Min(NPC.Distance(dest), 50f), .4f);
        }
        else if (AttackTimer < (riseTime + waitTime))
        {
            NPC.velocity.Y -= .04f;
            NPC.velocity *= .1f;
        }
        else if (attacking)
        {
            float speed = Utils.Remap(NPC.Distance(target.Center), 100f, 1300f, 0f, 15f);
            NPC.velocity.X = Animators.MakePoly(3f).OutFunction.Evaluate(NPC.velocity.X, speed * -side, .04f);
        }
        else if (settling)
        {
            NPC.velocity *= .5f;
        }

        if (AttackTimer < (riseTime + waitTime))
        {
            float comp = InverseLerp(0f, (riseTime + waitTime), AttackTimer);
            float leftAngle = new Animators.PiecewiseCurve()
                .Add(MathHelper.Pi * 3f / 4f, MathHelper.PiOver2, .5f, Animators.MakePoly(4f).InOutFunction)
                .AddStall(MathHelper.PiOver2, 1f)
                .Evaluate(comp);
            float rightAngle = new Animators.PiecewiseCurve()
                .Add(MathHelper.PiOver4, MathHelper.PiOver2, .5f, Animators.MakePoly(4f).InOutFunction)
                .AddStall(MathHelper.PiOver2, 1f)
                .Evaluate(comp);
            float dist = new Animators.PiecewiseCurve()
                .Add(500f, 0f, .6f, Animators.MakePoly(4f).InFunction)
                .Add(0f, 1300f, 1f, Animators.Exp(2.2f).OutFunction)
                .Evaluate(comp);
            Vector2 left = NPC.Center + PolarVector(dist, -leftAngle);
            Vector2 right = NPC.Center + PolarVector(dist, -rightAngle);

            for (int i = 0; i < 5; i++)
            {
                Vector2 speed = Main.rand.NextVector2Circular(3f, 3f);
                int life = Main.rand.Next(30, 40);
                float scale = Main.rand.NextFloat(.7f, 1.2f);
                Color col = MulticolorLerp(Main.rand.NextFloat(), Color.DarkRed, Color.Crimson, Color.DarkRed * 1.5f);
                ParticleRegistry.SpawnHeavySmokeParticle(left, speed, life, scale, col, Main.rand.NextFloat(.7f, 1.1f));
                ParticleRegistry.SpawnHeavySmokeParticle(right, speed, life, scale, col, Main.rand.NextFloat(.7f, 1.1f));
                ParticleRegistry.SpawnGlowParticle(left, speed * .5f, life * 2, scale * 100f, Color.DarkRed, .2f);
                ParticleRegistry.SpawnGlowParticle(right, speed * .5f, life * 2, scale * 100f, Color.DarkRed, .2f);
            }

            if (MathF.Round(comp, 2) == .6f)
                AdditionsSound.IkeSpecial1A.Play(NPC.Center, 1.2f, -.2f);
        }

        if (preparing)
            FixedRotation(target, .1f);
        else
        {
            NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * 0.08f, 0.19f);
            NPC.spriteDirection = NPC.velocity.X.NonZeroSign();
        }

        // Attacking
        if (AttackTimer == (riseTime + waitTime))
        {
            AdditionsSound.Rapture.Play(target.Center, 1.2f, -.1f);
            
            if (this.RunServer())
            {
                NPC.NewNPCProj(NPC.Center - Vector2.UnitY * 2000f, Vector2.Zero, ModContent.ProjectileType<StygainRoar>(), 0, 0f);
                NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<BloodBeacon>(), BloodBeaconDamage, 10f);
                NPC.netUpdate = true;
            }
        }

        if (preparing)
        {

        }
        else if (attacking)
        {
            if (AttackTimer % spearRelease == (spearRelease - 1))
            {
                int height = BloodBeacon.MaxLaserLength / 2 + Main.rand.Next(-100, 100);
                float rot = releaseCounter % 2 == 1 ? -.3f : .3f;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -height; y < height; y += (spearSpacing * 2))
                    {
                        Vector2 pos = new(NPC.Center.X + (340f * x), NPC.Center.Y + y);
                        if (releaseCounter % 2 == 1)
                            pos.Y -= spearSpacing;

                        ExsanguinationLance lance = Main.projectile[NPC.NewNPCProj(pos, (Vector2.UnitX * x).RotatedBy(rot),
                            ModContent.ProjectileType<ExsanguinationLance>(), BloodBeaconLanceDamage, 0f)].As<ExsanguinationLance>();
                        lance.Free = true;
                    }
                }
                AdditionsSound.etherealHit4.Play(NPC.Center, 1.5f, 0f, .2f, 50);

                releaseCounter++;
            }

            if (AttackTimer % moonRelease == (moonRelease - 1))
            {
                int type = ModContent.ProjectileType<BloodMoonlet>();
                Vector2 pos = new(NPC.Center.X, target.Center.Y + Main.rand.NextFloat(-120f, 120f));
                Vector2 vel = pos.SafeDirectionTo(target.Center) * Main.rand.NextFloat(15f, 20f);
                NPC.NewNPCProj(pos, vel, type, BloodBeaconDamage / 2, 0f, -1);
                NPC.netUpdate = true;
                SoundID.Item28.Play(pos, 0f, -.3f);
            }
        }
        else if (AttackTimer < (attackingTime + settleWait))
        {
            if (AttackTimer % starRelease == (starRelease - 1))
            {
                if (this.RunServer())
                {
                    int height = BloodBeacon.MaxLaserLength / 2 + Main.rand.Next(-600, 600);
                    for (int x = -1; x <= 1; x += 2)
                    {
                        for (int y = -height; y < height; y += (spearSpacing * 2))
                        {
                            Vector2 pos = new(NPC.Center.X + (140f * x), NPC.Center.Y + y);
                            if (releaseCounter % 2 == 1)
                                pos.Y -= spearSpacing;

                            NPC.NewNPCProj(pos, Vector2.UnitX * x * 3f, ModContent.ProjectileType<TaintedStar>(), BloodBeaconLanceDamage, 0f, -1, 0f, 1f);
                        }
                    }
                }
                SoundID.Item163.Play(NPC.Center, 1.4f, -.4f, 0f, null, 80);
            }
        }

        if (finished)
        {
            HasDoneBloodBeacon = true;
            SelectNextAttack();
        }
    }
}