using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public sealed partial class StygainHeart : ModNPC
{
    public void DoAttack_ShotgunBloodshot(Player target, bool phase2)
    {
        int shootCycleTime = DifficultyBasedValue(65, 60, 51, 47, 44, 40);
        int shootPrepareTime = DifficultyBasedValue(42, 36, 30, 25, 23, 20);
        int shotCount = DifficultyBasedValue(2, 2, 3, 3, 4, 4);
        int projectileCount = DifficultyBasedValue(10, 11, 12, 13, 15, 17);

        if (phase2)
        {
            shootCycleTime += 15;
            shootPrepareTime -= 15;
            shotCount = 4;
            projectileCount += 2;
        }

        float wrappedTime = AttackTimer % shootCycleTime;
        bool preparingToShoot = wrappedTime > shootCycleTime - shootPrepareTime;

        float angleTurnSharpness = 1f - InverseLerp(shootCycleTime - 25f, shootCycleTime - 3f, wrappedTime);
        FixedRotation(target, angleTurnSharpness);

        // Have the movement fall off quickly when preparing to shoot
        if (preparingToShoot)
        {
            NPC.velocity *= 0.97f;
            NPC.velocity = Vector2.SmoothStep(NPC.velocity, Vector2.Zero, 0.05f);
            if (NPC.velocity.Length() < 0.03f)
                NPC.velocity = Vector2.Zero;

            if (wrappedTime == shootCycleTime - 1f)
            {
                Vector2 shootDirection = NPC.Center.SafeDirectionTo(target.Center);
                Vector2 shootPosition = NPC.Center;

                for (int i = 0; i < projectileCount; i++)
                {
                    Vector2 shootVelocity = shootDirection.RotatedByRandom(0.33f) * Main.rand.NextFloat(11.5f, 15f);
                    if (phase2)
                    {
                        shootVelocity = shootDirection.RotatedByRandom(0.56f) * Main.rand.NextFloat(10.5f, 12.5f);
                    }
                    if (this.RunServer())
                        NPC.NewNPCProj(shootPosition, shootVelocity, ModContent.ProjectileType<BloodShot>(), BloodBeaconLanceDamage, 0f);
                    for (int a = 0; a < 4; a++)
                    {
                        Dust.NewDustPerfect(shootPosition, DustID.Blood, shootVelocity, 0, default, Main.rand.NextFloat(1.9f, 2.4f));
                        ParticleRegistry.SpawnHeavySmokeParticle(shootPosition, shootVelocity * Main.rand.NextFloat(.9f, 1.5f), 50, Main.rand.NextFloat(.4f, 1f), Color.DarkRed);
                    }
                }

                // Rebound backward
                float aimAwayFromTargetInterpolant = Utils.GetLerpValue(250f, 185f, NPC.Distance(target.Center), true);
                float reboundSpeed = Utils.Remap(NPC.Distance(target.Center), 500f, 100f, phase2 ? 12f : 5f, phase2 ? 28f : 16f);
                Vector2 reboundDirection = Vector2.Lerp(shootDirection, NPC.SafeDirectionTo(target.Center), aimAwayFromTargetInterpolant).SafeNormalize(Vector2.UnitY);
                NPC.velocity -= reboundDirection * reboundSpeed;

                // And sync the NPC, to catch potential accumulating desyncs
                NPC.netUpdate = true;

                // Play a split sound
                SoundEngine.PlaySound(SoundID.Item17 with { Pitch = -.3f, Volume = 1.5f, PitchVariance = .1f }, NPC.Center);
            }
        }

        else
        {
            Vector2 hoverDestination = target.Center + new Vector2((NPC.Center.X > target.Center.X).ToDirectionInt() * 325f, -70f);
            float distanceToDestination = NPC.Distance(hoverDestination);
            Vector2 idealVelocity = NPC.SafeDirectionTo(hoverDestination) * MathHelper.Min(distanceToDestination, 18f);
            NPC.SimpleFlyMovement(Vector2.Lerp(idealVelocity, (hoverDestination - NPC.Center) * 0.15f, Utils.GetLerpValue(280f, 540f, distanceToDestination, true)), 0.4f);
        }

        if (AttackTimer >= shotCount * shootCycleTime)
            SelectNextAttack();
    }

    public void Do_Attack_ChargeWait(Player target, bool phase2)
    {
        // Disable contact damage while redirecting.
        NPC.damage = 0;

        int waitDelay = 60;
        Vector2 hoverDestination = target.Center - Vector2.UnitY * 300f;
        NPC.SmoothFlyNear(hoverDestination, .09f, .92f);

        // Look at the target.
        NPC.spriteDirection = (target.Center.X < NPC.Center.X).ToDirectionInt();
        FixedRotation(target);

        Color col = Color.Lerp(Color.DarkRed, Color.Red, Main.rand.NextFloat(0.7f));
        Vector2 pos = NPC.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(172f, 172f);
        if (AttackTimer % 2f == 1f)
        {
            Vector2 vel = (NPC.Center - pos) * 0.034f;
            ParticleRegistry.SpawnGlowParticle(pos, vel, 30, 50f, col);
        }

        if (Utility.FindProjectile(out Projectile p, ModContent.ProjectileType<HemoglobBarrier>()))
        {
            p.As<HemoglobBarrier>().FadeOut = true;
        }

        if (NPC.spriteDirection == 1)
            NPC.rotation += MathHelper.Pi;

        if (AttackTimer >= waitDelay)
            SelectNextAttack();
    }

    public void DoAttack_Charge(Player target, bool inPhase2)
    {
        int chargeCount = 4;
        if (inPhase2)
            chargeCount = 5;
        if (HasDoneBloodBeacon)
            chargeCount = 7;

        int aimTime = DifficultyBasedValue(36, 32, 28, 27, 26, 24);
        int slowdownTime = DifficultyBasedValue(18, 14, 12, 11, 10, 9);
        int chargeTime = DifficultyBasedValue(50, 45, 40, 35, 35, 35);

        if (NPC.justHit)
            NPC.netUpdate = true;
        float lifeRatio = NPC.life / (float)NPC.lifeMax;
        float chargeSpeed = MathHelper.Lerp(30f, Main.getGoodWorld ? 60f : 45f, 1f - lifeRatio);

        ref float aimTimer = ref NPC.AdditionsInfo().ExtraAI[2];
        ref float chargeTimer = ref NPC.AdditionsInfo().ExtraAI[3];
        ref float chosenDirection = ref NPC.AdditionsInfo().ExtraAI[4];
        ref float currentCharges = ref NPC.AdditionsInfo().ExtraAI[5];
        ref float start = ref NPC.AdditionsInfo().ExtraAI[6];

        // Initially pick a random, unchosen position
        if (this.RunServer() && start == 0f)
        {
            List<int> index = [];
            for (int i = 0; i < Directions.Length; i++)
            {
                if (Directions[i] == false)
                    index.Add(i);
            }

            chosenDirection = index[Main.rand.Next(0, index.Count - 1)];

            Directions[(int)chosenDirection] = true;

            start = 1f;
            NPC.netUpdate = true;
        }

        Vector2 destination = chosenDirection switch
        {
            0 => target.Center + new Vector2(550f, 0f), // Right
            1 => target.Center + new Vector2(550f, 550f), // Bottom-Right
            2 => target.Center + new Vector2(0f, 500f), // Bottom
            3 => target.Center + new Vector2(-550f, 550f), // Bottom-Left
            4 => target.Center + new Vector2(-550f, 0f), // Left
            5 => target.Center + new Vector2(-550f, -550f), // Top-Left
            6 => target.Center + new Vector2(0f, -500f), // Top
            7 => target.Center + new Vector2(550f, -550f), // Top-Right
            _ => target.Center + new Vector2(550f, 0f)
        };

        // Fly to the destination
        if (aimTimer < aimTime)
        {
            NPC.damage = 0;
            NPC.SmoothFlyNear(destination, .07f, .57f);

            if (NPC.Center.WithinRange(destination, 200f))
                aimTimer++;

            FixedRotation(target, .4f);
        }

        if (aimTimer >= aimTime)
        {
            if (chargeTimer == 0f)
            {
                NPC.velocity = NPC.SafeDirectionTo(target.Center + target.velocity) * chargeSpeed;
                for (int i = 0; i < 15; i++)
                {
                    ParticleRegistry.SpawnCloudParticle(NPC.RotHitbox().RandomPoint(), -NPC.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(2f, 13f), Color.IndianRed, Color.Crimson,
                        Main.rand.Next(90, 160), Main.rand.NextFloat(120f, 150f), Main.rand.NextFloat(.5f, .7f), Main.rand.NextByte(0, 2));
                }
                SoundEngine.PlaySound(SoundID.DD2_WyvernDiveDown with { Volume = 5f, Pitch = -.3f, PitchVariance = .1f }, NPC.Center);
                NPC.netUpdate = true;
            }
            NPC.velocity *= 1.0015f;

            chargeTimer++;

            if (chargeTimer >= (chargeTime + slowdownTime))
            {
                AttackTimer = 0;
                aimTimer = chargeTimer = start = 0f;
                currentCharges += 1;
                NPC.netUpdate = true;
            }

            if (chargeTimer >= chargeTime)
            {
                NPC.velocity = Vector2.SmoothStep(NPC.velocity, Vector2.Zero, .1f);
            }
            else
            {
                int shootRate = 5;
                if (inPhase2)
                    shootRate = 4;
                if (HasDoneBloodBeacon || Main.getGoodWorld)
                    shootRate = 3;

                if (chargeTimer % shootRate == shootRate - 1)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        Vector2 pos = i == -1 ? NPC.RotHitbox().Left : NPC.RotHitbox().Right;
                        Vector2 vel = NPC.velocity.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.Zero) * 2.5f * i;
                        if (this.RunServer())
                            NPC.NewNPCProj(pos, vel, ModContent.ProjectileType<TaintedStar>(), BloodwavesDamage, 2f);
                        for (int a = 0; a < 18; a++)
                        {
                            ParticleRegistry.SpawnGlowParticle(pos + Main.rand.NextVector2Circular(10f, 10f), vel.RotatedByRandom(.25f) * Main.rand.NextFloat(.5f, 5.8f), Main.rand.Next(20, 50),
                                Main.rand.NextFloat(30f, 40f), Color.Crimson, Main.rand.NextFloat(.5f, 1.5f));
                        }
                    }
                }

                Vector2 veloc = -NPC.velocity.SafeNormalize(Vector2.Zero) * chargeSpeed;
                ParticleRegistry.SpawnBloomLineParticle(NPC.RotHitbox().RandomPoint(), veloc, Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, 1f), Color.DarkRed.Lerp(Color.Crimson, .5f));
                ParticleRegistry.SpawnDustParticle(NPC.RotHitbox().RandomPoint(), veloc * Main.rand.NextFloat(.4f, .9f), Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, .8f), Color.Crimson, .1f, false, true);
            }
        }

        if (currentCharges > chargeCount)
        {
            for (int i = 0; i < Directions.Length; i++)
                Directions[i] = false;
            NPC.netUpdate = true;
            SelectNextAttack();
        }
    }

    public void DoAttack_PortalSmash(Player target)
    {
        ref float counter = ref NPC.AdditionsInfo().ExtraAI[0];
        ref float hit = ref NPC.AdditionsInfo().ExtraAI[1];
        float summonTime = DifficultyBasedValue(40f, 35f, 30f, 28f, 25f, 20f);
        float reelTime = DifficultyBasedValue(100f, 90f, 85f, 80f, 70f, 60f);
        float smashTime = DifficultyBasedValue(50f, 45f, 40f, 35f, 30f, 25f);

        // Spawn the portal
        if (AttackTimer < summonTime)
        {
            NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * .08f, .08f);
            NPC.velocity = Vector2.SmoothStep(NPC.velocity, Vector2.Zero, .4f);

            if (hit != 0f)
            {
                hit = 0f;
                NPC.netUpdate = true;
            }
        }

        if (this.RunServer() && AttackTimer == summonTime)
        {
            Vector2 pos = target.Center + target.velocity * 65f;
            NPC.NewNPCProj(pos, Vector2.Zero, ModContent.ProjectileType<SanguinePortal>(), 0, 0f, -1);
        }

        // Race towards it
        else if (AttackTimer > summonTime)
        {
            if (Utility.FindProjectile(out Projectile p, ModContent.ProjectileType<SanguinePortal>()))
            {
                FixedRotation(p, .1f);

                float reelInterpolant = 1f - Animators.MakePoly(3).InFunction(InverseLerp(summonTime, reelTime, AttackTimer));
                float smashInterpolant = Animators.MakePoly(5).InFunction(InverseLerp(reelTime, reelTime + smashTime, AttackTimer));
                if (AttackTimer < reelTime)
                    NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(p.Center) * -(14f * reelInterpolant), reelInterpolant + .1f);
                else
                {
                    NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(p.Center) * (MathF.Min(60f, NPC.Distance(p.Center)) * smashInterpolant), .5f);

                    Vector2 veloc = -NPC.velocity * Main.rand.NextFloat(.2f, .8f);
                    ParticleRegistry.SpawnBloomLineParticle(NPC.RotHitbox().RandomPoint(), veloc, Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, 1f), Color.DarkRed.Lerp(Color.Crimson, .5f));
                    ParticleRegistry.SpawnDustParticle(NPC.RotHitbox().RandomPoint(), veloc * Main.rand.NextFloat(.4f, .9f), Main.rand.Next(20, 40), Main.rand.NextFloat(.4f, .8f), Color.Crimson, .1f, false, true);
                }

                if (NPC.Hitbox.Intersects(p.Hitbox) && hit == 0f)
                {
                    ParticleRegistry.SpawnBlurParticle(NPC.Center, 30, .2f, 1000f);
                    ScreenShakeSystem.New(new(2f, 1.9f, ScreenShake.DefaultRange * 3f), NPC.Center);
                    AdditionsSound.etherealSmash.Play(NPC.Center, 30f, -.2f);
                    NPC.velocity *= .2f;
                    AttackTimer = 0;
                    counter++;
                    hit = 1f;
                    p.Kill();
                    NPC.netUpdate = true;
                }
            }
        }

        // End the attack
        if (counter >= 5)
        {
            SelectNextAttack();
        }
    }

    public void DoAttack_Assimilations(Player target, bool phase2)
    {
        ref float Counter = ref NPC.AdditionsInfo().ExtraAI[2];
        int count = DifficultyBasedValue(2, 3, 4, 5, 6, 7) + (int)Counter;

        const int life = SanguineAssimilation.TimeForBeam;
        float wait = DifficultyBasedValue(life + 10f, life, life - 25f, life - 35f, life - 40f, life - 40f);
        if (phase2)
            wait -= 12f;

        NPC.damage = 0;

        Vector2 dest = target.Center + new Vector2(0f, -450f);
        NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(dest) * MathF.Min(NPC.Distance(dest), 15f), .4f);
        NPC.rotation = NPC.rotation.AngleLerp(NPC.velocity.X * .04f, .1f);

        if (AttackTimer % wait == wait - 1f)
        {
            if (this.RunServer())
            {
                int dir = Main.rand.NextBool().ToDirectionInt();
                for (int i = 0; i < count; i++)
                {
                    Projectile p = Main.projectile[NPC.NewNPCProj(NPC.Center + Main.rand.NextVector2CircularLimited(200f, 200f, .4f, 1f), Vector2.Zero, ModContent.ProjectileType<SanguineAssimilation>(), BloodBeaconLanceDamage, 0f, -1)];
                    SanguineAssimilation sangue = p.As<SanguineAssimilation>();
                    sangue.Rot = MathHelper.TwoPi * i / count;
                    sangue.Dir = dir;
                }
            }
            Counter++;
            NPC.netUpdate = true;
        }

        if (Counter > 4)
        {
            SelectNextAttack();
        }
    }

    public void DoAttack_Bloodrain(Player target, bool phase2)
    {
        float attackTime = SecondsToFrames(15f);
        int conjureTime = 180;
        int lances = conjureTime + 60;
        int throwLances = conjureTime + 220;
        int lanceCount = DifficultyBasedValue(3, 4, 5, 6, 7, 8);
        ref float cycles = ref NPC.AdditionsInfo().ExtraAI[2];
        ref float rot = ref NPC.AdditionsInfo().ExtraAI[1];

        rot = (rot + .05f) % MathHelper.TwoPi;
        FixedRotation(target, .6f);

        // Visuals for creating the rain
        if (AttackTimer < conjureTime)
        {
            NPC.SmoothFlyNear(target.Center + new Vector2((target.Center.X < NPC.Center.X).ToDirectionInt() * 300f, -200f), .1f, .6f);

            float interpolant = InverseLerp(0f, conjureTime, AttackTimer);
            for (int i = 0; i < 8; i++)
            {
                Vector2 pos = NPC.Center + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * (500f * (1f - interpolant));
                Vector2 vel = pos.SafeDirectionTo(NPC.Center).RotatedBy(MathHelper.PiOver2 * (1f - interpolant)) * Main.rand.NextFloat(4f, 8f);
                int life = Main.rand.Next(20, 30);
                float scale = Main.rand.NextFloat(40f, 55f) * interpolant;
                Color col = Color.Crimson;
                ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale, col, 2f);
            }
        }
        if (AttackTimer == conjureTime - 1f)
        {
            for (int i = 0; i < 5; i++)
                ParticleRegistry.SpawnPulseRingParticle(NPC.Center, Vector2.Zero, 20 + (i * 5), 0f, Vector2.One, 0f, 290f + (i * 81f), Color.DarkRed, true);
            AdditionsSound.etherealChargeBoom.Play(NPC.Center, 1f, -.3f);
        }

        if (AttackTimer >= conjureTime)
        {
            NPC.velocity = Vector2.SmoothStep(NPC.velocity, NPC.SafeDirectionTo(target.Center + (target.velocity * 10f)) * MathF.Min(NPC.Distance(target.Center), 12f), .09f);

            // Create the rain
            float wait = DifficultyBasedValue(15f, 14f, 13f, 12f, 10f, 8f);
            if (AttackTimer % wait == wait - 1f && this.RunServer())
            {
                int rainCount = 5;
                for (int i = 0; i < rainCount; i++)
                {
                    float posX2 = Utils.Remap(i, 0f, rainCount, -(Main.LogicCheckScreenWidth / 2), Main.LogicCheckScreenWidth / 2) + Main.rand.NextFloat(-300f, 300f);
                    float posY = -Main.LogicCheckScreenHeight / 2 - 300f;

                    Vector2 pos = target.Center + new Vector2(posX2, posY);
                    Vector2 vel = Vector2.UnitY;
                    NPC.NewNPCProj(pos, vel, ModContent.ProjectileType<BloodDroplet>(), BloodshotDamage, 0f, -1);
                }
            }
        }

        int lanceType = ModContent.ProjectileType<ExsanguinationLance>();
        if (AttackTimer == lances)
        {
            // Initially spawn in lances
            if (!Utility.AnyProjectile(lanceType) && this.RunServer())
            {
                for (int i = 0; i < lanceCount; i++)
                {
                    Projectile p = Main.projectile[NPC.NewNPCProj(NPC.Center, Vector2.Zero, lanceType, BloodBeaconLanceDamage, 0f, -1)];
                    p.As<ExsanguinationLance>().Rot = MathHelper.TwoPi * i / lanceCount;
                }
            }

            // Otherwise call them all back
            else
            {
                foreach (Projectile p in Main.ActiveProjectiles)
                {
                    ExsanguinationLance lance = p.As<ExsanguinationLance>();
                    if (p != null && p.type == ModContent.ProjectileType<ExsanguinationLance>() && lance.Released)
                    {
                        lance.HitPlayer = lance.Released = false;
                        p.netUpdate = true;
                    }
                }
                AdditionsSound.BraveAttackAirN01.Play(NPC.Center, 1f, 0f, .2f, 0);
            }
        }
        else if (AttackTimer == throwLances)
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                ExsanguinationLance lance = p.As<ExsanguinationLance>();
                if (p != null && p.type == lanceType && !lance.Released && !lance.HitPlayer)
                {
                    lance.Released = true;
                    p.netUpdate = true;
                }
            }

            AdditionsSound.etherealRelease.Play(NPC.Center, 1.2f, -.2f, .1f);
            AttackTimer = conjureTime;
            cycles++;
            NPC.netUpdate = true;
        }

        if (cycles > 5)
        {
            DeleteAllProjectiles(false, ModContent.ProjectileType<BloodDroplet>(), lanceType);
            SelectNextAttack();
        }
    }
}