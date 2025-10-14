using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Multi.Middle;

public class SkeleShot : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 200;
        Projectile.MaxUpdates = 4;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints cache;
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 5);

        if (Projectile.velocity != Vector2.Zero)
            Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * Projectile.Opacity);

        cache ??= new(15);
        cache.Update(Projectile.Center);
        Projectile.velocity *= .985f;
        Projectile.Opacity = InverseLerp(0f, 4f, Projectile.velocity.Length());

        if (Projectile.ai[0]++ >= 10 && cache.Points.AllPointsEqual())
            Projectile.Kill();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Vector2.Zero, 8, Main.rand.NextFloat(1.1f, 1.4f), Color.White, Color.Chocolate);
        AdditionsSound.AuroraTink1.Play(Projectile.Center, .5f, .4f, .1f, 20, Name);
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

    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.OrangeRed * GetLerpBump(0f, .1f, .8f, .27f, c.X) * Projectile.Opacity;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.FlameTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Pixel), 1);
            trail.DrawTrail(shader, cache.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}