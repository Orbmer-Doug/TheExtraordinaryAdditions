using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Graphics.Systems;

public class BossBackgroundsSystem : ModSystem
{
    public override void OnModLoad()
    {
        Main.QueueMainThreadAction(static () => { On_Main.DrawSurfaceBG += DrawFog; });
    }

    public override void OnModUnload()
    {
        Main.QueueMainThreadAction(static () => { On_Main.DrawSurfaceBG -= DrawFog; });
    }

    private static void DrawFog(On_Main.orig_DrawSurfaceBG orig, Main self)
    {
        orig(self);

        if (Main.gameMenu)
            return;

        int stygianIndex = NPC.FindFirstNPC(ModContent.NPCType<StygainHeart>());
        if (stygianIndex == -1)
            return;

        // Draw the fog if necessary.
        float fogInterpolant = Main.npc[stygianIndex].AdditionsInfo().ExtraAI[StygainHeart.FogInterpolantIndex];
        if (!(fogInterpolant > 0f))
            return;
        SpriteBatch sb = Main.spriteBatch;

        Texture2D noise = AssetRegistry.GennedTextures.DarkTurbulentNoise;
        ManagedShader fog = AssetRegistry.GennedShaders.FogShader;

        fog.TrySetParameter("opacity", fogInterpolant);
        fog.TrySetParameter("time", Main.GlobalTimeWrappedHourly * .2f);

        sb.EnterShaderRegion(fog.Effect);
        fog.Render();
        Point size = new(Main.graphics.GraphicsDevice.Viewport.Width,
            Main.graphics.GraphicsDevice.Viewport.Height);
        sb.Draw(noise, new Rectangle(0, 0, size.X, size.Y), null, Color.White * fogInterpolant, 0f, Vector2.Zero, 0,
            0f);
        sb.ResetToDefault();
    }
}
