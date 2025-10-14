using System.Collections.Generic;

namespace TheExtraordinaryAdditions.Core.Globals;

public partial class GlobalPlayer
{
    public enum AdditionsMinion : int
    {
        LaserDrones,
        Loki,
        SuperLoki,
        Avragen,
        Flare,
    }
    internal Dictionary<AdditionsMinion, bool> Minion = [];

    public void ResetMinion()
    {
        for (int i = 0; i < (int)GetLastEnumValue<AdditionsMinion>() + 1; i++)
            Minion[(AdditionsMinion)i] = false;
    }
}
