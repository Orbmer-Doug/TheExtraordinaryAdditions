using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> Lightripper_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Tesselestic, 1f }, { AsterlinAIType.Disintegration, .6f } };
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Lightripper()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Lightripper, Lightripper_PossibleStates, false, () =>
        {
            return Lightripper_Cycles >= Lightripper_TotalCycles;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Lightripper, DoBehavior_Lightripper);
    }

    public static int Lightripper_DartBombCount => DifficultyBasedValue(3, 3, 4, 4, 5, 5);
    public static int Lightripper_TotalCycles => DifficultyBasedValue(3, 3, 3, 5, 5, 5);

    public enum LightripperState
    {
        BeamRelease,
        Hover,
        Reel,
        Dash,
        Wait,
    }

    public ref float Lightripper_Cycles => ref ExtraAI[0];
    public LightripperState Lightripper_State
    {
        get => (LightripperState)ExtraAI[1];
        set => ExtraAI[1] = (int)value;
    }
    public ref float Lightripper_InitialDirection => ref ExtraAI[2];

    public static float Lightripper_BeamDelay => 11f;
    public static float Lightripper_FanOffset => 1.45f;
    public static int Lightripper_ReleaseRate => 2;
    public static int Lightripper_TotalBeams => DifficultyBasedValue(7, 8, 10, 11, 12, 14);

    public static int Lightripper_HoverTime => SecondsToFrames(1.1f);
    public static int Lightripper_ReelbackTime => SecondsToFrames(.6f);
    public static int Lightripper_DashTime => SecondsToFrames(.35f);
    public static int Lightripper_SlowdownTime => SecondsToFrames(.67f);

    public void DoBehavior_Lightripper()
    {
        switch (Lightripper_State)
        {
            case LightripperState.BeamRelease:
                if (AITimer == Lightripper_BeamDelay)
                    Lightripper_InitialDirection = NPC.Center.AngleTo(Target.Center);

                if (AITimer > Lightripper_BeamDelay)
                {
                    float fanInterpolant = Utils.GetLerpValue(0f, Lightripper_ReleaseRate * Lightripper_TotalBeams, AITimer - Lightripper_BeamDelay, true);
                    float offsetAngle = MathHelper.Lerp(-Lightripper_FanOffset, Lightripper_FanOffset, fanInterpolant);
                    Vector2 shootVelocity = (Lightripper_InitialDirection + offsetAngle).ToRotationVector2();

                    if (this.RunServer() && AITimer % Lightripper_ReleaseRate == Lightripper_ReleaseRate - 1f)
                    {
                        int type = ModContent.ProjectileType<LightrippingBeam>();
                        NPC.NewNPCProj(RightHandPosition + shootVelocity.SafeNormalize(Vector2.Zero) * 100f, shootVelocity, type, HeavyAttackDamage, 0f);
                    }

                    SetRightHandTarget(RightArm.RootPosition + shootVelocity * 400f);

                    if (AITimer >= Lightripper_BeamDelay + Lightripper_ReleaseRate * Lightripper_TotalBeams)
                    {
                        // Reset timers and proceed to next stage of the attack
                        Lightripper_State = LightripperState.Hover;
                        AITimer = 0;
                        this.Sync();
                    }
                }

                Vector2 hoverDestination = Target.Center + new Vector2((Target.Center.X < NPC.Center.X).ToDirectionInt() * 450f, -150f);
                Vector2 idealVelocity = (hoverDestination - NPC.Center) * 0.07f;
                NPC.velocity = Vector2.Lerp(NPC.velocity, idealVelocity, 0.3f);
                break;
            case LightripperState.Hover:
                hoverDestination = Target.Center + Target.Center.SafeDirectionTo(NPC.Center) * new Vector2(650f, 450f);
                float flySpeed = InverseLerp(0f, Lightripper_HoverTime, AITimer).Cubed() * 0.15f + 0.01f;
                NPC.SmoothFlyNear(hoverDestination, flySpeed, 1f - flySpeed);
                if (AITimer >= Lightripper_HoverTime)
                {
                    AdditionsSound.spearLaser.Play(NPC.Center, 2f, .12f);
                    Lightripper_State = LightripperState.Reel;
                    AITimer = 0;
                    this.Sync();
                }
                break;
            case LightripperState.Reel:
                float reelBackSpeed = InverseLerp(0f, Lightripper_ReelbackTime, AITimer).Squared() * 50f;
                float lookAngularVelocity = Utils.Remap(AITimer, 0f, Lightripper_ReelbackTime, 0.2f, 0.029f);

                NPC.rotation = NPC.rotation.AngleLerp(NPC.AngleTo(Target.Center), lookAngularVelocity);
                SetBodyRotation(NPC.rotation);
                NPC.velocity = NPC.rotation.ToRotationVector2() * -reelBackSpeed;
                NPC.velocity *= 0.9f;

                // Create a bunch of light to indicate direction
                ParticleRegistry.SpawnSquishyLightParticle(RotatedHitbox.RandomPoint(), NPC.rotation.ToRotationVector2() * Main.rand.NextFloat(8f, 40f),
                    Main.rand.Next(20, 50), Main.rand.NextFloat(.5f, 1.5f), Color.DeepSkyBlue, 1f, 2f, 5f);

                if (AITimer >= Lightripper_ReelbackTime)
                {
                    Lightripper_State = LightripperState.Dash;
                    AITimer = 0;
                    this.Sync();
                }
                break;
            case LightripperState.Dash:
                if (this.RunServer())
                {
                    // Release some bombs
                    for (int i = 0; i < Lightripper_DartBombCount; i++)
                    {
                        Vector2 pos = NPC.Center;
                        Vector2 vel = NPC.SafeDirectionTo(Target.Center).RotatedByRandom(.85f) * Main.rand.NextFloat(11f, 99f);
                        NPC.NewNPCProj(pos, vel, ModContent.ProjectileType<DartBomb>(), LightAttackDamage, 0f);
                    }
                }

                AdditionsSound.BlackHoleExplosion.Play(NPC.Center, 1.3f, -.3f);
                ScreenShakeSystem.New(new(.5f, .9f, 4000f), NPC.Center);
                NPC.velocity = NPC.rotation.ToRotationVector2() * 275f;
                Lightripper_State = LightripperState.Wait;
                AITimer = 0;
                this.Sync();
                break;
            case LightripperState.Wait:
                NPC.velocity *= .915f;
                NPC.damage = NPC.defDamage;

                if (AITimer >= Lightripper_SlowdownTime)
                {
                    Lightripper_Cycles++;
                    Lightripper_State = LightripperState.BeamRelease;
                    AITimer = 0;
                    this.Sync();
                }
                break;
        }
    }
}
