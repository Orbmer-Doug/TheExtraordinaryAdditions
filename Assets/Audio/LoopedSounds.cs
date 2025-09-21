using ReLogic.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Assets.Audio;

public readonly struct AdditionsLoopedSound()
{
    public AdditionsLoopedSound(SoundStyle style, Func<float> volume = null, Func<float> pitch = null) : this()
    {
        Style = style with { MaxInstances = 0, IsLooped = true, PauseBehavior = PauseBehavior.PauseWithGame };
        Volume = volume ?? (() => 1f);
        Pitch = pitch ?? (() => 0f);
    }
    public AdditionsLoopedSound(AdditionsSound sound, Func<float> volume = null, Func<float> pitch = null) : this()
    {
        Style = AssetRegistry.GetSound(sound) with { MaxInstances = 0, IsLooped = true, PauseBehavior = PauseBehavior.PauseWithGame };
        Volume = volume ?? (() => 1f);
        Pitch = pitch ?? (() => 0f);
    }

    public static bool NPCNotActive(NPC npc) => !npc.active || Main.gameMenu || Main.dedServ || !SoundEngine.IsAudioSupported;
    public static bool ProjectileNotActive(Projectile proj) => !proj.active || Main.gameMenu || Main.dedServ || !SoundEngine.IsAudioSupported;

    public readonly SoundStyle Style;
    public readonly Func<float> Volume;
    public readonly Func<float> Pitch;
}

public sealed class LoopedSoundManager : ModSystem
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

        if (loopedSounds.Count == 0)
            return;

        // Go through all looped sounds and perform automatic cleanup
        loopedSounds.RemoveAll(s =>
        {
            // If the sound was started but is no longer playing, restart it
            bool shouldBeRemoved = false;
            if (s.HasLoopSoundBeenStarted && !s.IsBeingPlayed)
                s.Restart();

            // If the sound's termination condition has been activated, remove the sound
            if (s.TerminationCondition())
                shouldBeRemoved = true;

            // If the sound has been stopped, remove it
            if (s.HasBeenStopped)
                shouldBeRemoved = true;

            // If the sound will be removed, mark it as stopped
            if (shouldBeRemoved)
                s.Stop();

            return shouldBeRemoved;
        });

        orig();
    }

    public static LoopedSoundInstance CreateNew(AdditionsLoopedSound loopingSound, Func<bool> terminationCondition = null, Func<bool> activeCondition = null)
    {
        LoopedSoundInstance sound = new(loopingSound, terminationCondition ?? (() => false), activeCondition);
        loopedSounds.Add(sound);
        return sound;
    }

    public static LoopedSoundInstance CreateNew(AdditionsLoopedSound startingSound, AdditionsLoopedSound loopingSound, Func<bool> terminationCondition = null, Func<bool> activeCondition = null)
    {
        LoopedSoundInstance sound = new(startingSound, loopingSound, terminationCondition ?? (() => false), activeCondition);
        loopedSounds.Add(sound);
        return sound;
    }
}

public sealed class LoopedSoundInstance
{
    private readonly AdditionsLoopedSound? startSound;

    private readonly AdditionsLoopedSound loopSound;

    /// <summary>
    /// Useful for cases where a sound is emitted by an entity but should cease when that entity is gone
    /// </summary>
    public Func<bool> TerminationCondition
    {
        get;
        private set;
    }

    public Func<bool> ActiveCondition 
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

    public bool UsesStartingSound => startSound is not null;

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

    /// <summary>
    /// Do not use this constructor manually. Utilize <see cref="LoopedSoundManager.CreateNew(AdditionsLoopedSound, Func{bool})"/>
    /// </summary>
    internal LoopedSoundInstance(AdditionsLoopedSound loopingSound, Func<bool> terminationCondition, Func<bool> activeCondition = null)
    {
        loopSound = loopingSound;
        TerminationCondition = terminationCondition;
        ActiveCondition = activeCondition ?? (() => true); // Default to always active
        LoopingSoundSlot = SlotId.Invalid;
        StartingSoundSlot = SlotId.Invalid;
    }

    /// <summary>
    /// Do not use this constructor manually. Utilize <see cref="LoopedSoundManager.CreateNew(AdditionsLoopedSound, AdditionsLoopedSound, Func{bool})"/>
    /// </summary>
    internal LoopedSoundInstance(AdditionsLoopedSound startingSound, AdditionsLoopedSound loopingSound, Func<bool> terminationCondition, Func<bool> activeCondition = null)
        : this(loopingSound, terminationCondition, activeCondition)
    {
        startSound = startingSound;
    }

    public void Update(Vector2 soundPosition)
    {
        bool isActive = ActiveCondition();

        // Start the sound if it hasn't been activated yet
        // If a starting sound should be used, play that first, and wait for it to end before playing the looping sound
        if (!HasLoopSoundBeenStarted && isActive && !IsBeingPlayed)
        {
            bool hasStartEnded = !HasStartingSoundBeenStarted || (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s) && s.IsPlaying);
            if (!UsesStartingSound)
                hasStartEnded = false;

            if (!hasStartEnded)
            {
                LoopingSoundSlot = SoundEngine.PlaySound(loopSound.Style, soundPosition);
                HasLoopSoundBeenStarted = true;
                HasStartingSoundBeenStarted = true;
            }
            else if (UsesStartingSound && !HasStartingSoundBeenStarted)
            {
                StartingSoundSlot = SoundEngine.PlaySound(startSound.Value.Style, soundPosition);
                HasStartingSoundBeenStarted = true;
            }
        }

        // Keep the sounds updated
        if (SoundEngine.TryGetActiveSound(StartingSoundSlot, out ActiveSound s1))
        {
            s1.Position = soundPosition;
            s1.Volume = startSound.Value.Volume();
            s1.Pitch = startSound.Value.Pitch();
            if (!isActive)
                s1.Pause();
            else
                s1.Resume();
        }
        if (SoundEngine.TryGetActiveSound(LoopingSoundSlot, out ActiveSound s2))
        {
            s2.Position = soundPosition;
            s2.Volume = loopSound.Volume();
            s2.Pitch = loopSound.Pitch();
            if (!isActive)
                s2.Pause();
            else
                s2.Resume();
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