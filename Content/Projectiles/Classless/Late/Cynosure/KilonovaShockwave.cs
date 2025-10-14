using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class KilonovaShockwave : ModProjectile
{
    public static readonly float Lifetime = SecondsToFrames(1.9f);
    public ref float Radius => ref Projectile.ai[1];
    public static Color DetermineExplosionColor()
    {
        Color c = Color.Lerp(Color.SkyBlue, Color.LightBlue * 1.2f, 0.24f);
        c = Color.Lerp(c, Color.SkyBlue, .8f);
        return c with { A = 250 };
    }

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 24000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = (int)Lifetime;
        Projectile.scale = 0.001f;

        Projectile.hostile = false;
        Projectile.friendly = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 16;

        Projectile.DamageType = DamageClass.Ranged;
    }

    public const int MaxRadius = 9200;

    public override void AI()
    {
        Radius = Animators.Circ.OutFunction(1f - InverseLerp(0f, Lifetime, Projectile.timeLeft)) * MaxRadius;
        Projectile.scale = MathHelper.Lerp(.6f, 2.4f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Projectile.Opacity = InverseLerp(0f, 15f, Projectile.timeLeft);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius * 0.5f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.FlameMap2);
        DrawData explosionDrawData = new(tex, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White * Projectile.Opacity);

        ManagedShader shockwaveShader = ShaderRegistry.LightShockwave;
        shockwaveShader.TrySetParameter("mainColor", DetermineExplosionColor().ToVector3());
        shockwaveShader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        shockwaveShader.TrySetParameter("explosionDistance", Radius * Projectile.scale * 0.5f);
        shockwaveShader.TrySetParameter("projPosition", Projectile.Center - Main.screenPosition);
        shockwaveShader.TrySetParameter("shockwaveOpacity", Projectile.Opacity * .4f);
        shockwaveShader.Render();
        explosionDrawData.Draw(Main.spriteBatch);

        Main.spriteBatch.ResetToDefault();
        return false;
    }
}
