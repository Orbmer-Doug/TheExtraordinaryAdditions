using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class CryogenicBlast : ModProjectile
{
    public static readonly float Lifetime = SecondsToFrames(.4f);

    public ref float Radius => ref Projectile.ai[1];

    public static Color DetermineExplosionColor()
    {
        Color c = Color.Lerp(Color.DarkBlue, Color.DarkSlateBlue, 0.24f);
        c = Color.Lerp(c, Color.DarkSlateGray, Main.rand.NextFloat(.4f, .9f));
        return c with { A = 80 };
    }

    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = DistanceToTiles(120);
    }
    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = (int)Lifetime;
        Projectile.scale = 0.001f;

        Projectile.hostile = false;
        Projectile.friendly = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 16;

        Projectile.DamageType = DamageClass.Default;
    }

    public override void AI()
    {
        // Cause the wave to expand outward, along with its hitbox.
        Radius = MathHelper.Lerp(Radius, 400f, 0.39f);
        Projectile.scale = MathHelper.Lerp(.8f, 1f, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Projectile.Opacity = InverseLerp(2f, 15f, Projectile.timeLeft);

        Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);
        Vector2 vel = RandomVelocity(2f, 5f, 14f);
        ParticleRegistry.SpawnDustParticle(pos, vel, 35, Main.rand.NextFloat(.9f, 1f), Color.Cyan, 1.1f);
        ParticleRegistry.SpawnSparkleParticle(pos, vel, 19, 1f, Color.White, Color.LightCyan, 1.2f, .2f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius * 0.5f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise);
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
