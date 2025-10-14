using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;
using TheExtraordinaryAdditions.Core.Netcode;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public sealed partial class StygainHeart : ModNPC
{
    public void DoAttack_SpawnEffects(Player target)
    {
        const int animationTime = 400;
        HasDoneBloodBeacon = false;

        // Focus on the boss as it spawns.
        float interpolant = InverseLerp(0f, 120f, AttackTimer);
        float zoomInterpolant = InverseLerp(120f, 180f, AttackTimer) * .35f;
        CameraSystem.SetCamera(NPC.Center, interpolant, zoomInterpolant);

        if (AttackTimer < animationTime)
        {
            NPC.damage = 0;
            if (!NPC.dontTakeDamage)
            {
                NPC.dontTakeDamage = true;
                NPC.netUpdate = true;
            }

            Vector2 hoverDestination = target.Center + new Vector2((target.Center.X < NPC.Center.X).ToDirectionInt() * 320f, -440f);
            Vector2 idealVelocity = NPC.SafeDirectionTo(hoverDestination) * MathF.Min(NPC.Distance(hoverDestination), 15f);
            NPC.velocity = Vector2.SmoothStep(NPC.velocity, idealVelocity, .3f);

            FixedRotation(target, .1f);

            if (AttackTimer % 20 == 19)
            {
                ParticleRegistry.SpawnPulseRingParticle(NPC.Center, Vector2.Zero, 50, 0f, Vector2.One, 2f, 0f, Color.Crimson, true);
                AdditionsSound.HeatTail.Play(NPC.Center, 1.8f, 0f, .3f, 0);
            }
            if (AttackTimer % 4 == 3)
            {
                Vector2 pos = NPC.Center + Main.rand.NextVector2CircularLimited(300f, 300f, .4f, 1f);
                Vector2 vel = Main.rand.NextVector2Circular(9f, 9f);
                float scale = Main.rand.NextFloat(.5f, .8f);
                int life = Main.rand.Next(40, 80);
                Color col = Color.Crimson.Lerp(Color.DarkRed, .4f);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, col, Color.DarkRed, NPC.Center, Main.rand.NextFloat(1f, 2f), 4);
                if (Main.rand.NextBool())
                    ParticleRegistry.SpawnSparkParticle(pos, vel, life * 2, scale * .9f, col * 2f, false, false, NPC.Center);
            }

            HasDoneDramaticBurst = false;
        }
        else
        {
            NPC.dontTakeDamage = false;
            CombatText.NewText(target.Hitbox, Color.PaleVioletRed, this.GetLocalizedValue("Ominous"), true);

            if (!Main.raining || Main.maxRaining < 0.7f)
            {
                StartRain();
                Main.cloudBGActive = 1f;
                Main.numCloudsTemp = 160;
                Main.numClouds = Main.numCloudsTemp;
                Main.windSpeedCurrent = 1.04f;
                Main.windSpeedTarget = Main.windSpeedCurrent;
                Main.maxRaining = 0.96f;
            }
            Main.bloodMoon = true;
            AdditionsNetcode.SyncWorld();

            NPC.netUpdate = true;

            for (int i = 1; i < 5; i++)
            {
                ParticleRegistry.SpawnPulseRingParticle(NPC.Center, Vector2.Zero, 200, RandomRotation(), new(Main.rand.NextFloat(.6f, 1f), 1f), 0f, i, Color.DarkRed, true);
            }

            AdditionsSound.spearCharge.Play(NPC.Center, 1.2f, -.2f);

            HasDoneDramaticBurst = true;
            SelectNextAttack();
        }
    }

    public void DoBehavior_Phase2Drama(Player target)
    {
        int totalTime = SecondsToFrames(7f);

        NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, .4f);

        float interpolant = GetLerpBump(0f, 120f, totalTime - 240f, totalTime - 120f, AttackTimer);
        CameraSystem.SetCamera(NPC.Center, interpolant, interpolant * .1f);

        if (AttackTimer.BetweenNum(120, totalTime))
        {
            float completion = InverseLerp(120f, totalTime, AttackTimer);
            NPC.position += Main.rand.NextVector2Circular(10f, 10f) * completion;

            if (AttackTimer % 5 == 4)
                ParticleRegistry.SpawnCartoonAngerParticle(NPC.RotHitbox().RandomPoint(), Main.rand.Next(30, 40),
                    Main.rand.NextFloat(.4f, .6f), RandomRotation(), Color.Crimson, Color.DarkRed);

            FixedRotation(target, .04f);
        }
        if (AttackTimer >= totalTime)
        {
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<StygainRoar>(), 0, 0f, -1);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 2f, Pitch = -.3f }, NPC.Center);

            HasDonePhase2Drama = true;
            AttackCycle = 0;
            SelectNextAttack();
        }
    }

    public const float deathAnimTime = 800f;
    public void DoBehavior_DeathEffects(Player target)
    {
        HideBossBar(NPC);
        HasDoneBloodBeacon = false;
        NPC.ShowNameOnHover = false;

        NPC.velocity = Vector2.Lerp(NPC.velocity, Vector2.Zero, .15f);
        FixedRotation(target, .05f);

        float animCompletion = InverseLerp(0f, deathAnimTime, AttackTimer);

        float cameraInterpolant = GetLerpBump(120f, 180f, deathAnimTime - 120f, deathAnimTime - 180f, AttackTimer);
        CameraSystem.SetCamera(NPC.Center, cameraInterpolant, cameraInterpolant * .3f);

        int wait = (int)(30f - (animCompletion * 28f));
        if (AttackTimer % wait == wait - 1)
        {
            Vector2 pos = NPC.RandAreaInEntity();
            for (int i = 0; i < 20; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(12f, 12f);
                int life = Main.rand.Next(50, 90);
                float scale = Main.rand.NextFloat(.4f, .6f);
                ParticleRegistry.SpawnBloodParticle(pos, vel, life, scale, Color.DarkRed);
            }
            ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Vector2.One * 130f, Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(24, 34), Color.Crimson);

            if (this.RunServer())
            {
                NPC.velocity += Main.rand.NextVector2CircularLimited(6f, 6f, .4f, 1f);
                NPC.netUpdate = true;
            }

            if (this.RunServer() && Main.rand.NextBool())
            {
                NPC.NewNPCBetter(NPC.RotHitbox().RandomPoint(), Main.rand.NextVector2Circular(10f, 10f), Main.rand.Next(4) switch
                {
                    1 => NPCID.DemonEye,
                    2 => NPCID.Drippler,
                    3 => NPCID.EyeballFlyingFish,
                    _ => NPCID.WanderingEye

                }, 0, 0f, 0f, 0f, 0f, NPC.target);
            }

            SoundEngine.PlaySound(SoundID.DD2_BetsyFireballImpact with { Pitch = -.3f, PitchVariance = .1f }, pos);
        }

        if (animCompletion >= 1f)
        {
            ScreenShakeSystem.New(new(1f, 1.5f, 2000f), NPC.Center);
            ParticleRegistry.SpawnFlash(NPC.Center, 80, .4f, 2000f);
            ParticleRegistry.SpawnBlurParticle(NPC.Center, 90, .5f, 1400f, .8f);

            for (int i = 0; i < 100; i++)
            {
                if (i.BetweenNum(1, 6, true))
                {
                    Color randomColor = Main.rand.Next(4) switch
                    {
                        0 => Color.DarkRed * 1.6f,
                        1 => Color.Crimson,
                        2 => Color.DarkRed * 1.4f,
                        _ => Color.Red * 1.5f,
                    };

                    Vector2 pos = NPC.Center;
                    Vector2 vel = Vector2.Zero;
                    float start = 0f;
                    float end = 900f + (i * 500f);
                    int life = 146 + (i * 10);

                    ParticleRegistry.SpawnPulseRingParticle(pos, vel, life, 0f, Vector2.One, start, end, randomColor, true);
                }

                if (i < 70)
                    ParticleRegistry.SpawnBloomPixelParticle(NPC.RandAreaInEntity(), Main.rand.NextVector2Circular(48f, 48f), Main.rand.Next(90, 140),
                        Main.rand.NextFloat(.3f, .5f), Color.Red, Color.Crimson, null, 1.4f);

                if (i < 50)
                    ParticleRegistry.SpawnBloomLineParticle(NPC.Center, Main.rand.NextVector2CircularLimited(30f, 30f, .2f, 1f),
                        Main.rand.Next(40, 80), Main.rand.NextFloat(.5f, .8f), Color.Crimson);

                ParticleRegistry.SpawnBloodParticle(NPC.RandAreaInEntity(), Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.Next(200, 300), Main.rand.NextFloat(.6f, 1.5f), Color.Crimson.Lerp(Color.DarkRed, Main.rand.NextFloat(.3f, .8f)));
            }

            AdditionsSound.GaussBoomLittle.Play(NPC.Center);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 1.5f, Pitch = -.3f }, NPC.Center);
            NPC.Kill();
        }
    }
}