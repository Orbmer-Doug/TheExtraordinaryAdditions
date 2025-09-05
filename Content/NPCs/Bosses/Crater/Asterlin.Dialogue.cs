using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.ManagedRenderTarget;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin
{
    private ManagedRenderTarget crtTarget;
    private ManagedShader crtShader;
    private static readonly RenderTargetInitializationAction TargetInitializer = (width, height) => new RenderTarget2D(Main.instance.GraphicsDevice, width, height);
    public DialogueManager Dialogue_Manager;
    public float Dialogue_ScreenInterpolant;
    public bool Dialogue_FindingChannel;

    public static readonly Color Dialogue_InfoColor = Color.Goldenrod;
    public static readonly Color Dialogue_WarningColor = Color.Red;
    public static readonly Color Dialogue_StatusColor = Color.DeepSkyBlue;
    public TextSnippet Dialogue_Pause => new(" ", Color.Transparent, .5f);
    public TextSnippet Dialogue_LongPause => new(" ", Color.Transparent, 3.6f);
    public string GetIngameTime()
    {
        const double fullDayLength = Main.dayLength + Main.nightLength;
        double num5 = Main.time;
        if (!Main.dayTime)
            num5 += Main.dayLength;
        num5 = Main.time / fullDayLength * 24.0;
        double num6 = 7.5;
        num5 = Main.time - num6 - 12.0;
        if (Main.time < 0.0)
            num5 += 24.0;
        int num7 = (int)(double)Main.time;
        double num8 = Main.time - (double)num7;
        num8 = (int)(num8 * 60.0);
        string text4 = string.Concat(num8);
        if (num8 < 10.0)
            text4 = "0" + text4;
        if (num7 > 12)
            num7 -= 12;
        if (num7 == 0)
            num7 = 12;
        text4 = ((!(num8 < 30.0)) ? "30" : "00");
        return num7 + ":" + text4;
    }

    public AwesomeSentence FullDialogue => new(1000f, [
        new TextSnippet(this.GetLocalization("OverheatTemperatureWarning").Format(GetIngameTime()), Dialogue_WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatImminent").Format(GetIngameTime()), Dialogue_WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatHeatsinks").Format(GetIngameTime()), Dialogue_InfoColor, .025f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
        new TextSnippet(this.GetLocalization($"OverheatEyeScan").Format(GetIngameTime()), Dialogue_StatusColor, .02f, null, null, true, 2.4f),
         Dialogue_Pause,
         new TextSnippet(this.GetLocalization($"OverheatEyeScan1").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatEyeScan2").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatEyeScan3").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(this.GetLocalization($"OverheatLegScans").Format(GetIngameTime()), Dialogue_StatusColor, .02f, null, null, true, 2.4f),
         Dialogue_Pause,
         new TextSnippet(this.GetLocalization($"OverheatLegScans1").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatLegScans2").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatLegScans3").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(this.GetLocalization($"OverheatArmScans").Format(GetIngameTime()), Dialogue_StatusColor, .02f, null, null, true, 2.4f),
         Dialogue_Pause,
         new TextSnippet(this.GetLocalization($"OverheatArmScans1").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization($"OverheatArmScans2").Format(GetIngameTime()), Dialogue_StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(this.GetLocalization($"OverheatUhOh").Format(GetIngameTime()), Dialogue_InfoColor, .037f, TextSnippet.AppearFadingFromRight, TextSnippet.RandomDisplacement, true, 2.4f),
        Dialogue_LongPause
    ]);

    public AwesomeSentence LeavingDialogue => new(1000f, [
        new TextSnippet("", Dialogue_StatusColor)
        ]);

    public void LoadDialogue()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        Main.QueueMainThreadAction(() =>
        {
            crtTarget = new ManagedRenderTarget(true, TargetInitializer, true);

            // Initialize target
            GraphicsDevice device = Main.instance.GraphicsDevice;
            device.SetRenderTarget(crtTarget);
            device.Clear(Color.Transparent);
            device.SetRenderTarget(null);
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTarget;
        On_Main.DrawProjectiles += DrawTheTarget;
    }

    public void UnloadDialogue()
    {
        Main.QueueMainThreadAction(() =>
        {
            crtTarget?.Dispose();
            crtTarget = null;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= DrawToTarget;
        On_Main.DrawProjectiles -= DrawTheTarget;
    }

    // Despite being in the same file as Asterlin, the render target knows nothing other than its own target and shader
    // So for any outside changes to variables they must be yoinked from the raw npc to reflect changes
    private void DrawToTarget()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        if (!FindNPC(out NPC npc, ModContent.NPCType<Asterlin>()))
            return;
        Asterlin asterlin = npc.As<Asterlin>();
        if (asterlin.CurrentState != AsterlinAIType.DesperationDrama)
            return;

        GraphicsDevice device = Main.instance.GraphicsDevice;
        Vector2 resolution = new(Main.screenWidth, Main.screenHeight);

        device.SetRenderTarget(crtTarget);
        device.Clear(Color.Transparent);

        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Matrix.Identity);

        // Initial background
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Main.spriteBatch.DrawBetterRect(tex, ToTarget(Main.screenPosition, resolution), null, Color.Black, 0f, Vector2.Zero, SpriteEffects.None, false);

        // Actual text
        asterlin.Dialogue_Manager?.Draw();

        Main.spriteBatch.End();
    }

    private void DrawTheTarget(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.netMode == NetmodeID.Server)
            return;

        if (!FindNPC(out NPC npc, ModContent.NPCType<Asterlin>()))
            return;
        Asterlin asterlin = npc.As<Asterlin>();
        if (asterlin.CurrentState != AsterlinAIType.DesperationDrama)
            return;

        crtShader = AssetRegistry.GetShader("AsterlinScreen");
        crtShader.TrySetParameter("time", Main.GlobalTimeWrappedHourly);
        crtShader.TrySetParameter("findingChannel", asterlin.Dialogue_FindingChannel);
        crtShader.TrySetParameter("resolution", new Vector2(800f, 600f));

        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, crtShader.Effect, Main.GameViewMatrix.TransformationMatrix);

        float x = 800f * Animators.MakePoly(3f).InFunction(asterlin.Dialogue_ScreenInterpolant);
        float y = 600f * Animators.MakePoly(12f).InFunction(asterlin.Dialogue_ScreenInterpolant);
        Vector2 size = new Vector2(x, y);
        Vector2 pos = asterlin.NPC.Center - Vector2.UnitY * 450f;
        Main.spriteBatch.DrawBetterRect(crtTarget, ToTarget(pos, size), null, Color.White, 0f, crtTarget.Size() / 2f, SpriteEffects.None, false);

        Main.spriteBatch.End();
    }
}
