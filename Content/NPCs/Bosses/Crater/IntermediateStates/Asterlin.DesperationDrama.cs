using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using Utils = Terraria.Utils;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public static readonly Dictionary<AsterlinAIType, float> DesperationDrama_PossibleStates =
        new Dictionary<AsterlinAIType, float> { { AsterlinAIType.UnrelentingRush, 1f } };

    [AutomatedMethodInvoke]
    public void LoadStateTransitions_DesperationDrama()
    {
        StateMachine.RegisterTransition(AsterlinAIType.DesperationDrama, DesperationDrama_PossibleStates, false,
            () => AITimer >= DesperationDrama_MaxTime);
        StateMachine.RegisterStateBehavior(AsterlinAIType.DesperationDrama, DoBehavior_DesperationDrama);
    }

    public bool DesperationDrama_BeginDialogue
    {
        get => (int) ExtraAI[0] == 1;
        set => ExtraAI[0] = value.ToInt();
    }

    public static readonly float DesperationDrama_CameraScrollTime = SecondsToFrames(2f);
    public static readonly float DesperationDrama_ScreenPullupTime = SecondsToFrames(.8f);
    public static readonly float DesperationDrama_ChannelFindTime = SecondsToFrames(.5f);

    public static readonly int DesperationDrama_Wait = (int) (DesperationDrama_CameraScrollTime +
                                                              DesperationDrama_ScreenPullupTime +
                                                              DesperationDrama_ChannelFindTime);

    public static readonly float DesperationDrama_MaxTime = DesperationDrama_Wait;

    public const float DesperationDrama_MaxHeatDistortionArea = 1200f;
    public const float DesperationDrama_MaxHeatDistortionStrength = 1.2f;

    public LoopedSoundInstance Ominous;

    public void DoBehavior_DesperationDrama()
    {
        /*Ominous ??= LoopedSoundManager.CreateNew(
            new(AssetRegistry.GennedSounds.PipIdle, () => InverseLerp(TimeToTemp, TimeToHeatsink, AITimer) * .4f),
            () => CurrentState != AsterlinAIType.DesperationDrama || AdditionsLoopedSound.NPCNotActive(NPC));
        Ominous?.Update(NPC.Center);*/

        HeatDistortionArea = Sine.InOutFunction.Evaluate(AITimer, SecondsToFrames(2.6f),
            SecondsToFrames(4f), EnterPhase3_MaxHeatDistortionArea, DesperationDrama_MaxHeatDistortionArea);
        HeatDistortionStrength = MakePoly(3f).InFunction.Evaluate(AITimer, SecondsToFrames(2.6f),
            SecondsToFrames(4f), EnterPhase3_MaxHeatDistortionStrength,
            DesperationDrama_MaxHeatDistortionStrength);
        //GlowInterpolant = Utils.Remap(AITimer, TimeToUhOh, TimeToLast, 0f, .4f);
        //PowerInterpolant = Utils.Remap(AITimer, TimeToUhOh, TimeToLast, 0f, 1f);

        SetLegFlamesInterpolant(InverseLerp(70f, 0f, AITimer));
        SetLeftLegRotation(LeftLegRotation.AngleLerp(-1.5f * Direction, .2f));
        SetRightLegRotation(RightLegRotation.AngleLerp(-1.5f * Direction, .2f));
        SetHeadRotation(EyePosition.AngleTo(EyePosition + PolarVector(400f,
            Direction == -1 ? MathHelper.PiOver4 : -(MathHelper.PiOver4 + MathHelper.Pi))));

        NPC.velocity.X *= .6f;
        NPC.velocity.Y += .4f;
        NPC.noGravity = false;
        NPC.noTileCollide = false;

        CameraSystem.SetCamera(NPC.Center - Vector2.UnitY * 200f,
            MakePoly(2.3f).InOutFunction(InverseLerp(0f, DesperationDrama_CameraScrollTime, AITimer)));
    }
}
