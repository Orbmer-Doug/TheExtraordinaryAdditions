using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using State = TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.TesselesticMeltdownProj.State;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> Tesselestic_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Disintegration, 1f }, { AsterlinAIType.Lightripper, .6f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Tesselestic()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Tesselestic, Tesselestic_PossibleStates, false, () =>
        {
            return Tesselestic_FadeTime > Tesselestic_FadeDuration;
        });

        // State transition checks happen after behavior, meaning by the time it reaches back to behaviors setup with the meltdown will already have been set
        StateMachine.RegisterStateEntryCallback(AsterlinAIType.Tesselestic, () =>
        {
            if (this.RunServer())
                NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<TheTesselesticMeltdown>(), MediumAttackDamage, 0f);
        });

        StateMachine.RegisterStateBehavior(AsterlinAIType.Tesselestic, DoBehavior_Tesselestic);
    }

    public int Tesselestic_Cycle
    {
        get => (int)ExtraAI[0];
        set => ExtraAI[0] = value;
    }

    public bool Tesselestic_Shooting
    {
        get => ExtraAI[1] == 1;
        set => ExtraAI[1] = value.ToInt();
    }

    public int Tesselestic_AttackTime
    {
        get => (int)ExtraAI[2];
        set => ExtraAI[2] = value;
    }

    public int Tesselestic_FadeTime
    {
        get => (int)ExtraAI[3];
        set => ExtraAI[3] = value;
    }

    public static int Tesselestic_Cycles => DifficultyBasedValue(7, 8, 9);
    public static readonly int Tesselestic_ChargeUp = SecondsToFrames(2.2f);
    public static int Tesselestic_NodeCount => DifficultyBasedValue(9, 13, 15, 18, 21, 24);
    public static int Tesselestic_PositionTime => SecondsToFrames(1.8f);
    public static int Tesselestic_FireTime => DifficultyBasedValue(SecondsToFrames(1.4f), SecondsToFrames(1.35f),
        SecondsToFrames(1.16f), SecondsToFrames(1f), SecondsToFrames(.9f), SecondsToFrames(.85f));
    public static float Tesselestic_NodeRotationAmt => DifficultyBasedValue(.5f, 1.7f, 2.8f, 3.4f, 4.5f, 5.2f);
    public static int Tesselestic_FadeDuration => SecondsToFrames(.4f);
    public void DoBehavior_Tesselestic()
    {
        Vector2 ideal = Utility.GetHomingVelocity(NPC.Center, Target.Center, Target.Velocity, 20f);
        Vector2 destination = new(Target.Center.X + 300f * (NPC.Center.X > Target.Center.X).ToDirectionInt(), Target.Center.Y - 140f);
        NPC.SmoothFlyNear(destination, .2f, .9f);

        if (Tesselestic_Cycle >= Tesselestic_Cycles)
        {
            Tesselestic_FadeTime++;

            foreach (Projectile p in AllProjectilesByID(ModContent.ProjectileType<LightningNode>()))
                p.Kill();

            return;
        }

        if (Staff != null)
        {
            int node = ModContent.ProjectileType<LightningNode>();
            if (AITimer < Tesselestic_ChargeUp)
            {
                float interpol = InverseLerp(0f, Tesselestic_ChargeUp, AITimer);
                int area = (int)Animators.MakePoly(3f).InOutFunction.Evaluate(400, 10, interpol);
                Vector2 pos = Staff.TipOfStaff.ToRectangle(area, area).ToRotated(0f).RandomPoint(true);

                for (int i = 0; i < 20; i++)
                {
                    int life = Main.rand.Next(30, 40);
                    float scale = Main.rand.NextFloat(.4f, .8f);
                    ParticleRegistry.SpawnTechyHolosquareParticle(pos, pos.SafeDirectionTo(Staff.TipOfStaff) * 4f, life, scale, Color.DeepSkyBlue, 1);
                }
            }
            else if (AITimer == Tesselestic_ChargeUp)
            {
                ParticleRegistry.SpawnDetailedBlastParticle(Staff.TipOfStaff, Vector2.Zero, Vector2.One * 200f, Vector2.Zero, 50, Color.Cyan);
                if (this.RunServer())
                {
                    for (int i = 0; i < Tesselestic_NodeCount; i++)
                        NPC.NewNPCProj(Staff.TipOfStaff, Main.rand.NextVector2CircularLimited(20f, 20f, .3f, 1f), node, 0, 0f);
                }
                AdditionsSound.ElectricalPowBoom.Play(Staff.TipOfStaff, 1.2f, 0f, .1f);
            }
            else
            {
                if (Staff.CurrentState != State.Barrage)
                {
                    Staff.CurrentState = State.Barrage;
                    Staff.Sync();
                }
                SetRightHandTarget(RightArm.RootPosition - Vector2.UnitY * 400f);

                if (!Tesselestic_Shooting)
                {
                    if (Tesselestic_AttackTime > Tesselestic_PositionTime)
                    {
                        Tesselestic_AttackTime = 0;
                        Tesselestic_Shooting = true;
                        this.Sync();
                    }
                }
                else
                {
                    NPC.velocity *= .1f;
                    foreach (Projectile p in AllProjectilesByID(node))
                    {
                        if (!p.As<LightningNode>().Channeling)
                        {
                            if (this.RunServer())
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    Vector2 vel = (MathHelper.TwoPi * InverseLerp(0, 4, i) + p.rotation).ToRotationVector2() * 3f;
                                    NPC.NewNPCProj(p.Center, vel, ModContent.ProjectileType<OverchargedLaser>(), LightAttackDamage, 0f, -1, 1f);
                                }
                            }

                            p.As<LightningNode>().Channeling = true;
                            p.netUpdate = true;
                        }

                        if (this.RunServer() && Tesselestic_AttackTime % 8 == 7)
                        {
                            NPC.NewNPCProj(Staff.TipOfStaff, Vector2.Zero, ModContent.ProjectileType<HonedTesselesticLightning>(),
                                MediumAttackDamage, 0f, ai1: p.Center.X, ai2: p.Center.Y);
                        }
                    }

                    if (Tesselestic_AttackTime > Tesselestic_FireTime)
                    {
                        foreach (Projectile p in AllProjectilesByID(node))
                        {
                            p.As<LightningNode>().Channeling = false;
                            p.netUpdate = true;
                        }

                        Tesselestic_Cycle++;
                        Tesselestic_AttackTime = 0;
                        Tesselestic_Shooting = false;
                        this.Sync();
                    }
                }

                Tesselestic_AttackTime++;
            }
        }
    }
}
