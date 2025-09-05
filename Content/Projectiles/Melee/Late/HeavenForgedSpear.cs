using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class HeavenForgedSpear : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavenForgedSpear);
    
    public Player Owner => Main.player[Projectile.owner];

    private const int Lifetime = 360;
    public override void SetDefaults()
    {
        Projectile.width = 84;
        Projectile.height = 18;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = 2;
        Projectile.MaxUpdates = 2;
        Projectile.timeLeft = Lifetime;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Fade => ref Projectile.ai[1];
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(tip, WidthFunction, ColorFunction, null, 40);
        cache ??= new(20);
        cache.Update(Projectile.RotHitbox().Right);

        Projectile.FacingRight();

        if (Time % 2f == 0f)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f;
                Vector2 vel = -Projectile.velocity.RotatedBy(.45f * i) * Main.rand.NextFloat(.3f, .5f);
                ParticleRegistry.SpawnGlowParticle(pos, vel, Main.rand.Next(12, 20), Main.rand.NextFloat(.2f, .3f), Color.DeepSkyBlue);
            }
        }

        if (Projectile.numHits > 0)
        {
            Projectile.velocity *= .9f;
            Projectile.timeLeft = Lifetime;

            Projectile.Opacity = InverseLerp(30f, 0f, Fade);
            if (Fade > 30f)
                Projectile.Kill();
            Fade++;
        }

        Projectile.scale = GetLerpBump(0f, 20f, Lifetime, Lifetime - 5f, Time);
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {       
        Projectile.friendly = false;
    }

    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
        {
            Vector2 pos = Projectile.Center;
            for (int i = 0; i < 20; i++)
            {
                ParticleRegistry.SpawnCloudParticle(Projectile.Center, RandomVelocity(2f, 1f, 3f), Color.DeepSkyBlue, Color.DarkCyan, Main.rand.Next(60, 80), Main.rand.NextFloat(30f, 50f), Main.rand.NextFloat(.4f, .7f));
                ParticleRegistry.SpawnSparkleParticle(Projectile.Center, RandomVelocity(1f, 1f, 6f), Main.rand.Next(30, 40), Main.rand.NextFloat(.3f, .5f), Color.Cyan, Color.CornflowerBlue, 1.4f);
                ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center, RandomVelocity(1.4f, 2f, 8f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .5f), Color.Cyan, Color.DeepSkyBlue, null, 1f, 4);
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, Vector2.Zero, Main.rand.Next(24, 28), Main.rand.NextFloat(50f, 80f), Color.LightCyan);
            }
            Projectile.CreateFriendlyExplosion(Projectile.Center, Vector2.One * 120f, Projectile.damage / 2, Projectile.knockBack, 4, 3);
            SoundEngine.PlaySound(SoundID.Item125 with { MaxInstances = 40, PitchVariance = .2f }, Projectile.Center, null);
        }
    }

    private float WidthFunction(float c)
    {
        return MathHelper.SmoothStep(Projectile.height * 3f, 0f, c) * Projectile.Opacity;
    }

    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return Color.SkyBlue * Projectile.Opacity;
    }

    public TrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public readonly ITrailTip tip = new RoundedTip(12);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader prim = ShaderRegistry.PierceTrailShader;
            trail.DrawTippedTrail(prim, cache.Points, tip, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
