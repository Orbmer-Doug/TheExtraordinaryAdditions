using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class AcceleratingLight : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public bool HitTarget
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float MaxSpeed => ref Projectile.ai[2];

    public const int Life = 200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.penetrate = 3;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.tileCollide = Projectile.ignoreWater = false;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;

        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public TrailPoints cache;
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(c => Projectile.height / 2 * GetLerpBump(0f, .2f, 1f, .2f, c), (c, pos) => Color.Gold, null, 15);

        float lifeRatio = GetLerpBump(0f, 20f, Life, Life - 20f, Time) * InverseLerp(0f, 30f, Projectile.timeLeft);
        Projectile.Opacity = lifeRatio;
        Projectile.scale = lifeRatio * .1f;

        Projectile.rotation = Projectile.velocity.ToRotation();
        Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * Projectile.scale);

        if (Projectile.numHits >= 3)
        {
            if (Projectile.timeLeft > 30)
                Projectile.timeLeft = 30;

            Projectile.velocity *= .9f;
        }
        else if (Projectile.velocity.Length() < MaxSpeed)
            Projectile.velocity *= 1.1f;

        cache ??= new(15);
        cache.Update(Projectile.Center);

        Time++;
    }

    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || cache == null)
                return;
            ManagedShader shader = ShaderRegistry.FadedStreak;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1);
            trail.DrawTrail(shader, cache.Points, 60);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
