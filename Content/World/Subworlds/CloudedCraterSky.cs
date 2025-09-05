using Microsoft.Xna.Framework.Graphics;
using SubworldLibrary;
using System;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Events;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;
using Terraria.ID;
using Terraria.Enums;

namespace TheExtraordinaryAdditions.Content.World.Subworlds;

public class CloudyCraterSky : CustomSky
{
    public const string Key = "TheExtraordinaryAdditions:CloudyBackground";
    public const string ShaderKey = "CloudyBackground";

    public float BackgroundIntensity;
    public static bool CanSkyBeActive
    {
        get
        {
            return SubworldSystem.IsActive<CloudedCrater>();
        }
    }

    public static float CurrentIntensity
    {
        get
        {
            return 1f;
        }
    }

    public static readonly Color DrawColor = new(0.46f, 0.46f, 0.46f);

    public override void Update(GameTime gameTime)
    {
        if (!CanSkyBeActive)
        {
            BackgroundIntensity = Clamp(BackgroundIntensity - 0.08f, 0f, 1f);
            Deactivate([]);
            return;
        }

        BackgroundIntensity = Clamp(BackgroundIntensity + 0.01f, 0f, 1f);

        Opacity = BackgroundIntensity;
    }

    public override Color OnTileColor(Color inColor) => new(Vector4.Lerp(DrawColor.ToVector4(), inColor.ToVector4(), 1f - BackgroundIntensity));

    public static void DrawGreySky()
    {
        Vector2 screenSize = new(instance.GraphicsDevice.Viewport.Width, instance.GraphicsDevice.Viewport.Height);

        #region Vanilla Sky Calculations
        bool dayTime = Main.dayTime;
        float dayCompletion = (float)(time / dayLength);
        float nightCompletion = (float)(time / nightLength);

        int screenWidth = instance.GraphicsDevice.Viewport.Width;
        int screenHeight = instance.GraphicsDevice.Viewport.Height;
        float ForcedMinimumZoom = Main.ForcedMinimumZoom;
        Texture2D sunTexture = TextureAssets.Sun.Value;

        // In this case we won't be using it in the shader because it looks weird.
        Texture2D moonTexture = TextureAssets.Moon[moonType].Value;

        // Scene area calculations
        int num13 = screenWidth;
        int num14 = screenHeight;
        Vector2 zero = Vector2.Zero;
        if (num13 < 800)
        {
            int num15 = 800 - num13;
            zero.X -= num15 * 0.5f;
            num13 = 800;
        }
        if (num14 < 600)
        {
            int num16 = 600 - num14;
            zero.Y -= num16 * 0.5f;
            num14 = 600;
        }
        SceneArea sceneArea = new()
        {
            bgTopY = 0,
            totalWidth = num13,
            totalHeight = num14,
            SceneLocalScreenPositionOffset = zero
        };

        // Sun and Moon positions
        int num2 = sceneArea.bgTopY;
        int sunX = (int)(dayCompletion * (sceneArea.totalWidth + sunTexture.Width * 2)) - sunTexture.Width;
        int sunY = 0;
        float sunScale = 1f;
        int moonX = (int)(nightCompletion * (sceneArea.totalWidth + moonTexture.Width * 2)) - moonTexture.Width;
        int moonY = 0;
        float moonScale = 1f;

        if (dayTime)
        {
            double num10;
            if (dayCompletion < .5f)
            {
                num10 = Math.Pow(1.0 - dayCompletion * 2.0, 2.0);
                sunY = (int)(num2 + num10 * 250.0 + 180.0);
            }
            else
            {
                num10 = Math.Pow((dayCompletion - 0.5) * 2.0, 2.0);
                sunY = (int)(num2 + num10 * 250.0 + 180.0);
            }
            sunScale = (float)(1.2 - num10 * 0.4);
            sunScale *= ForcedMinimumZoom;
            sunScale *= 1.1f;
        }
        else
        {
            double num11;
            if (nightCompletion < .5f)
            {
                num11 = Math.Pow(1.0 - nightCompletion * 2.0, 2.0);
                moonY = (int)(num2 + num11 * 250.0 + 180.0);
            }
            else
            {
                num11 = Math.Pow((nightCompletion - 0.5) * 2.0, 2.0);
                moonY = (int)(num2 + num11 * 250.0 + 180.0);
            }
            moonScale = (float)(1.2 - num11 * 0.4);
            moonScale *= ForcedMinimumZoom;
        }

        // Convert pixel positions to normalized screen coordinates (0.0 to 1.0)
        Vector2 sunPosition = new(
            (sunX + sceneArea.SceneLocalScreenPositionOffset.X) / sceneArea.totalWidth,
            (sunY + sceneArea.SceneLocalScreenPositionOffset.Y) / sceneArea.totalHeight
        );
        Vector2 moonPosition = new(
        (moonX + sceneArea.SceneLocalScreenPositionOffset.X) / sceneArea.totalWidth,
        (moonY + sceneArea.SceneLocalScreenPositionOffset.Y) / sceneArea.totalHeight
        );

        float intensity = dayTime ? Clamp(Convert01To101(dayCompletion) * 2f, 1f, 2f) : Clamp(Convert01To010(nightCompletion), .6f, 1f);
        #endregion

        ManagedShader cloudShader = AssetRegistry.GetShader(ShaderKey);

        //Color ballColor = GetBackgroundColors(out _, out _);
        //MoonPhase phase = Main.GetMoonPhase();
        cloudShader.TrySetParameter("SunPosition", sunPosition);
        cloudShader.TrySetParameter("MoonPosition", moonPosition);
        cloudShader.TrySetParameter("IsDay", dayTime);
        cloudShader.TrySetParameter("GravDir", LocalPlayer.gravDir == 1 ? false : true);
        cloudShader.TrySetParameter("Time", GlobalTimeWrappedHourly);
        cloudShader.TrySetParameter("SkyColor", new Vector4(Vector3.Max(new Vector3(.2f, .2f, .2f), ColorOfTheSkies.ToVector3()), intensity));
        cloudShader.TrySetParameter("Parallax", screenPosition * (caveParallax * new Vector2(.3f, .175f)));
        cloudShader.TrySetParameter("ScreenRes", Main.graphics.GraphicsDevice.Viewport.Bounds.Size());
        cloudShader.Render();

        Texture2D pix = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        Vector2 drawPosition = screenSize * 0.5f;
        Vector2 skyScale = screenSize / pix.Size();
        spriteBatch.Draw(pix, drawPosition, null, Color.White, 0f, pix.Size() * 0.5f, skyScale, 0, 0f);
    }

    public override void Draw(SpriteBatch spriteBatch, float minDepth, float maxDepth)
    {
        if (!CanSkyBeActive)
            return;

        // Draw in the foreground
        if (maxDepth >= float.MaxValue || minDepth < float.MaxValue)
        {
            Matrix backgroundMatrix = BackgroundViewMatrix.TransformationMatrix;
            Vector3 translationDirection = new(1f, BackgroundViewMatrix.Effects.HasFlag(SpriteEffects.FlipVertically) ? -1f : 1f, 1f);
            backgroundMatrix.Translation -= BackgroundViewMatrix.ZoomMatrix.Translation * translationDirection;

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Rasterizer, null, backgroundMatrix);
            DrawGreySky();
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, DefaultSamplerState, DepthStencilState.None, Rasterizer, null, backgroundMatrix);
        }
    }

    public override float GetCloudAlpha() => 0f;

    public override void Reset() { }

    public override void Activate(Vector2 position, params object[] args) { }

    public override void Deactivate(params object[] args) { }

    public override bool IsActive()
    {
        return !gameMenu && CanSkyBeActive;
    }
}

public class CloudedCraterScreenShaderData(string passName) : ScreenShaderData(passName)
{
    public override void Apply()
    {
        if (SubworldSystem.IsActive<CloudedCrater>())
            UseTargetPosition(screenPosition + new Vector2(screenWidth * 0.5f, screenHeight * 0.5f));

        base.Apply();
    }

    public override void Update(GameTime gameTime)
    {
        if (!CloudyCraterSky.CanSkyBeActive)
            Filters.Scene[CloudyCraterSky.Key].Deactivate(args: []);
    }
}

public class CloudedCraterBackgroundScene : ModSceneEffect
{
    public override SceneEffectPriority Priority => SceneEffectPriority.BossHigh;

    public override bool IsSceneEffectActive(Player player) => CloudyCraterSky.CanSkyBeActive;

    public override void Load()
    {
        Filters.Scene[CloudyCraterSky.Key] = new Filter(new CloudedCraterScreenShaderData("FilterMiniTower").UseColor(CloudyCraterSky.DrawColor).UseOpacity(0.25f), EffectPriority.VeryHigh);
        SkyManager.Instance[CloudyCraterSky.Key] = new CloudyCraterSky();
        SkyManager.Instance[CloudyCraterSky.Key].Load();
    }

    public override void SpecialVisuals(Player player, bool isActive)
    {
        player.ManageSpecialBiomeVisuals(CloudyCraterSky.Key, isActive);
        if (isActive)
            SkyManager.Instance.Activate(CloudyCraterSky.Key, player.Center);
        else
            SkyManager.Instance.Deactivate(CloudyCraterSky.Key);
    }
}