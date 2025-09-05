using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;

namespace TheExtraordinaryAdditions.MainMenu;

public class StarfieldMainMenu : ModMenu
{
    public override bool IsAvailable => true;
    public override string DisplayName => GetText(Name + ".Name").Value;
    public override int Music => MusicLoader.GetMusicSlot(AssetRegistry.GetMusicPath(AdditionsSound.clairdelune));
    public override Asset<Texture2D> Logo => ModContent.Request<Texture2D>("TheExtraordinaryAdditions/icon_workshop");

    public OptimizedPrimitiveTrail LogoTrail;
    public TrailPoints Points;
    public static int jumpscare;
    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
    {
        if (!AssetRegistry.HasFinishedLoadingShaders)
            return false;

        logoRotation *= 2f;
        logoScale = WorldGen.getGoodWorldGen ? .77f : .65f;
        drawColor = WorldGen.everythingWorldGen ? Main.DiscoColor : Color.White;

        int width = (int)(Logo.Value.Width * logoScale);
        int height = (int)(Logo.Value.Height * logoScale);

        Points ??= new(40);

        int scrWidth = Main.graphics.GraphicsDevice.Viewport.Width;
        int scrHeight = Main.graphics.GraphicsDevice.Viewport.Height;
        ManagedShader shader = AssetRegistry.GetShader("StarfieldShader");
        float time = TimeSystem.RenderTime;
        if (WorldGen.drunkWorldGen)
            time *= -5f;
        shader.TrySetParameter("time", time);
        shader.TrySetParameter("mouse", Main.MouseScreen);
        shader.TrySetParameter("resolution", new Vector2(scrWidth, scrHeight));

        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, null, null, null, null, Main.UIScaleMatrix);
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
        shader.Render();
        spriteBatch.Draw(pixel, new Rectangle(scrWidth / 2, scrHeight / 2, scrWidth, scrHeight), null, Color.White, 0f, pixel.Size() / 2, 0, 0f);
        spriteBatch.End();
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, Main.UIScaleMatrix);

        Point pos = Main.alreadyGrabbingSunOrMoon ? Main.MouseScreen.ToPoint() : new((int)logoDrawCenter.X, (int)logoDrawCenter.Y);
        RotatedRectangle iconHitbox = new(pos.X - width / 2, pos.Y - height / 2, width, height, logoRotation);
        Rectangle mouseHitbox = new((int)Main.MouseScreen.X, (int)Main.MouseScreen.Y, 14, 14);

        if ((Main.mouseLeft || Main.starGame) && Main.hasFocus && iconHitbox.Intersects(mouseHitbox))
        {
            logoDrawCenter = Main.MouseScreen;
            Points.Update(Main.MouseScreen);

            ManagedShader streak = ShaderRegistry.SideStreakTrail;
            streak.Matrix = Matrix.CreateOrthographicOffCenter(0, Main.screenWidth, Main.screenHeight, 0, -1, 1);
            streak.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CausticNoise), 1, SamplerState.LinearWrap);
            LogoTrail = new(c => width, (c, pos) =>
            {
                Color col = MulticolorLerp(c.X + Main.GlobalTimeWrappedHourly, new(232, 242, 255), new(146, 192, 239), new(107, 162, 229), new(94, 126, 181));
                return (WorldGen.remixWorldGen ? new Color(byte.MaxValue - col.R, byte.MaxValue - col.G, byte.MaxValue - col.B) : col) * MathHelper.SmoothStep(1f, 0f, c.X);
            }, null, 40);
            LogoTrail.DrawTrail(streak, Points.Points, 1000, true, false);

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
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, AssetRegistry.GetShader("NegativeOverlay").Effect, Main.UIScaleMatrix);
        }

        Texture2D tex = Logo.Value;
        if (Main.starGame && Main.rand.NextBool(1000) && jumpscare <= 0)
            jumpscare = 14;
        if (jumpscare > 0)
        {
            tex = AssetRegistry.GetTexture(AdditionsTexture.TheGiantSnailFromAncientTimes);
            logoScale *= 6f;
        }
        if (jumpscare > 0)
            jumpscare--;
        int logoWidth = tex.Width;
        int logoHeight = tex.Height;
        spriteBatch.Draw(tex, logoDrawCenter, new Rectangle(0, 0, logoWidth, logoHeight),
            drawColor, logoRotation, new Vector2(logoWidth / 2, logoHeight / 2), logoScale, SpriteEffects.None, 0f);

        if (WorldGen.remixWorldGen)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, Main.Rasterizer, null, Main.UIScaleMatrix);
        }
        
        if (Main.starGame)
        {
            Texture2D texture = AssetRegistry.GetTexture(AdditionsTexture.TungstenCube);
            for (int i = 0; i < Main.numStars; i++)
            {
                Star star = Main.star[i];
                if (star == null)
                    continue;
                
                star.rotation += (MathF.Abs(star.fallSpeed.X) + MathF.Abs(star.fallSpeed.Y)) * .009f;
                spriteBatch.Draw(texture, star.position, null, star.falling ? Color.White : Main.DiscoColor, star.rotation, texture.Size() / 2, star.scale * star.twinkle * Main.ForcedMinimumZoom, 0, 0f);
            }
        }

        return false;
    }
}