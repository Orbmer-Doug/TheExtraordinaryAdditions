using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Core.Graphics.Specific;

// Inspired from: https://github.com/LucilleKarma/WrathOfTheGodsPublic/blob/main/Content/NPCs/Bosses/NamelessDeity/SpecificEffectManagers/LightSlashDrawer.cs
public class ChasersDrawer : ModSystem
{
    public static int ContinueRenderingCountdown
    {
        get;
        private set;
    }

    public static ManagedRenderTarget ChaserTarget
    {
        get;
        private set;
    }

    public static ManagedRenderTarget ChaserTargetPrevious
    {
        get;
        private set;
    }

    public override void OnModLoad()
    {
        RenderTargetManager.RenderTargetUpdateLoopEvent += PrepareAfterimageTarget;
        Main.QueueMainThreadAction(static () =>
        {
            ChaserTarget = new(true, ManagedRenderTarget.CreateScreenSizedTarget);
            ChaserTargetPrevious ??= new(true, ManagedRenderTarget.CreateScreenSizedTarget);
        });
    }

    private void PrepareAfterimageTarget()
    {
        if (ContinueRenderingCountdown > 0)
            ContinueRenderingCountdown--;

        bool active = AnyProjectile(ModContent.ProjectileType<LuminescentChaser>());

        if (!active && ContinueRenderingCountdown <= 0)
            return;

        if (active)
            ContinueRenderingCountdown = 130;

        GraphicsDevice gd = Main.instance.GraphicsDevice;

        // Prepare the render target for drawing.
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp, DepthStencilState.Default, RasterizerState.CullNone, null, Matrix.Identity);
        gd.SetRenderTarget(ChaserTargetPrevious);
        gd.Clear(Color.Transparent);

        // Draw the contents of the previous frame to the target
        // The color represents exponential decay factors for each RGBA component
        Main.spriteBatch.Draw(ChaserTarget, Vector2.Zero, new(0.52f, 0.95f, 0.95f, 0.85f));

        // Draw the blur shader to the result
        ApplyBlurEffects();

        // Draw all chaers to the render target
        foreach (Projectile p in Main.ActiveProjectiles)
        {
            if (p.type == ModContent.ProjectileType<LuminescentChaser>())
                p.As<LuminescentChaser>().DrawToTarget();
        }

        // Return to the backbuffer
        Main.spriteBatch.End();
        gd.SetRenderTarget(null);

        PrepareScreenShader();
    }

    public static void ApplyBlurEffects()
    {
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        gd.SetRenderTarget(ChaserTarget);
        gd.Clear(Color.Transparent);

        ManagedScreenShader afterimageShader = ShaderRegistry.GaussianBlur;
        afterimageShader.TrySetParameter("blurOffset", .00132f);
        afterimageShader.TrySetParameter("colorMask", Color.Blue.ToVector4());
        afterimageShader.Apply();

        Main.spriteBatch.Draw(ChaserTargetPrevious, Vector2.Zero, Color.White);
    }

    public static void PrepareScreenShader()
    {
        ManagedScreenShader shader = AssetRegistry.GetFilter("ChasersOverlay");
        shader.TrySetParameter("splitBrightnessFactor", 3.2f);
        shader.TrySetParameter("splitTextureZoomFactor", 0.75f);
        shader.TrySetParameter("backgroundOffset", (Main.screenPosition - Main.screenLastPosition) / Main.ScreenSize.ToVector2());
        shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        shader.SetTexture(ChaserTarget, 1, SamplerState.AnisotropicClamp);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos2), 2, SamplerState.AnisotropicWrap);
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 3, SamplerState.AnisotropicWrap);
        shader.Activate();
    }
}