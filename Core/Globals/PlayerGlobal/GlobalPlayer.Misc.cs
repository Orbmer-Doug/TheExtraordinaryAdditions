namespace TheExtraordinaryAdditions.Core.Globals;

public partial class GlobalPlayer
{
    /// <summary>
    /// Acts as the <see cref="Main.GameUpdateCount"/> for a player without any arbitrary resets
    /// </summary>
    public uint GlobalTimer;

    public float HealingPotBonus = 1f;
    public bool LungingDown;
    public bool Teleport;
}
