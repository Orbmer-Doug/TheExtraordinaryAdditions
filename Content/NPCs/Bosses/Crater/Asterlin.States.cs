using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    private RandomPushdownAutomata2<EntityAIState<AsterlinAIType>, AsterlinAIType> stateMachine;

    public RandomPushdownAutomata2<EntityAIState<AsterlinAIType>, AsterlinAIType> StateMachine
    {
        get
        {
            if (stateMachine is null)
                LoadStates();
            return stateMachine!;
        }
        set => stateMachine = value;
    }

    public ref int AITimer => ref StateMachine.CurrentState.Time;

    public void LoadStates()
    {
        // Initialize the AI state machine
        StateMachine = new(new(AsterlinAIType.AbsorbingEnergy));
        StateMachine.OnStateTransition += ResetGenericVariables;

        // Register all of Asterlins states in the machine
        foreach (AsterlinAIType type in Enum.GetValues(typeof(AsterlinAIType)))
            StateMachine.RegisterState(new EntityAIState<AsterlinAIType>(type));

        StateMachine.AddTransitionStateHijack(
        originalState =>
        {
            if (NPC.life <= 1 && !DoneDesperationTransition)
                return AsterlinAIType.DesperationDrama;
            if (DoneDesperationTransition)
                return originalState;

            if (LifeRatio <= Phase3LifeRatio && !DonePhase3Transition)
                return AsterlinAIType.EnterPhase3;
            if (DonePhase3Transition)
                return originalState;

            if (LifeRatio <= Phase2LifeRatio && !DonePhase2Transition)
                return AsterlinAIType.EnterPhase2;
            if (DonePhase2Transition)
                return originalState;

            return originalState;
        },
        finalState =>
        {
            if (finalState == AsterlinAIType.EnterPhase2)
                DonePhase2Transition = true;
            if (finalState == AsterlinAIType.EnterPhase3)
                DonePhase3Transition = true;
            if (finalState == AsterlinAIType.DesperationDrama)
                DoneDesperationTransition = true;
        });

        // Load state transitions
        AutomatedMethodInvokeAttribute.InvokeWithAttribute(this);
    }

    public void ResetGenericVariables(bool stateWasPopped, EntityAIState<AsterlinAIType> oldState)
    {
        AITimer = 0;
        for (int i = 0; i < 10; i++)
            NPC.AdditionsInfo().ExtraAI[i] = 0f;
        NPC.netUpdate = true;
    }
}