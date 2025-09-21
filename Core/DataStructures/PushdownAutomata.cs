using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Utilities;

namespace TheExtraordinaryAdditions.Core.DataStructures;

// Credit goes to Luminance:
// https://github.com/LucilleKarma/Luminance/blob/main/Common/StateMachines/PushdownAutomata.cs

// PDA has a peculiar and odd issue where accessing something like NPC.ai[0] in its transition checking hardlocks the game
// I have no idea why its doing this and im unsure if its something specific to Aurora Guard (when it was using it)
public class PushdownAutomata<TStateWrapper, TStateIdentifier>
    where TStateWrapper : class, IState<TStateIdentifier>
    where TStateIdentifier : struct
{
    /// <summary>
    /// Represents a framework for hijacking a transition's final state selection.
    /// </summary>
    public record TransitionHijack(Func<TStateIdentifier?, TStateIdentifier?> SelectionHijackFunction, Action<TStateIdentifier?> HijackAction);

    /// <summary>
    /// Represents a framework for a state transition's information.
    /// </summary>
    public record TransitionInfo(TStateIdentifier? NewState, bool RememberPreviousState, Func<bool> TransitionCondition, Action TransitionCallback = null);

    /// <summary>
    /// Delegate for actions that run when OnStateTransition is fired.
    /// </summary>
    public delegate void OnStateTransitionDelegate(bool stateWasPopped, TStateWrapper oldState);

    public PushdownAutomata(TStateWrapper initialState)
    {
        StateStack.Push(initialState);
        RegisterState(initialState);
    }

    /// <summary>
    /// A collection of custom states that should be performed when a state is ongoing.
    /// </summary>
    public readonly Dictionary<TStateIdentifier, Action> StateBehaviors = [];

    /// <summary>
    /// A list of hijack actions to perform during a state transition.
    /// </summary>
    public List<TransitionHijack> HijackActions = [];

    /// <summary>
    /// A generalized registry of states with individualized data.
    /// </summary>
    public Dictionary<TStateIdentifier, TStateWrapper> StateRegistry = [];

    /// <summary>
    /// The state stack for the automaton.
    /// </summary>
    public Stack<TStateWrapper> StateStack = new();

    private readonly Dictionary<TStateIdentifier, List<TransitionInfo>> transitionTable = [];

    /// <summary>
    /// The current state of the automaton.
    /// </summary>
    public TStateWrapper CurrentState
    {
        get
        {
            if (StateStack.Count > 0)
            {
                return StateStack.Peek();
            }
            return null;
        }
    }

    /// <summary>
    /// The set of actions that should occur when a state is popped.
    /// </summary>
    public event Action<TStateWrapper> OnStatePop;

    /// <summary>
    /// The set of actions that should occur when a state transition occurs.
    /// </summary>
    public event OnStateTransitionDelegate OnStateTransition;

    /// <summary>
    /// The set of actions that should occur when the stack is out of items.
    /// </summary>
    public event Action OnStackEmpty;

    public void AddTransitionStateHijack(Func<TStateIdentifier?, TStateIdentifier?> hijackSelection, Action<TStateIdentifier?> hijackAction = null)
    {
        HijackActions.Add(new TransitionHijack(hijackSelection, hijackAction));
    }

    public void PerformBehaviors()
    {
        if (CurrentState != null && StateBehaviors.TryGetValue(CurrentState.Identifier, out Action value))
        {
            Action behavior = value;
            behavior?.Invoke();
        }
    }

    public void PerformStateTransitionCheck()
    {
        if (StateStack.Count == 0)
        {
            OnStackEmpty?.Invoke();
            return;
        }

        TStateWrapper currentState = CurrentState;
        if (currentState == null || !transitionTable.TryGetValue(currentState.Identifier, out List<TransitionInfo> value))
        {
            return;
        }

        List<TransitionInfo> potentialStates = value;
        TransitionInfo transition = null;

        // Find the first valid transition
        for (int i = 0; i < potentialStates.Count; i++)
        {
            if (potentialStates[i].TransitionCondition())
            {
                transition = potentialStates[i];
                break;
            }
        }

        if (transition == null)
        {
            return;
        }

        TStateWrapper oldState = null;

        // Pop the previous state if it doesn't need to be remembered
        if (!transition.RememberPreviousState && StateStack.Count > 0)
        {
            oldState = StateStack.Pop();
            OnStatePop?.Invoke(oldState);
            oldState?.OnPopped();
        }

        // Perform the transition
        TStateIdentifier? newState = transition.NewState;
        for (int i = 0; i < HijackActions.Count; i++)
        {
            TransitionHijack hijack = HijackActions[i];
            TStateIdentifier? hijackedState = hijack.SelectionHijackFunction(newState);
            if (!Equals(hijackedState, newState))
            {
                newState = hijackedState;
                hijack.HijackAction?.Invoke(newState);
                break;
            }
        }

        if (newState.HasValue && StateRegistry.TryGetValue(newState.Value, out TStateWrapper wrapper))
        {
            StateStack.Push(wrapper);
        }

        OnStateTransition?.Invoke(!transition.RememberPreviousState, oldState);
        transition.TransitionCallback?.Invoke();

        // Iteratively check for more transitions instead of recursion
        while (true)
        {
            if (StateStack.Count == 0)
            {
                OnStackEmpty?.Invoke();
                break;
            }

            currentState = CurrentState;
            if (currentState == null || !transitionTable.ContainsKey(currentState.Identifier))
            {
                break;
            }

            potentialStates = value;
            transition = null;

            for (int i = 0; i < potentialStates.Count; i++)
            {
                if (potentialStates[i].TransitionCondition())
                {
                    transition = potentialStates[i];
                    break;
                }
            }

            if (transition == null)
            {
                break;
            }

            oldState = null;
            if (!transition.RememberPreviousState && StateStack.Count > 0)
            {
                oldState = StateStack.Pop();
                OnStatePop?.Invoke(oldState);
                oldState?.OnPopped();
            }

            newState = transition.NewState;
            for (int i = 0; i < HijackActions.Count; i++)
            {
                TransitionHijack hijack = HijackActions[i];
                TStateIdentifier? hijackedState = hijack.SelectionHijackFunction(newState);
                if (!Equals(hijackedState, newState))
                {
                    newState = hijackedState;
                    hijack.HijackAction?.Invoke(newState);
                    break;
                }
            }

            if (newState.HasValue && StateRegistry.TryGetValue(newState.Value, out wrapper))
            {
                StateStack.Push(wrapper);
            }

            OnStateTransition?.Invoke(!transition.RememberPreviousState, oldState);
            transition.TransitionCallback?.Invoke();
        }
    }

    public void RegisterState(TStateWrapper state)
    {
        StateRegistry[state.Identifier] = state;
    }

    public void RegisterStateBehavior(TStateIdentifier state, Action behavior)
    {
        StateBehaviors[state] = behavior;
    }

    public void RegisterTransition(TStateIdentifier initialState, TStateIdentifier? newState, bool rememberPreviousState,
                                  Func<bool> transitionCondition, Action transitionCallback = null)
    {
        if (!transitionTable.TryGetValue(initialState, out List<TransitionInfo> value))
        {
            value = [];
            transitionTable[initialState] = value;
        }

        value.Add(new TransitionInfo(newState, rememberPreviousState, transitionCondition, transitionCallback));
    }

    public void ApplyToAllStatesExcept(Action<TStateIdentifier> action, params TStateIdentifier[] exceptions)
    {
        foreach (KeyValuePair<TStateIdentifier, TStateWrapper> pair in StateRegistry)
        {
            bool isException = false;
            for (int i = 0; i < exceptions.Length; i++)
            {
                if (Equals(pair.Key, exceptions[i]))
                {
                    isException = true;
                    break;
                }
            }
            if (!isException)
            {
                action(pair.Key);
            }
        }
    }
}

public class EntityAIState<TStateIdentifier>(TStateIdentifier identifier) : IState<TStateIdentifier> where TStateIdentifier : struct
{
    public TStateIdentifier Identifier { get; protected set; } = identifier;
    public int Time;

    public void OnPopped()
    {
        Time = 0;
    }
}

public interface IState<TStateIdentifier> where TStateIdentifier : struct
{
    TStateIdentifier Identifier { get; }
    void OnPopped();
}

[AttributeUsage(AttributeTargets.Method)]
public class AutoloadAsBehavior<TStateWrapper, TStateIdentifier>(TStateIdentifier associatedState) : Attribute
    where TStateWrapper : class, IState<TStateIdentifier>
    where TStateIdentifier : struct
{
    public readonly TStateIdentifier AssociatedState = associatedState;

    public static void FillStateMachineBehaviors<TInstanceType>(PushdownAutomata<TStateWrapper, TStateIdentifier> stateMachine, TInstanceType instance)
    {
        MethodInfo[] methods = instance.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        if (methods == null || methods.Length == 0)
        {
            return;
        }

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            object[] attributes = method.GetCustomAttributes(typeof(AutoloadAsBehavior<TStateWrapper, TStateIdentifier>), false);
            if (attributes.Length > 0)
            {
                AutoloadAsBehavior<TStateWrapper, TStateIdentifier> autoloadAttribute = (AutoloadAsBehavior<TStateWrapper, TStateIdentifier>)attributes[0];
                stateMachine.RegisterStateBehavior(autoloadAttribute.AssociatedState, () => method.Invoke(instance, null));
            }
        }
    }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class AutomatedMethodInvokeAttribute : Attribute
{
    public static void InvokeWithAttribute(object instance)
    {
        MethodInfo[] methods = instance.GetType().GetMethods(UniversalBindingFlags);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            object[] attributes = method.GetCustomAttributes(typeof(AutomatedMethodInvokeAttribute), false);
            if (attributes.Length > 0 && method.GetParameters().Length == 0)
            {
                method.Invoke(method.IsStatic ? null : instance, null);
            }
        }
    }
}

public class RandomPushdownAutomata<TStateWrapper, TStateIdentifier>
    where TStateWrapper : class, IState<TStateIdentifier>
    where TStateIdentifier : struct
{
    /// <summary>
    /// Represents a framework for hijacking a transition's final state selection.
    /// </summary>
    public record TransitionHijack(Func<TStateIdentifier?, TStateIdentifier?> SelectionHijackFunction, Action<TStateIdentifier?> HijackAction);

    /// <summary>
    /// Represents information for a random state transition with multiple possible states.
    /// </summary>
    public record RandomTransitionInfo(
        Dictionary<TStateIdentifier, float> PossibleStatesWeights,
        bool RememberPreviousState,
        Func<bool> TransitionCondition,
        Action TransitionCallback = null);

    /// <summary>
    /// Delegate for actions that run when OnStateTransition is fired.
    /// </summary>
    public delegate void OnStateTransitionDelegate(bool stateWasPopped, TStateWrapper oldState);

    private readonly UnifiedRandom random;
    private readonly Dictionary<TStateIdentifier, List<RandomTransitionInfo>> transitionTable = [];
    private readonly Dictionary<TStateIdentifier, Action> stateEntryCallbacks = []; // New dictionary for entry callbacks
    public readonly Dictionary<TStateIdentifier, Action> StateBehaviors = [];
    public List<TransitionHijack> HijackActions = [];
    public Dictionary<TStateIdentifier, TStateWrapper> StateRegistry = [];
    public Stack<TStateWrapper> StateStack = new();

    // Cache for filtered weights to avoid allocations
    private readonly Dictionary<TStateIdentifier, Dictionary<TStateIdentifier, float>> filteredWeightsCache = [];

    /// <summary>
    /// The current state of the automaton.
    /// </summary>
    public TStateWrapper CurrentState => StateStack.Count > 0 ? StateStack.Peek() : null;

    public event Action<TStateWrapper> OnStatePop;
    public event OnStateTransitionDelegate OnStateTransition;
    public event Action OnStackEmpty;

    /// <summary>
    /// Initializes the automata with an initial state and a UnifiedRandom instance for seeded randomness.
    /// </summary>
    public RandomPushdownAutomata(TStateWrapper initialState, UnifiedRandom random)
    {
        StateStack.Push(initialState);
        RegisterState(initialState);
        this.random = random ?? throw new ArgumentNullException(nameof(random));
    }

    /// <summary>
    /// Initializes the automata with an initial state and a seed for randomness.
    /// </summary>
    public RandomPushdownAutomata(TStateWrapper initialState, int seed) : this(initialState, new UnifiedRandom(seed)) { }

    /// <summary>
    /// Initializes the automata with an initial state and Main.rand for randomness.
    /// </summary>
    public RandomPushdownAutomata(TStateWrapper initialState) : this(initialState, Main.rand) { }

    public void AddTransitionStateHijack(Func<TStateIdentifier?, TStateIdentifier?> hijackSelection, Action<TStateIdentifier?> hijackAction = null)
    {
        HijackActions.Add(new TransitionHijack(hijackSelection, hijackAction));
    }

    public void PerformBehaviors()
    {
        if (CurrentState != null && StateBehaviors.TryGetValue(CurrentState.Identifier, out var behavior))
        {
            behavior?.Invoke();
        }
    }

    public void PerformStateTransitionCheck()
    {
        if (StateStack.Count == 0)
        {
            OnStackEmpty?.Invoke();
            return;
        }

        TStateWrapper currentState = CurrentState;
        if (currentState == null || !transitionTable.TryGetValue(currentState.Identifier, out var transitions))
        {
            return;
        }

        RandomTransitionInfo transition = null;
        for (int i = 0; i < transitions.Count; i++)
        {
            if (transitions[i].TransitionCondition())
            {
                transition = transitions[i];
                break;
            }
        }

        if (transition == null)
        {
            return;
        }

        var possibleStatesWeights = transition.PossibleStatesWeights;
        var filteredStatesWeights = GetFilteredWeights(currentState.Identifier, possibleStatesWeights);

        TStateIdentifier? newStateId = SelectRandomState(filteredStatesWeights);
        if (!newStateId.HasValue)
        {
            return;
        }

        for (int i = 0; i < HijackActions.Count; i++)
        {
            var hijack = HijackActions[i];
            var hijackedState = hijack.SelectionHijackFunction(newStateId);
            if (!Equals(hijackedState, newStateId))
            {
                newStateId = hijackedState;

                hijack.HijackAction?.Invoke(newStateId);
                break;
            }
        }

        TStateWrapper oldState = null;
        if (!transition.RememberPreviousState && StateStack.Count > 0)
        {
            oldState = StateStack.Pop();
            OnStatePop?.Invoke(oldState);
            oldState?.OnPopped();
        }

        if (newStateId.HasValue && StateRegistry.TryGetValue(newStateId.Value, out var newStateWrapper))
        {
            StateStack.Push(newStateWrapper);
            if (stateEntryCallbacks.TryGetValue(newStateId.Value, out var entryCallback))
            {
                entryCallback?.Invoke(); // Invoke the entry callback for the new state
            }
        }

        OnStateTransition?.Invoke(!transition.RememberPreviousState, oldState);
        transition.TransitionCallback?.Invoke();

        while (true)
        {
            if (StateStack.Count == 0)
            {
                OnStackEmpty?.Invoke();
                break;
            }

            currentState = CurrentState;
            if (currentState == null || !transitionTable.TryGetValue(currentState.Identifier, out transitions))
            {
                break;
            }

            transition = null;
            for (int i = 0; i < transitions.Count; i++)
            {
                if (transitions[i].TransitionCondition())
                {
                    transition = transitions[i];
                    break;
                }
            }

            if (transition == null)
            {
                break;
            }

            possibleStatesWeights = transition.PossibleStatesWeights;
            filteredStatesWeights = GetFilteredWeights(currentState.Identifier, possibleStatesWeights);

            newStateId = SelectRandomState(filteredStatesWeights);
            if (!newStateId.HasValue)
            {
                continue;
            }

            for (int i = 0; i < HijackActions.Count; i++)
            {
                var hijack = HijackActions[i];
                var hijackedState = hijack.SelectionHijackFunction(newStateId);
                if (!Equals(hijackedState, newStateId))
                {
                    newStateId = hijackedState;
                    hijack.HijackAction?.Invoke(newStateId);
                    break;
                }
            }

            oldState = null;
            if (!transition.RememberPreviousState && StateStack.Count > 0)
            {
                oldState = StateStack.Pop();
                OnStatePop?.Invoke(oldState);
                oldState?.OnPopped();
            }

            if (newStateId.HasValue && StateRegistry.TryGetValue(newStateId.Value, out newStateWrapper))
            {
                StateStack.Push(newStateWrapper);
                if (stateEntryCallbacks.TryGetValue(newStateId.Value, out var entryCallback))
                {
                    entryCallback?.Invoke(); // Invoke the entry callback for the new state
                }
            }

            OnStateTransition?.Invoke(!transition.RememberPreviousState, oldState);
            transition.TransitionCallback?.Invoke();
        }
    }

    public void RegisterState(TStateWrapper state)
    {
        StateRegistry[state.Identifier] = state;
    }

    public void RegisterStateBehavior(TStateIdentifier state, Action behavior)
    {
        StateBehaviors[state] = behavior;
    }

    public void RegisterTransition(TStateIdentifier initialState, Dictionary<TStateIdentifier, float> possibleStatesWeights,
                                  bool rememberPreviousState, Func<bool> transitionCondition, Action transitionCallback = null)
    {
        if (!transitionTable.TryGetValue(initialState, out var list))
        {
            list = new List<RandomTransitionInfo>(4); // Preallocate with reasonable capacity
            transitionTable[initialState] = list;
        }
        list.Add(new RandomTransitionInfo(possibleStatesWeights, rememberPreviousState, transitionCondition, transitionCallback));
    }

    /// <summary>
    /// Registers a callback to be invoked when entering the specified state.
    /// </summary>
    public void RegisterStateEntryCallback(TStateIdentifier state, Action entryCallback)
    {
        stateEntryCallbacks[state] = entryCallback;
    }

    public void ApplyToAllStatesExcept(Action<TStateIdentifier> action, params TStateIdentifier[] exceptions)
    {
        foreach (var pair in StateRegistry)
        {
            bool isException = false;
            for (int i = 0; i < exceptions.Length; i++)
            {
                if (Equals(pair.Key, exceptions[i]))
                {
                    isException = true;
                    break;
                }
            }
            if (!isException)
            {
                action(pair.Key);
            }
        }
    }

    private Dictionary<TStateIdentifier, float> GetFilteredWeights(TStateIdentifier currentStateId, Dictionary<TStateIdentifier, float> possibleStatesWeights)
    {
        // Try to get cached filtered weights
        if (filteredWeightsCache.TryGetValue(currentStateId, out var cachedWeights))
        {
            // Verify cache is still valid (same possibleStatesWeights reference)
            bool isValid = true;
            if (cachedWeights.Count == possibleStatesWeights.Count - (possibleStatesWeights.ContainsKey(currentStateId) ? 1 : 0))
            {
                foreach (var kv in cachedWeights)
                {
                    if (!possibleStatesWeights.ContainsKey(kv.Key) || possibleStatesWeights[kv.Key] != kv.Value)
                    {
                        isValid = false;
                        break;
                    }
                }
            }
            else
            {
                isValid = false;
            }

            if (isValid)
            {
                return cachedWeights.Count > 0 ? cachedWeights : possibleStatesWeights;
            }
        }

        // Create new filtered dictionary
        var filtered = new Dictionary<TStateIdentifier, float>(possibleStatesWeights.Count);
        foreach (var kv in possibleStatesWeights)
        {
            if (!Equals(kv.Key, currentStateId))
            {
                filtered.Add(kv.Key, kv.Value);
            }
        }

        // Cache the result
        filteredWeightsCache[currentStateId] = filtered;
        return filtered.Count > 0 ? filtered : possibleStatesWeights;
    }

    private TStateIdentifier? SelectRandomState(Dictionary<TStateIdentifier, float> statesWeights)
    {
        if (statesWeights.Count == 0)
        {
            return null;
        }

        // Inline sum calculation
        float totalWeight = 0f;
        foreach (var pair in statesWeights)
        {
            totalWeight += pair.Value;
        }

        float randomValue = (float)random.NextDouble() * totalWeight;

        float cumulative = 0f;
        foreach (var pair in statesWeights)
        {
            cumulative += pair.Value;
            if (randomValue < cumulative)
            {
                return pair.Key;
            }
        }

        // Fallback to last key
        foreach (var pair in statesWeights)
        {
            return pair.Key;
        }

        return null;
    }
}