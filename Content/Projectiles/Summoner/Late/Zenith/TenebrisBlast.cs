using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;

public class TenebrisBlast : ModProjectile
{
    public static readonly int Lifetime = SecondsToFrames(.5f);

    public ref float Radius => ref Projectile.ai[0];

    public static Color DetermineExplosionColor()
    {
        Color c = Color.Lerp(Color.Violet, Color.DarkViolet, 0.24f);
        c = Color.Lerp(c, Color.BlueViolet, Main.rand.NextFloat(.4f, .9f));
        return c with { A = 80 };
    }

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1111;
    }
    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.scale = 0.001f;

        Projectile.hostile = false;
        Projectile.friendly = true;
    }

    public override void AI()
    {
        float interpol = InverseLerp(Lifetime, 0f, Projectile.timeLeft);
        Radius = Animators.Circ.OutFunction.Evaluate(0f, 400f, interpol);
        Projectile.scale = MathHelper.Lerp(.2f, 1.2f, interpol);
        Projectile.Opacity = GetLerpBump(2f, 15f, Lifetime - 12f, Lifetime, Projectile.timeLeft);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius * 0.4f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.ManifoldNoise);
        DrawData explosionDrawData = new(tex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

        ManagedShader shockwaveShader = ShaderRegistry.ShockwaveShader;
        shockwaveShader.TrySetParameter("mainColor", DetermineExplosionColor().ToVector3());
        shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
        shockwaveShader.TrySetParameter("projPosition", Projectile.Center - Main.screenPosition);
        shockwaveShader.TrySetParameter("shockwaveOpacity", Projectile.Opacity * .6f);
        shockwaveShader.Render();
        explosionDrawData.Draw(Main.spriteBatch);

        Main.spriteBatch.ResetToDefault();
        return false;
    }
}
