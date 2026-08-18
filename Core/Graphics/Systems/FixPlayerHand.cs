using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Core.Graphics.Systems;

// I forget what causes this so I'm just putting a messy band-aid on the players arm going wacky when a shader technique is applied
// red code
public sealed class FixPlayerHand : ModSystem
{
    public override void Load()
    {
        Main.QueueMainThreadAction(() =>
        {
            On_PlayerDrawLayers.DrawHeldProj += On_PlayerDrawLayersOnDrawHeldProj;
            On_PlayerDrawLayers.DrawPlayer_28_ArmOverItemComposite +=
                On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItemComposite;
        });
    }

    private void On_PlayerDrawLayersOnDrawPlayer_28_ArmOverItemComposite(
        On_PlayerDrawLayers.orig_DrawPlayer_28_ArmOverItemComposite orig, ref PlayerDrawSet drawinfo)
    {
        orig.Invoke(ref drawinfo);
        Main.spriteBatch.ResetToDefault();
    }

    private void On_PlayerDrawLayersOnDrawHeldProj(On_PlayerDrawLayers.orig_DrawHeldProj orig, PlayerDrawSet drawinfo,
        Projectile proj)
    {
        orig.Invoke(drawinfo, proj);
        Main.spriteBatch.ResetToDefault();
    }
}
