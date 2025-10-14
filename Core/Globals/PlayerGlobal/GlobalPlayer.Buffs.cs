using System.Collections.Generic;

namespace TheExtraordinaryAdditions.Core.Globals;

public partial class GlobalPlayer
{
    public enum AdditionsBuff : int
    {
        EternalRested,
        AridFlask,
        FrigidTonic,
        DentedBySpoon,
        Overheat,
        BigOxygen
    }
    internal Dictionary<AdditionsBuff, bool> Buffs = [];

    public void ResetBuffs()
    {
        for (int i = 0; i < (int)GetLastEnumValue<AdditionsBuff>() + 1; i++)
            Buffs[(AdditionsBuff)i] = false;
    }
}
