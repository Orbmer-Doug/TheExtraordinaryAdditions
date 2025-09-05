using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class AbyssalCurrentsHoldout : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbyssalCurrent);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();


    /*
    public void DisturbWater()
    {
        if (Main.netMode != NetmodeID.Server)
        {
            WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
            float waveSine = 0.1f * MathF.Sin(Main.GlobalTimeWrappedHourly * 20f);
            Vector2 ripplePos = Projectile.Center + Projectile.velocity * 7f;
            Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
            ripple.QueueRipple(ripplePos, waveData, Vector2.One * 1360f, RippleShape.Square, Projectile.rotation);
        }
    }
    public void DrawGlow()
    {
        SpriteBatch sb = Main.spriteBatch;
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise);

        ManagedShader shine = AssetRegistry.GetShader("RadialShineShader");
        shine.TrySetParameter("glowPower", .2f);
        shine.TrySetParameter("glowColor", Abysslon.WaterPalette[0].ToVector4());
        shine.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 5f);

        sb.EnterShaderRegionAlt();
        shine.Render();

        float fade = Animators.MakePoly(2f).OutFunction(InverseLerp(0f, 24f, GlowCounter));
        sb.Draw(tex, ToTarget(Tip, new(400f * fade)), null, Abysslon.BrackishPalette[4] * 0.4f * fade, Projectile.rotation, tex.Size() * 0.5f, 0, 0f);

        sb.ExitShaderRegion();
    }
    public void DrawTelegraph()
    {
        Texture2D texture = AssetRegistry.InvisTex;
        float fade = 1f - InverseLerp(ThrowReelTime * .85f, ThrowReelTime, ThrowTimer);
        float completion = InverseLerp(0f, ThrowReelTime, ThrowTimer) * fade;
        float size = Utils.Remap(ThrowTimer, 0f, ThrowReelTime * .5f, 0f, 2200f) * fade;

        ManagedShader scope = ShaderRegistry.PixelatedSightLine;
        scope.TrySetParameter("noiseOffset", Main.GameUpdateCount * -0.003f);
        scope.TrySetParameter("mainOpacity", 1f);
        scope.TrySetParameter("resolution", new Vector2(size / 2));
        scope.TrySetParameter("rotation", -Projectile.rotation - DefaultRot);
        scope.TrySetParameter("width", 0.0025f * completion);
        scope.TrySetParameter("lightStrength", 3f);
        scope.TrySetParameter("color", Abysslon.BrackishPalette[2].ToVector3());
        scope.TrySetParameter("darkerColor", Abysslon.BrackishPalette[0].ToVector3());
        scope.TrySetParameter("bloomSize", 0.29f * completion);
        scope.TrySetParameter("bloomMaxOpacity", 0.4f);
        scope.TrySetParameter("bloomFadeStrength", 7f);

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, scope.Effect);

        Main.EntitySpriteDraw(texture, Projectile.RotHitbox().TopRight - Main.screenPosition, null, Color.White, 0f, texture.Size() * .5f, size, 0, 0f);

        Main.spriteBatch.ExitShaderRegion();
    }



    POOL


                bool away = Vector2.Dot(Projectile.Center.SafeDirectionTo(player.Center), Projectile.velocity) < 0f;
            Vector2 force = Projectile.Center.SafeDirectionTo(player.Center) * (MathF.Min(Projectile.Distance(player.Center), 15f) * (away ? 1.8f : 1f));
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, force, .1f);
            Projectile.velocity += force * .001f;

            // Beeline to the target if they are too far away
            if (!Projectile.WithinRange(player.Center, screenWidth * 2f))
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.Center.SafeDirectionTo(player.Center) * 22f, 0.50f);
            }

        public override bool PreDraw(ref Color lightColor)
    {
        ManagedScreenShader swirl = AssetRegistry.GetFilter("WhirlpoolSwirl");
        swirl.TrySetParameter("screenSize", new Vector2(screenWidth, screenHeight));
        swirl.TrySetParameter("distortionRadius", Projectile.Size.X * .3f * Projectile.Opacity);
        swirl.TrySetParameter("distortionIntensity", .7f * Projectile.Opacity);
        swirl.TrySetParameter("blackSize", .6f * Projectile.Opacity);
        swirl.TrySetParameter("distortionPosition", Vector2.Transform(Projectile.Center - screenPosition, GameViewMatrix.TransformationMatrix));
        swirl.TrySetParameter("zoom", GameViewMatrix.Zoom.X);
        swirl.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.BigWavyBlobNoise), 1, SamplerState.LinearClamp);
        swirl.Activate();

        SpriteBatch sb = spriteBatch;
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.CausticNoise);

        ManagedShader shader = AssetRegistry.GetShader("WhirlpoolShader");
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Rasterizer, shader.Effect, GameViewMatrix.TransformationMatrix);
        shader.Render();
        sb.Draw(noise, ToTarget(Projectile.Center, Projectile.Size * Projectile.Opacity), null, Color.Cyan * Projectile.Opacity, 0f, noise.Size() / 2f, 0, 0f);
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, DefaultSamplerState, DepthStencilState.None, Rasterizer, null, GameViewMatrix.TransformationMatrix);

        return false;
    }

    public override bool PreKill(int timeLeft)
    {
        AssetRegistry.GetFilter("WhirlpoolSwirl").Deactivate();
        return true;
    }

    */

}