namespace TheExtraordinaryAdditions.Core.Globals;

public partial class GlobalPlayer
{
    public bool EternalRested;
    public bool AridFlask;
    public bool FrigidTonic;
    public bool DentedBySpoon;
    public bool Overheat;
    public bool BigOxygen;

    public void ResetBuffs()
    {
        EternalRested = AridFlask = FrigidTonic = DentedBySpoon = Overheat = BigOxygen = false;
    }
}
