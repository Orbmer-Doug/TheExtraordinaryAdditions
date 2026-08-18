using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Resources;

namespace TheExtraordinaryAdditions.Core.Graphics.Systems;

public class ImpactSystem : CustomSky
{
    public const string Key = "ForbiddenShapes:ImpactSky";

    public static TimeSpan DrawCooldown { get; set; }

    public static bool InProximityOfMonolith { get; set; }

    public static TimeSpan LastFrameElapsedGameTime { get; set; }

    public static int TimeSinceCloseToMonolith { get; set; }

    private static int ImpactTimer { get; set; }
    private static bool isActive => ImpactTimer > 0;

    public static void QueueImpact(int frames)
    {
        SkyManager.Instance.Activate(Key, Main.LocalPlayer.Center, frames);
    }

    public override void Activate(Vector2 position, params object[] args)
    {
        ImpactTimer = (int) args[0];
    }

    public override void Deactivate(params object[] args)
    {
    }

    public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
    {
        // Make the background only draw once per frame
        DrawCooldown -= LastFrameElapsedGameTime;
        bool invalidDepth = minDepth >= -1000000f;

        if (invalidDepth || (DrawCooldown.TotalMilliseconds >= 17 && Main.instance.IsActive))
            return;

        Main.spriteBatch.End();
        Main.spriteBatch.Begin();

        DrawCooldown = TimeSpan.FromSeconds(1D / 60D);

        // Pure-green pixels are replaced in the screen shader
        Texture2D pix = AssetRegistry.GennedTextures.Pixel;
        Vector2 screenArea = Main.instance.GraphicsDevice.Viewport.Bounds.Size();
        Vector2 textureArea = screenArea * 2f;
        Vector2 drawPosition = screenArea * 0.5f;
        Main.spriteBatch.Draw(pix, drawPosition, null, new Color(0f, 1f, 0f), 0f, pix.Size() * 0.5f,
            textureArea / pix.Size(), 0, 0f);

        ManagedScreenShader filter = AssetRegistry.GennedShaders.Screen.ImpactFilter;
        filter.TrySetParameter("silhouetteColor", Color.Black);
        filter.TrySetParameter("foregroundColor", Color.White);
        filter.Activate();

        Main.spriteBatch.ResetToDefault();
    }

    public override float GetCloudAlpha() => 1f;

    public override bool IsActive()
    {
        return isActive;
    }

    public override Color OnTileColor(Color inColor)
    {
        return inColor;
    }

    public override void Reset()
    {
    }

    public override void Update(GameTime gameTime)
    {
        LastFrameElapsedGameTime = gameTime.ElapsedGameTime;

        if (!Main.gamePaused && Main.instance.IsActive)
            TimeSinceCloseToMonolith++;
        if (TimeSinceCloseToMonolith >= 10)
            InProximityOfMonolith = false;

        if (isActive)
        {
            SkyManager.Instance["Ambience"].Deactivate();
            SkyManager.Instance["Party"].Deactivate();
            ImpactTimer--;
        }
    }
}

public class ImpactSkyScene : ModSceneEffect
{
    public override bool IsSceneEffectActive(Player player) => ImpactSystem.InProximityOfMonolith;

    public override void SpecialVisuals(Player player, bool isActive)
    {
        player.ManageSpecialBiomeVisuals(ImpactSystem.Key, isActive);
    }

    public override void Load()
    {
        SkyManager.Instance[ImpactSystem.Key] = new ImpactSystem();
        Filters.Scene[ImpactSystem.Key] =
            new Filter(new GenericScreenShaderData("FilterMiniTower").UseColor(Color.Transparent).UseOpacity(0f),
                EffectPriority.VeryHigh);
    }
}

public sealed class GenericScreenShaderData : ScreenShaderData
{
    public GenericScreenShaderData(string passName)
        : base(passName)
    {
    }

    public GenericScreenShaderData(Asset<Effect> shader, string passName)
        : base(shader, passName)
    {
    }

    public override void Apply()
    {
        UseTargetPosition(Main.LocalPlayer.Center);
        UseColor(Color.Transparent);
        base.Apply();
    }
}
