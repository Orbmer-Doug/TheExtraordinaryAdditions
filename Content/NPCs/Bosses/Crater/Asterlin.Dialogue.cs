using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.ManagedRenderTarget;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

public partial class Asterlin
{
    private static ManagedRenderTarget crtTarget;
    private static ManagedShader crtShader;
    private static readonly RenderTargetInitializationAction TargetInitializer = (width, height) => new RenderTarget2D(Main.instance.GraphicsDevice, width, height);
    public DialogueManager Dialogue_Manager;
    public float Dialogue_ScreenInterpolant;
    public bool Dialogue_FindingChannel;

    public void Dialouge_SendExtraAI(BinaryWriter wr)
    {
        wr.Write((float)Dialogue_ScreenInterpolant);
        wr.Write((bool)Dialogue_FindingChannel);
    }
    public void Dialogue_RecieveExtraAI(BinaryReader re)
    {
        Dialogue_ScreenInterpolant = (float)re.ReadSingle();
        Dialogue_FindingChannel = (bool)re.ReadBoolean();
    }

    public static readonly Color Dialogue_InfoColor = Color.Goldenrod;
    public static readonly Color Dialogue_WarningColor = Color.Red;
    public static readonly Color Dialogue_StatusColor = Color.DeepSkyBlue;
    public static readonly TextSnippet Dialogue_Pause = new(" ", Color.Transparent, .5f);
    public static readonly TextSnippet Dialogue_LongPause = new(" ", Color.Transparent, 3.6f);
    public AwesomeSentence FullDialogue => new(1000f, [
        new TextSnippet(this.GetLocalization("OverheatTemperatureWarning").Value, Dialogue_WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatImminent").Value, Dialogue_WarningColor, .045f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatHeatsinks").Value, Dialogue_InfoColor, .025f, TextSnippet.AppearFadingFromRight, null, true, 2.4f),
        new TextSnippet(this.GetLocalization("OverheatEyeScan").Value, Dialogue_StatusColor, .02f, null, null, true, 2.4f),
         Dialogue_Pause,
         new TextSnippet(this.GetLocalization("OverheatEyeScan1").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatEyeScan2").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatEyeScan3").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(this.GetLocalization("OverheatLegScans").Value, Dialogue_StatusColor, .02f, null, null, true, 2.4f),
         Dialogue_Pause,
         new TextSnippet(this.GetLocalization("OverheatLegScans1").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatLegScans2").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatLegScans3").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(this.GetLocalization("OverheatArmScans").Value, Dialogue_StatusColor, .02f, null, null, true, 2.4f),
         Dialogue_Pause,
         new TextSnippet(this.GetLocalization("OverheatArmScans1").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
         new TextSnippet(this.GetLocalization("OverheatArmScans2").Value, Dialogue_StatusColor, .015f, null, null, true, 2.4f),
        new TextSnippet(this.GetLocalization("OverheatUhOh").Value, Dialogue_InfoColor, .037f, TextSnippet.AppearFadingFromRight, TextSnippet.RandomDisplacement, true, 2.4f),
        Dialogue_LongPause
    ]);

    // Because multiplayer wants to be retarded
    public const int TimeToTemp = 191; // FullDialogue.GetTimeToSnippet(1);
    public const int TimeToHeatsink = 356; // FullDialogue.GetTimeToSnippet(2);
    public const int TimeToChange = 1089; // FullDialogue.GetTimeToSnippet(15);
    public const int TimeToUhOh = 1145; // FullDialogue.GetTimeToSnippet(16);
    public const int TimeToLast = 1201; // FullDialogue.GetTimeToSnippet(17);
    public const float DialogueTime = 1256.94f; // FullDialogue.MaxProgress * 60

    public void LoadDialogue()
    {
        if (Main.dedServ)
            return;

        Main.QueueMainThreadAction(() =>
        {
            crtTarget = new ManagedRenderTarget(true, TargetInitializer, true);

            // Initialize target
            GraphicsDevice device = Main.instance.GraphicsDevice;
            device.SetRenderTarget(crtTarget);
            device.Clear(Color.Transparent);
            device.SetRenderTarget(null);

            On_Main.DrawProjectiles += DrawTheTarget;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent += DrawToTarget;
    }

    public void UnloadDialogue()
    {
        Main.QueueMainThreadAction(() =>
        {
            crtTarget?.Dispose();
            crtTarget = null;
            On_Main.DrawProjectiles -= DrawTheTarget;
        });

        RenderTargetManager.RenderTargetUpdateLoopEvent -= DrawToTarget;
    }

    // Despite being in the same file as Asterlin, the render target knows nothing other than its own target and shader
    // So for any outside changes to variables they must be yoinked from the raw npc to reflect changes
    private static void DrawToTarget()
    {
        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.dedServ)
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

    private static void DrawTheTarget(On_Main.orig_DrawProjectiles orig, Main self)
    {
        orig(self);

        if (!AssetRegistry.HasFinishedLoading || Main.gameMenu || Main.dedServ)
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
