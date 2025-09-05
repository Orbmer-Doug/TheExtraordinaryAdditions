using ReLogic.Utilities;
using System;
using Terraria.Audio;

namespace TheExtraordinaryAdditions.Assets.Audio;

public class LoopedSound(SoundStyle soundStyle, Func<bool> activeCondition)
{
    private SoundStyle style = soundStyle;

    private SlotId slot = SlotId.Invalid;

    private readonly Func<bool> condition = activeCondition;

    public void Update(Func<Vector2> position, Func<float> volume, Func<float> pitch)
    {
        bool num = SoundEngine.TryGetActiveSound(slot, out ActiveSound activeSound);
        if ((!num || !slot.IsValid) && condition())
        {
            slot = SoundEngine.PlaySound(style, (Vector2?)position(), delegate (ActiveSound sound)
            {
                if (sound != null)
                {
                    sound.Position = position();
                    sound.Volume = volume();
                    sound.Pitch = pitch();
                }
                return condition();
            });
        }
        if (num)
        {
            activeSound.Position = position();
            activeSound.Volume = volume();
            activeSound.Pitch = pitch();
        }
    }
}
