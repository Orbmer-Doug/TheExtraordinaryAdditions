using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class CalciumShot : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 200;
        Projectile.penetrate = 2;
        Projectile.MaxUpdates = 2;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public TrailPoints cache;
    public override void AI()
    {
        if (Projectile.velocity != Vector2.Zero)
            Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * Projectile.Opacity);

        cache ??= new(5);
        cache.Update(Projectile.Center);

        if (Projectile.ai[0]++ >= 10 && cache.Points.AllPointsEqual())
            Projectile.Kill();
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = Vector2.Zero;
        Projectile.friendly = false;
        return false;
    }

    private float WidthFunction(float c)
    {
        return Projectile.width * MathHelper.SmoothStep(2f, 0f, c);
    }

    private static Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.OrangeRed * GetLerpBump(0f, .1f, .8f, .27f, c.X);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage /= 2;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 1);
            OptimizedPrimitiveTrail trail = new(WidthFunction, ColorFunction, null, 5);
            trail.DrawTrail(shader, cache.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}