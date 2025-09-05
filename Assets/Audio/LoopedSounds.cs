using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Assets.Audio;

public class LoopedSoundManager : ModSystem
{
    private static readonly List<LoopedSoundInstance> loopedSounds = new();

    public override void OnModLoad()
    {
        On_SoundEngine.Update += UpdateLoopedSounds;
    }

    private void UpdateLoopedSounds(On_SoundEngine.orig_Update orig)
    {
        if (!SoundEngine.IsAudioSupported)
            return;

        for (int i = loopedSounds.Count; i > 0; i--)
        {
            LoopedSoundInstance instance = loopedSounds[i];
            if (instance.HasLoopSoundBeenStarted && !instance.IsBeingPlayed)
                instance.Restart();

            if (instance.AutomaticTerminationCondition() || instance.HasBeenStopped)
            {
                instance.Stop();
                loopedSounds.RemoveAt(i);
            }
        }

        /*
        // Go through all looped sounds and perform automatic cleanup.
        loopedSounds.RemoveAll(s =>
        {
            // If the sound was started but is no longer playing, restart it.
            bool shouldBeRemoved = false;
            if (s.HasLoopSoundBeenStarted && !s.IsBeingPlayed)
                s.Restart();

            // If the sound's termination condition has been activated, remove the sound.
            if (s.AutomaticTerminationCondition())
                shouldBeRemoved = true;

            // If the sound has been stopped, remove it.
            if (s.HasBeenStopped)
                shouldBeRemoved = true;

            // If the sound will be removed, mark it as stopped.
            if (shouldBeRemoved)
                s.Stop();

            return shouldBeRemoved;
        });
        */

        orig();
    }

    public static LoopedSoundInstance CreateNew(SoundStyle loopingSound, Func<bool> automaticTerminationCondition = null)
    {
        LoopedSoundInstance sound = new(loopingSound, automaticTerminationCondition ?? (() => false));
        loopedSounds.Add(sound);

        return sound;
    }

    public static LoopedSoundInstance CreateNew(SoundStyle startingSound, SoundStyle loopingSound, Func<bool> automaticTerminationCondition = null)
    {
        LoopedSoundInstance sound = new(startingSound, loopingSound, automaticTerminationCondition ?? (() => false));
        loopedSounds.Add(sound);

        return sound;
    }
}

public class LoopedSoundInstance
{
    private readonly SoundStyle? startSoundStyle;

    private readonly SoundStyle loopSoundStyle;

    // Useful for cases where a sound is emitted by an entity but should cease when that entity is dead.
    // I've had far too many headache inducing cases where looped sounds refuse to go away after their producer stopped existing.
    // This condition is checked in a central manager instead of in the Update method because if an entity is responsible for updating then naturally
    // it'll be too late for Update to cleanly dispose of this sound instance.
    public Func<bool> AutomaticTerminationCondition
    {
        get;
        private set;
    }

    public SlotId StartingSoundSlot
    {
        get;
        private set;
    }

    public SlotId LoopingSoundSlot
    {
        get;
        private set;
    }

    public bool UsesStartingSound => startSoundStyle is not null;

    public bool HasStartingSoundBeenStarted
    {
        get;
        private set;
    }

    public bool HasLoopSoundBeenStarted
    {
        get;
        private set;
    }

    public bool HasBeenStopped
    {
        get;
        internal set;
    }

    public bool IsBeingPlayed => SoundEngine.TryGetActiveSound(LoopingSoundSlot, out _);

    // This constructor should not be used manually. Rather, sound instances should be created via the LoopedSoundManager's utilities, since that ensures that the sound is
    // properly logged by the manager.
    internal LoopedSoundInstance(SoundStyle loopingSound, Func<bool> automaticTerminationCondition)
    {
        loopSoundStyle = loopingSound;
        AutomaticTerminationCondition = automaticTerminationCondition;
        LoopingSoundSlot = SlotId.Invalid;
        StartingSoundSlot = SlotId.Invalid;
    }

    internal LoopedSoundInstance(SoundStyle startingSound, SoundStyle loopingSound, Func<bool> automaticTerminationCondition) : this(loopingSound, automaticTerminationCondition)
    {
        startSoundStyle = startingSound;
    }

    public void Update(Vector2 soundPosition, Action<ActiveSound> soundUpdateStep = null)
    {
        // Start the sound if it hasn't been activated yet.
        // If a starting sound should be used, play that first, and wait for it to end before playing the looping sound.
        if (!HasLoopSoundBeenStarted && !IsBeingPlayed)
        {
            bool waitingForStartingSoundToEnd = !HasStartingSoundBeenStarted || (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s) && s.IsPlaying);
            if (!UsesStartingSound)
                waitingForStartingSoundToEnd = false;

            if (!waitingForStartingSoundToEnd)
            {
                LoopingSoundSlot = SoundEngine.PlaySound(loopSoundStyle with { MaxInstances = 0, IsLooped = true }, soundPosition);
                HasLoopSoundBeenStarted = true;
                HasStartingSoundBeenStarted = true;
            }
            else if (UsesStartingSound && !HasStartingSoundBeenStarted)
            {
                StartingSoundSlot = SoundEngine.PlaySound(startSoundStyle.Value with { MaxInstances = 0 }, soundPosition);
                HasStartingSoundBeenStarted = true;
            }
        }

        // Keep the sound(s) updated.
        if (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s1))
        {
            s1.Position = soundPosition;
            soundUpdateStep?.Invoke(s1);
        }
        if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound s2))
        {
            s2.Position = soundPosition;
            soundUpdateStep?.Invoke(s2);
        }
        else if (!HasBeenStopped)
            HasLoopSoundBeenStarted = false;
    }

    public void Restart() => HasLoopSoundBeenStarted = false;

    public void Stop()
    {
        if (HasBeenStopped)
            return;

        if (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s1))
            s1?.Stop();
        if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound s2))
            s2?.Stop();

        HasBeenStopped = true;
    }
}