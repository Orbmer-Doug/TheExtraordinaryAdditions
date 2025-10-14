using System;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin : ModNPC
{
    public RandomPushdownAutomata<EntityAIState<AsterlinAIType>, AsterlinAIType> StateMachine;

    /// <summary>
    /// The state that Asterlin is currently in
    /// </summary>
    public AsterlinAIType CurrentState
    {
        get
        {
            if (StateMachine == null)
                return AsterlinAIType.AbsorbingEnergy;
            return StateMachine.CurrentState.Identifier;
        }
        set
        {
            if (StateMachine != null)
                StateMachine.StateStack.Push(StateMachine.StateRegistry[value]);
        }
    }

    /// <summary>
    /// The time spent in the current state
    /// </summary>
    public int AITimer
    {
        get
        {
            if (StateMachine == null)
                return 0;
            return StateMachine.CurrentState.Time;
        }
        set
        {
            if (StateMachine != null)
                StateMachine.CurrentState.Time = value;
        }
    }

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
            ExtraAI[i] = 0f;
        this.Sync();
    }
}