using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

public sealed class PlayerMinion : ModPlayer
{
    public bool LaserDrones;
    public bool Loki;
    public bool SuperLoki;
    public bool Avragen;
    public bool Flare;

    public override void Load() => ResetMinion();

    public override void ResetEffects() => ResetMinion();

    public void ResetMinion()
    {
        LaserDrones = Loki = SuperLoki = Avragen = Flare = false;
    }
}
