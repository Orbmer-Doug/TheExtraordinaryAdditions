using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;


namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;

public class AvragenMinion : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public const int Sight = 4000;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.minionSlots = 8f;
        Projectile.penetrate = -1;
        Projectile.width = Projectile.height = 148;
        Projectile.scale = 1f;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.netImportant = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.minion = true;
    }

    public static readonly float CycleTime = SecondsToFrames(3f);
    public enum AvraPhases
    {
        CurvedBeam,
        DaggerBarrage,
        ConsumingVortexes,
    }
    public ref float Timer => ref Projectile.ai[0];
    public AvraPhases Phase
    {
        get => (AvraPhases)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }
    public ref float PhaseTimer => ref Projectile.ai[2];
    public bool Idling
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public bool Cycling
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }

    public NPCTargeting.NPCSeekingData data => new(Projectile.Center, Sight, false, false);
    public override void AI()
    {
        if (!Owner.active || Owner.dead)
        {
            Modded.Minion[GlobalPlayer.AdditionsMinion.Avragen] = false;
            return;
        }
        Owner.AddBuff(ModContent.BuffType<AvragenPresence>(), 3600);
        if (Modded.Minion[GlobalPlayer.AdditionsMinion.Avragen])
            Projectile.timeLeft = 2;

        Visuals();
        Timer++;
        PhaseTimer++;

        List<NPC> targets = NPCTargeting.GetNPCsClosestToFarthest(data);

        if (targets.Count > 0)
        {
            Idling = false;
            Cycling = false;
            if (HasCloseProximityEnemies(targets, 2, 200f))
            {
                ChangeState(AvraPhases.DaggerBarrage);
            }
            else
            {
                if (targets.Count >= 3)
                {
                    if (Phase == AvraPhases.ConsumingVortexes && PhaseTimer >= SecondsToFrames(3f))
                        ChangeState(AvraPhases.DaggerBarrage);
                    else if (Phase == AvraPhases.DaggerBarrage && PhaseTimer >= SecondsToFrames(6f))
                        ChangeState(AvraPhases.ConsumingVortexes);
                }
                else
                {
                    Cycling = true;
                    if (PhaseTimer >= SecondsToFrames(3f))
                    {
                        CycleToNextPhase();
                    }
                }
            }
        }
        else
        {
            Idling = true;
        }

        // Execute phase with appropriate targeting
        NPC closest = (targets.Count > 0 ? targets[0] : null);
        NPC densest = NPCTargeting.GetNPCInLargestCluster(data) ?? null;
        switch (Phase)
        {
            case AvraPhases.CurvedBeam:
                Beam(!Cycling ? densest : closest, targets.Count);
                break;
            case AvraPhases.DaggerBarrage:
                Barrage(closest);
                break;
            case AvraPhases.ConsumingVortexes:
                Vortexes(densest);
                break;
        }

        Vector2 dest = Owner.Center + new Vector2(0f, -200f);
        Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(dest) * MathF.Min(Projectile.Distance(dest), 40f), .7f);
    }

    private bool HasCloseProximityEnemies(List<NPC> targets, int minCount, float safeDistance)
    {
        int closeCount = targets.Count(npc => Vector2.Distance(Owner.Center, npc.Center) <= safeDistance);
        return closeCount >= minCount;
    }

    private void CycleToNextPhase()
    {
        switch (Phase)
        {
            case AvraPhases.CurvedBeam:
                ChangeState(AvraPhases.DaggerBarrage);
                break;
            case AvraPhases.DaggerBarrage:
                ChangeState(AvraPhases.ConsumingVortexes);
                break;
            case AvraPhases.ConsumingVortexes:
                ChangeState(AvraPhases.CurvedBeam);
                break;
        }
    }

    public void ChangeState(AvraPhases phase)
    {
        if (Phase != phase)
        {
            Phase = phase;
            PhaseTimer = 0;
            Projectile.netUpdate = true;
        }
    }

    public void Beam(NPC target, int targetCount)
    {
        if (target == null)
            return;

        if (PhaseTimer == 0 && this.RunLocal())
        {
            AdditionsSound.HeavyLaserBlast.Play(Projectile.Center, .8f, -.2f, .1f);
            int laser = Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<VoidBeam>(),
                Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f, Projectile.whoAmI);
            Main.projectile[laser].netUpdate = true;
        }

        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.SafeDirectionTo(target.Center).ToRotation(), .2f);
    }

    public void Barrage(NPC target)
    {
        if (target == null)
            return;

        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(target.Center), .8f);

        if (PhaseTimer % 20 == 19)
        {
            AdditionsSound.banditShot1A.Play(Projectile.Center, .9f, -.25f, .1f, 0);
            for (int i = 0; i < 4; i++)
            {
                Utils.ChaseResults chase = Utils.GetChaseResults(Projectile.Center, Main.rand.NextFloat(18f, 26f), target.Center, target.velocity);
                Vector2 vel = chase.ChaserVelocity.RotatedByRandom(.3f);
                if (this.RunLocal())
                    Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<FleetDaggers>(), Projectile.damage / 4, Projectile.knockBack, Owner.whoAmI);

                for (int j = 0; j < 4; j++)
                {
                    ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.5f, 1f), Main.rand.Next(70, 100), Main.rand.NextFloat(.4f, .9f), Color.Violet, Color.BlueViolet, 4);
                    ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel.RotatedByRandom(.3f) * Main.rand.NextFloat(.7f, 1.4f), Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .6f), Color.Violet, Main.rand.NextFloat(.8f, 1.5f));
                }
            }
        }
    }

    public void Vortexes(NPC target)
    {
        if (target == null)
            return;

        Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(target.Center), 1f);

        if (PhaseTimer % 18 == 17)
        {
            AdditionsSound.BraveSpecial1B.Play(Projectile.Center, 1.5f, -.1f, .2f, 0);
            Utils.ChaseResults chase = Utils.GetChaseResults(Projectile.Center, 20f, target.Center, target.velocity);
            if (this.RunLocal())
                Projectile.NewProj(Projectile.Center, chase.ChaserVelocity, ModContent.ProjectileType<ConsumingVoid>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);

            for (int i = 0; i < 20; i++)
            {
                ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center, Main.rand.NextVector2CircularEdge(20f, 20f), 100, Main.rand.NextFloat(.8f, 1.4f), Color.Violet, Color.BlueViolet, 10, false, false, Main.rand.NextFloat(-.2f, .2f));
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, chase.ChaserVelocity.RotatedByRandom(.4f) * Main.rand.NextFloat(.4f, 1.4f), Main.rand.Next(40, 50), Main.rand.NextFloat(.5f, 1.5f), Color.BlueViolet);
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, chase.ChaserVelocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.3f, .7f), Main.rand.Next(40, 50), Main.rand.NextFloat(40f, 100f), Color.DarkViolet);
            }
        }
    }

    public void Visuals()
    {
        for (int i = -1; i <= 1; i += 2)
        {
            for (int a = -1; a <= 2; a += 2)
            {
                for (int o = 0; o < 8; o++)
                {
                    float speed = Main.rand.NextFloat(1f, 12f);
                    Vector2 horiz = (i == -1 ? -Vector2.UnitX * speed : Vector2.UnitX * speed);
                    Vector2 vert = (a == -1 ? -Vector2.UnitY * speed : Vector2.UnitY * speed);
                    float size = 1.3f * (1f - InverseLerp(1f, 15f, speed));
                    int life = 30;
                    Color col = Color.Lerp(Color.Violet, Color.DarkViolet, .5f);
                    ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center, vert, life, size, col);
                    ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center, horiz, life, size, col);
                }
            }
        }

        for (int i = 0; i < 4; i++)
        {
            Vector2 position = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.height / 2);
            ParticleRegistry.SpawnHeavySmokeParticle(position, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(40, 50), Main.rand.NextFloat(.5f, 1.5f), Color.Violet);
            if (i % 2 == 0)
                ParticleRegistry.SpawnGlowParticle(position, Main.rand.NextVector2CircularEdge(8f, 8f), Main.rand.Next(30, 40), Main.rand.NextFloat(30f, 150f), Color.BlueViolet);
        }

        Vector2 offset = Main.rand.NextVector2Circular(Projectile.width, Projectile.height);
        Vector2 pos = Projectile.Center + offset;

        if (Main.rand.NextBool(5))
            ParticleRegistry.SpawnBloomPixelParticle(pos + offset * 2f, Main.rand.NextVector2Circular(4f, 4f),
                Main.rand.Next(80, 140), Main.rand.NextFloat(.5f, 1.2f), Color.Violet, Color.DarkViolet, Projectile.Center, Main.rand.NextFloat(.9f, 1.5f));

        if (Main.rand.NextBool(6))
            ParticleRegistry.SpawnDustParticle(pos, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, 1.1f), Color.Violet, .1f, false, true, true);
    }
}