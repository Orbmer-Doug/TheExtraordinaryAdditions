using System;
using TheExtraordinaryAdditions.Core.DataStructures;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin
{
    public RandomPushdownAutomata<EntityAIState<AsterlinAIType>, AsterlinAIType> StateMachine;

    /// <summary>
    /// The state that Asterlin is currently in
    /// </summary>
    public AsterlinAIType CurrentState
    {
        get => StateMachine == null ? AsterlinAIType.AbsorbingEnergy : StateMachine.CurrentState.Identifier;
        set => StateMachine?.StateStack.Push(StateMachine.StateRegistry[value]);
    }

    /// <summary>
    /// The time spent in the current state
    /// </summary>
    public int AITimer
    {
        get => StateMachine == null ? 0 : StateMachine.CurrentState.Time;
        set => StateMachine?.CurrentState.Time = value;
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

                return originalState;
            },
            finalState =>
            {
                switch (finalState)
                {
                    case AsterlinAIType.EnterPhase2:
                        DonePhase2Transition = true;
                        break;
                    case AsterlinAIType.EnterPhase3:
                        DonePhase3Transition = true;
                        break;
                    case AsterlinAIType.DesperationDrama:
                        DoneDesperationTransition = true;
                        break;
                }
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
