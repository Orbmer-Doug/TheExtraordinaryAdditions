using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Systems;

/// <summary>
/// For some reason any blendstate/shaders on the spritebatch screw with the arms
/// <br></br>
/// I dont know of a better way to currently remedy this but its mighty annoying
/// </summary>
public class ForceCorrectArmDraw : ModSystem
{
    public override void OnModLoad()
    {
        On_PlayerDrawLayers.DrawHeldProj += Modify;
    }

    public override void OnModUnload()
    {
        On_PlayerDrawLayers.DrawHeldProj -= Modify;
    }

    public static void Modify(On_PlayerDrawLayers.orig_DrawHeldProj main, PlayerDrawSet drawInfo, Projectile proj)
    {
        // Only fix for this mod
        if (proj.ModProjectile != null)
        {
            if (proj.ModProjectile.Mod.Name != AdditionsMain.Instance.Name)
                return;
        }
        
        // Force the blend state
        Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        main(drawInfo, proj);
        Main.spriteBatch.ResetBlendState();
    }
}
