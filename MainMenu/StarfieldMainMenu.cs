using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.MainMenu;

// TODO:
// credits visible?
// background?
// sun and moon dragging?
// one layer starfield in title logo bubble letters instead of icon
// 
public class StarfieldMainMenu : ModMenu
{
    public override bool IsAvailable => true;
    public override string DisplayName => GetText(Name + ".Name").Value;
    public override int Music => MusicLoader.GetMusicSlot(AdditionsMain.Instance, AssetRegistry.GennedSounds.Music.Protostar.SoundPath);
    public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("TheExtraordinaryAdditions/icon_menu");
    public override Asset<Texture2D> SunTexture => null;
    public override Asset<Texture2D> MoonTexture => null;

    public Trail LogoTrail;
    public TrailPoints Points;
    public static int jumpscare;

    public override void OnSelected()
    {
        
    }

    public override void Update(bool isOnTitleScreen)
    {
        
    }

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation,
        ref float logoScale, ref Color drawColor)
    {
        logoRotation *= 2f;
        logoScale = WorldGen.getGoodWorldGen ? .77f : .65f;
        drawColor = WorldGen.everythingWorldGen ? Main.DiscoColor : Color.White;

        int width = (int) (Logo.Value.Width * logoScale);
        int height = (int) (Logo.Value.Height * logoScale);

        Points ??= new(40);

        /*
        int scrWidth = Main.graphics.GraphicsDevice.Viewport.Width;
        int scrHeight = Main.graphics.GraphicsDevice.Viewport.Height;
        ManagedShader shader = AssetRegistry.GennedShaders.StarfieldShader;
        float time = TimeSystem.RenderTime;
        if (WorldGen.drunkWorldGen)
            time *= -5f;
        shader.TrySetParameter("time", time);
        shader.TrySetParameter("mouse", Main.MouseScreen);
        shader.TrySetParameter("resolution", new Vector2(scrWidth, scrHeight));

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);
        Texture2D pixel = AssetRegistry.GennedTextures.Pixel;
        shader.Render();
        spriteBatch.Draw(pixel, new Rectangle(scrWidth / 2, scrHeight / 2, scrWidth, scrHeight), null, Color.White, 0f,
            pixel.Size() / 2, 0, 0f);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);
        */

        Point pos = Main.alreadyGrabbingSunOrMoon
            ? Main.MouseScreen.ToPoint()
            : new((int) logoDrawCenter.X, (int) logoDrawCenter.Y);
        RotatedRectangle iconHitbox = new(new(pos.X - width / 2f, pos.Y - height / 2f), new(width, height),
            logoRotation, Vector2.One / 2);
        Rectangle mouseHitbox = new((int) Main.MouseScreen.X, (int) Main.MouseScreen.Y, 14, 14);

        if ((Main.mouseLeft || Main.starGame) && Main.hasFocus && iconHitbox.Intersects(mouseHitbox))
        {
            logoDrawCenter = Main.MouseScreen;
            Points.Update(Main.MouseScreen);

            ManagedShader streak = AssetRegistry.GennedShaders.SideStreakTrail;
            streak.SetTexture(AssetRegistry.GennedTextures.CausticNoise, 1, SamplerState.LinearWrap);
            LogoTrail = new(_ => width, (c, _) =>
            {
                Color col = MulticolorLerp(c.X + Main.GlobalTimeWrappedHourly, new(232, 242, 255), new(146, 192, 239),
                    new(107, 162, 229), new(94, 126, 181));
                return (WorldGen.remixWorldGen
                    ? new Color(byte.MaxValue - col.R, byte.MaxValue - col.G, byte.MaxValue - col.B)
                    : col) * MathHelper.SmoothStep(1f, 0f, c.X);
            }, null, 40);
            LogoTrail.DrawTrail(streak, Points.Points,
                Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1), 600, true);

            Main.alreadyGrabbingSunOrMoon = true;
        }
        else
        {
            logoDrawCenter += PolarVector(new Vector2(20f, 10f), TimeSystem.RenderTime);
            Points.Clear();

            Main.alreadyGrabbingSunOrMoon = false;
        }

        if (WorldGen.remixWorldGen)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, AssetRegistry.GennedShaders.NegativeOverlay.Effect,
                Main.UIScaleMatrix);
        }

        Texture2D tex = Logo.Value;
        if (Main.starGame && Main.rand.NextBool(1000) && jumpscare <= 0)
            jumpscare = 14;
        if (jumpscare > 0)
        {
            tex = AssetRegistry.GennedTextures.TheGiantSnailFromAncientTimes;
            logoScale *= 6f;
        }

        if (jumpscare > 0)
            jumpscare--;
        int logoWidth = tex.Width;
        int logoHeight = tex.Height;
        spriteBatch.Draw(tex, logoDrawCenter, new Rectangle(0, 0, logoWidth, logoHeight),
            drawColor, logoRotation, new Vector2(logoWidth / 2f, logoHeight / 2f), logoScale, SpriteEffects.None, 0f);

        if (WorldGen.remixWorldGen)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        }

        if (Main.starGame)
        {
            Texture2D texture = AssetRegistry.GennedTextures.TungstenCube;
            for (int i = 0; i < Main.numStars; i++)
            {
                Star star = Main.star[i];
                if (star == null)
                    continue;

                star.rotation += (MathF.Abs(star.fallSpeed.X) + MathF.Abs(star.fallSpeed.Y)) * .009f;
                spriteBatch.Draw(texture, star.position, null, star.falling ? Color.White : Main.DiscoColor,
                    star.rotation, texture.Size() / 2, star.scale * star.twinkle * Main.ForcedMinimumZoom, 0, 0f);
            }
        }

        return false;
    }
}
