using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;

public sealed class PlayerMovement : ModPlayer
{
    public bool FastFall;
    public bool DisableXMovement;
    public bool DisableYMovement;

    public bool DisableAllMovement
    {
        set => DisableXMovement = DisableYMovement = value;
    }

    public override void PreUpdateMovement()
    {
        if (DisableXMovement)
        {
            Player.velocity.X = 0f;
            DisableXMovement = false;
        }

        if (DisableYMovement)
        {
            Player.velocity.Y = 0f;
            DisableYMovement = false;
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (!FastFall)
            return;

        Player.maxFallSpeed = 400f;
        Player.fallStart = Player.fallStart2 = 2000;
        Player.noFallDmg = true;

        FastFall = false;
    }
}
