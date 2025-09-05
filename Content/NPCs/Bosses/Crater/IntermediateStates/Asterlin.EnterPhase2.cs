using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    [AutomatedMethodInvoke]
    public void LoadStateTransitions_EnterPhase2()
    {
        StateMachine.RegisterTransition(AsterlinAIType.EnterPhase2, new Dictionary<AsterlinAIType, float> { { AsterlinAIType.Cleave, 1f } }, false, () => AITimer >= EnterPhase2_Length);
        StateMachine.RegisterStateBehavior(AsterlinAIType.EnterPhase2, DoBehavior_EnterPhase2);
    }

    public static readonly int EnterPhase2_Length = SecondsToFrames(1.6f);
    public void DoBehavior_EnterPhase2()
    {
        if (AITimer == 1)
        {
            AdditionsSound.AsterlinChange.Play(EyePosition, 1.8f, -.2f, 0f);
        }

        float completion = InverseLerp(0f, EnterPhase2_Length, AITimer);
        EyeGleamInterpolant = new Animators.PiecewiseCurve()
            .Add(0f, 1f, .5f, Animators.MakePoly(3f).OutFunction)
            .Add(1f, 0f, 1f, Animators.MakePoly(4f).InOutFunction)
            .Evaluate(completion);

        NPC.velocity *= .95f;
    }
}