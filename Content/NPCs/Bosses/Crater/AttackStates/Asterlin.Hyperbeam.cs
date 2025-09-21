using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_Hyperbeam()
    {
        StateMachine.RegisterTransition(AsterlinAIType.Hyperbeam, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Judgement, 1f }, { AsterlinAIType.TechnicBombBarrage, 1f } }, false, () =>
        {
            return Hyperbeam_CurrentState == Hyperbeam_States.Fade && AITimer >= Hyperbeam_FadeTime;
        });
        StateMachine.RegisterStateBehavior(AsterlinAIType.Hyperbeam, DoBehavior_Hyperbeam);
    }

    public static int Hyperbeam_HoverTime => 90;
    public static int Hyperbeam_PortalChargeTime => 110;
    public static int Hyperbeam_PortalWait => 30;
    public static int Hyperbeam_BeamBuildTime => 90;
    public static int Hyperbeam_BeamCycleTime => DifficultyBasedValue(190, 180, 170, 165, 150, 130);
    public static int Hyperbeam_BeamTime => SecondsToFrames(18f);
    public static float Hyperbeam_MovementSharpness => DifficultyBasedValue(.009f, .011f, .019f, .021f, .024f, .029f);
    public static float Hyperbeam_MovementSpeed => DifficultyBasedValue(34f, 38f, 40f, 42f, 46f, 50f);
    public static int Hyperbeam_FadeTime => 75;

    public enum Hyperbeam_States
    {
        Hover,
        SummonPortal,
        Chase,
        Fade
    }

    public Hyperbeam_States Hyperbeam_CurrentState
    {
        get => (Hyperbeam_States)NPC.AdditionsInfo().ExtraAI[0];
        set => NPC.AdditionsInfo().ExtraAI[0] = (int)value;
    }

    public int Hyperbeam_TotalTime
    {
        get => (int)NPC.AdditionsInfo().ExtraAI[1];
        set => NPC.AdditionsInfo().ExtraAI[1] = value;
    }

    public void DoBehavior_Hyperbeam()
    {
        SetLookingStraight(true);
        SetLeftHandTarget(leftArm.RootPosition - Vector2.UnitX * 400f);
        SetRightHandTarget(rightArm.RootPosition + Vector2.UnitX * 400f);
        SetZPosition(InverseLerp(Hyperbeam_HoverTime, 0f, Hyperbeam_TotalTime) * .5f);

        switch (Hyperbeam_CurrentState)
        {
            case Hyperbeam_States.Hover:
                CasualHoverMovement();

                if (AITimer >= Hyperbeam_HoverTime)
                {
                    if (this.RunServer())
                        NPC.NewNPCProj(NPC.Center, Vector2.Zero, ModContent.ProjectileType<SoulForgedRift>(), 0, 0f);
                    Hyperbeam_CurrentState = Hyperbeam_States.SummonPortal;
                    AITimer = 0;
                    NPC.netUpdate = true;
                }
                break;
            case Hyperbeam_States.SummonPortal:
                NPC.velocity *= .985f;

                if (AITimer >= (Hyperbeam_PortalChargeTime + Hyperbeam_PortalWait + Hyperbeam_BeamBuildTime))
                {
                    Hyperbeam_CurrentState = Hyperbeam_States.Chase;
                    AITimer = 0;
                    NPC.netUpdate = true;
                }
                break;
            case Hyperbeam_States.Chase:
                Vector2 pos = Target.Center - new Vector2(Target.Velocity.X, 80f);
                Vector2 direction = NPC.SafeDirectionTo(pos) * Hyperbeam_MovementSpeed;
                NPC.velocity = Vector2.Lerp(NPC.velocity, direction, Hyperbeam_MovementSharpness);

                if (AITimer >= Hyperbeam_BeamTime)
                {
                    Hyperbeam_CurrentState = Hyperbeam_States.Fade;
                    AITimer = 0;
                    NPC.netUpdate = true;
                }
                break;
            case Hyperbeam_States.Fade:

                break;
        }

        Hyperbeam_TotalTime++;
    }
}