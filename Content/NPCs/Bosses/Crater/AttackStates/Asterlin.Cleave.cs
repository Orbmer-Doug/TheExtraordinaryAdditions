using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> Cleave_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Hyperbeam, 1f }, { AsterlinAIType.TechnicBombBarrage, .6f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Cleave()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Cleave, Cleave_PossibleStates, false, () =>
        {
            bool throwTime = Cleave_ThrowTimer >= Cleave_ThrowTime;
            if (Hammer != null)
                return throwTime && Hammer.Free;
            return throwTime;
        },
        () =>
        {
            ProjOwnedByNPC<Asterlin>.KillAll(ModContent.ProjectileType<LightPillar>());
        });

        StateMachine.RegisterStateEntryCallback(AsterlinAIType.Cleave, () =>
        {
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<JudgementHammer>(), HeavyAttackDamage, 10f);
        });

        StateMachine.RegisterStateBehavior(AsterlinAIType.Cleave, DoBehavior_Cleave);
    }

    public static int Cleave_MaxCycles => DifficultyBasedValue(6, 7, 8);
    public static int Cleave_HammerReelTime => DifficultyBasedValue(SecondsToFrames(1.4f), SecondsToFrames(1.2f), SecondsToFrames(1f), SecondsToFrames(.8f), SecondsToFrames(.6f));
    public static int Cleave_HammerOutTime => SecondsToFrames(.2f);
    public static float Cleave_DownAcceleration => 15f;
    public static float Cleave_MaxDownSpeed => 116f;
    public static int Cleave_DartCount => DifficultyBasedValue(10, 12, 15, 16, 17, 20);
    public static int Cleave_DartWaves => DifficultyBasedValue(1, 1, 2);
    public static int Cleave_PillarCount => 40;
    public static int Cleave_PillarWait => DifficultyBasedValue(30, 25, 20);
    public static float Cleave_PillarFallSpeed => DifficultyBasedValue(20f, 25f, 30f, 32f, 34f, 36f);
    public static int Cleave_BreatheTime => SecondsToFrames(1.5f);
    public static int Cleave_ThrowTime => SecondsToFrames(.85f);

    public int Cleave_Cycle
    {
        get => (int)ExtraAI[0];
        set => ExtraAI[0] = value;
    }
    public bool Cleave_Diving
    {
        get => ExtraAI[1] == 1;
        set => ExtraAI[1] = value.ToInt();
    }
    public int Cleave_Wait
    {
        get => (int)ExtraAI[2];
        set => ExtraAI[2] = value;
    }
    public bool Cleave_HitGround
    {
        get => ExtraAI[3] == 1;
        set => ExtraAI[3] = value.ToInt();
    }
    public int Cleave_ThrowTimer
    {
        get => (int)ExtraAI[4];
        set => ExtraAI[4] = value;
    }

    public void DoBehavior_Cleave()
    {
        int dir = -Direction;
        float behind = dir == 1 ? -ThreePIOver4 : -MathHelper.PiOver4;

        if (Cleave_Cycle >= Cleave_MaxCycles)
        {
            NPC.SmoothFlyNear(new Vector2(Target.Center.X + 100f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), Target.Center.Y - 20f), .15f, .9f);

            float interpol = InverseLerp(0f, Cleave_ThrowTime, Cleave_ThrowTimer);
            SetRightHandTarget(RightArm.RootPosition + PolarVector(400f, Utils.AngleLerp(-MathHelper.PiOver2, behind, Animators.MakePoly(5f).InFunction(interpol))));

            if (interpol >= 1 && Hammer != null)
            {
                if (!Hammer.Free)
                {
                    Hammer.Projectile.velocity = RightArm.RootPosition.SafeDirectionTo(RightHandPosition) * 35f;
                    Hammer.Free = true;
                    Hammer.Sync();
                }
            }

            Cleave_ThrowTimer++;
            return;
        }

        if (!Cleave_Diving)
        {
            float interpol = InverseLerp(0f, Cleave_HammerReelTime, AITimer);
            NPC.SmoothFlyNear(new Vector2(Target.Center.X + Target.Velocity.ClampLength(0f, 50f).X * 20f,
                Target.Center.Y - Animators.MakePoly(3f).InFunction.Evaluate(100f, 450f, interpol)), .15f, .9f);

            float rot = Utils.AngleLerp(dir == 1 ? 0f : MathHelper.Pi, behind, Animators.MakePoly(3f).InOutFunction(interpol));
            SetRightHandTarget(RightArm.RootPosition + PolarVector(400f, rot));

            if (AITimer > (Cleave_HammerReelTime + 20))
            {
                if (this.RunServer())
                {
                    int spacing = AngledWidth * 2 + LightPillar.Width;
                    for (int o = -1; o <= 1; o += 2)
                    {
                        for (int i = 0; i < Cleave_PillarCount; i++)
                        {
                            // Use an exponential to deter running away
                            float lerp = InverseLerp(0, Cleave_PillarCount, i);
                            float lerp2 = InverseLerp(0, Cleave_PillarCount - 1, i);
                            float exp = Animators.MakePoly(4f).OutFunction(lerp2);
                            float x = (spacing / 4 * i) * o;
                            float y = -60f * (Cleave_PillarCount * exp);
                            float speed = MathHelper.Lerp(Cleave_PillarFallSpeed, Cleave_PillarFallSpeed * 3, lerp);

                            int maxWait = -Cleave_PillarWait * i;
                            int wait = (int)MathHelper.Lerp(maxWait, maxWait / 2, lerp);
                            NPC.NewNPCProj(RotatedHitbox.Top + new Vector2(x, y), Vector2.Zero, ModContent.ProjectileType<LightPillar>(), MediumAttackDamage, 2f, ai0: wait, ai2: speed);
                        }
                    }
                }

                foreach (Player player in Main.ActivePlayers)
                {
                    player.velocity.Y += GaussianFalloff2D(NPC.Center, player.Center, 20f, 1000f);
                }

                AITimer = 0;
                Cleave_Diving = true;
                this.Sync();
            }
        }
        else
        {
            NPC.damage = NPC.defDamage;
            SetFlipped(false);
            FlameEngulfInterpolant = InverseLerp(0f, 20f, AITimer);
            SetHeadRotation(Direction == 1 ? 0 : MathHelper.Pi);
            SetBodyRotation(BodyRotation.AngleLerp(-MathHelper.Pi, .2f));
            SetRightHandTarget(RightHandTarget.Lerp(RightArm.RootPosition + PolarVector(400f, MathHelper.PiOver2), .3f));

            if (!Cleave_HitGround)
            {
                NPC.velocity.X *= 0.55f;
                NPC.velocity.Y = MathHelper.Clamp(NPC.velocity.Y + Cleave_DownAcceleration, -25f, Cleave_MaxDownSpeed);

                if (RotatedHitbox.SolidCollision())
                {
                    Vector2 ground = RaytraceTiles(NPC.Center - Vector2.UnitY * 1500f, NPC.Center + Vector2.UnitY * 200f) ?? NPC.Center;

                    if (this.RunServer())
                    {
                        for (int j = 0; j < Cleave_DartCount; j++)
                        {
                            float completion = InverseLerp(0f, Cleave_DartCount - 1, j);
                            float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-MathHelper.PiOver2, MathHelper.PiOver2, completion);
                            Vector2 vel = PolarVector(3f, angle);
                            NPC.NewNPCProj(ground, vel * 3, ModContent.ProjectileType<OverloadedLightDart>(), LightAttackDamage, 0f);
                        }
                    }

                    for (int i = 0; i < 120; i++)
                    {
                        ParticleRegistry.SpawnSquishyPixelParticle(ground, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(8f, 70f),
                            Main.rand.Next(200, 310), Main.rand.NextFloat(2.7f, 4.8f), Color.Gold, Color.OrangeRed, 8, false, true);
                        ParticleRegistry.SpawnMistParticle(ground, -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2).RotatedByRandom(.1f) * Main.rand.NextFloat(8f, 50f), Main.rand.NextFloat(.5f, 1.4f), Color.Gold, Color.DarkGoldenrod, Main.rand.NextFloat(200f, 240f));

                        float completion = InverseLerp(0f, 120, i);
                        Vector2 vel = Vector2.UnitX * MathHelper.Lerp(-74f, 74f, completion);
                        if (vel == Vector2.Zero)
                            vel = Vector2.UnitX * 2f;
                        int life = (int)MathHelper.Lerp(60, 80, Convert01To010(completion));
                        float scale = (int)MathHelper.Lerp(.1f, 2f, Convert01To010(completion));
                        ParticleRegistry.SpawnGlowParticle(ground, vel, life, scale * 350f, Color.OrangeRed.Lerp(Color.Gold, .4f));
                    }

                    AdditionsSound.MeteorImpact.Play(ground, 1.2f, -.1f, .1f);
                    ParticleRegistry.SpawnBlurParticle(ground, 70, .5f, 1000f);
                    ParticleRegistry.SpawnChromaticAberration(ground, 50, 1.4f, 800f);
                    ScreenShakeSystem.New(new(1.8f, 1.6f, 3000f), ground);
                    Cleave_HitGround = true;
                    this.Sync();
                }
            }
            else
            {
                NPC.velocity = Vector2.Zero;
                if (Cleave_Wait > Cleave_BreatheTime)
                {
                    FlameEngulfInterpolant =
                    Cleave_Wait =
                    AITimer = 0;
                    Cleave_Diving = Cleave_HitGround = false;
                    Cleave_Cycle++;
                    this.Sync();
                }
                Cleave_Wait++;
            }
        }
    }
}
