using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class MartianLaser : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.extraUpdates = 4;
        Projectile.timeLeft = 300;
        Projectile.penetrate = 1;
    }

    public ref float Time => ref Projectile.ai[0];
    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public override void AI()
    {
        cache ??= new(40);
        cache.Update(Projectile.Center);
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 40);

        Projectile.velocity *= .985f;
        if (Time > 30f && Utility.AllPointsEqual(cache.Points))
            Projectile.Kill();

        Projectile.scale = Projectile.Opacity = InverseLerp(0f, 10f, Time);
        Time++;
    }

    internal Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.DeepSkyBlue * Projectile.Opacity * InverseLerp(0f, .14f, Projectile.velocity.Length());
    }

    internal float WidthFunction(float c)
    {
        float expanse = GetLerpBump(.3f, 0f, 1f, .7f, c);
        return MathHelper.SmoothStep(Projectile.height, 0f, expanse) * Projectile.scale;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            ManagedShader prim = ShaderRegistry.SpecialLightningTrail;
            prim.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1);
            trail.DrawTrail(prim, cache.Points, 50);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}