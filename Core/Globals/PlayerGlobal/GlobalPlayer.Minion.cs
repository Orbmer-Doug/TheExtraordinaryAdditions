namespace TheExtraordinaryAdditions.Core.Globals;

public partial class GlobalPlayer
{
    public bool LaserDrones;
    public bool Loki;
    public bool SuperLoki;
    public bool Avragen;
    public bool Flare;

    public void ResetMinion()
    {
        LaserDrones = Loki = SuperLoki = Avragen = Flare = false;
    }
}
