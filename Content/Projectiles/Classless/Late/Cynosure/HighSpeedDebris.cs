using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class HighSpeedDebris : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TungstenCube);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 90;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = Projectile.tileCollide = true;
        Projectile.MaxUpdates = 30;
        Projectile.timeLeft = Projectile.MaxUpdates * 180;
        Projectile.penetrate = 1;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        if (Time == 0f)
            AdditionsSound.IkeMaster4.Play(Projectile.Center, .6f, 1f, .2f);

        // Make the plasma trail as if its colliding with air
        ParticleRegistry.SpawnGlowParticle(Projectile.Center, Vector2.Zero, 90, Main.rand.NextFloat(90f, 135f), Color.OrangeRed, 2f);
        //MetaballRegistry.SpawnPlasmaMetaball(Projectile.Center, Vector2.Zero, 90, Main.rand.Next(120, 130), .7f);
        Time++;
    }

    public const int ExplosionRadius = 500;
    public void Explode()
    {
        // Create the initial blast
        ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 20, 0f, Vector2.One, 0f, ExplosionRadius * 1.2f, Color.OrangeRed);
        for (int i = 0; i < 110; i++)
        {
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, Main.rand.NextVector2Circular(14f, 14f),
                Main.rand.Next(40, 60), Main.rand.NextFloat(50f, 140f), Color.OrangeRed, Main.rand.NextFloat(.6f, 1.2f));
            ParticleRegistry.SpawnCloudParticle(Projectile.Center, Main.rand.NextVector2Circular(14f, 14f), Color.OrangeRed, Color.DarkGray,
                Main.rand.Next(50, 90), Main.rand.NextFloat(80f, 200f), Main.rand.NextFloat(.6f, 1f));
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(30f, 30f, .5f, 1f),
                Main.rand.Next(32, 35), Main.rand.NextFloat(.6f, 1.5f), Color.OrangeRed, Color.White, null, 1f, 7, false, true);
            ParticleRegistry.SpawnBloomLineParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(50f, 50f, .7f, 1f), Main.rand.Next(10, 12), Main.rand.NextFloat(.7f, 1.6f), Color.OrangeRed);
        }
        Projectile.CreateFriendlyExplosion(Projectile.Center, Vector2.One * ExplosionRadius, Projectile.damage, Projectile.knockBack, 10, 9);
        
        // Create a 'shockwave'
        for (int i = 0; i < 30; i++)
        {
            float completion = InverseLerp(0f, 30, i);
            Vector2 norm = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 dir = new(-norm.Y, norm.X);
            Vector2 vel = dir * MathHelper.Lerp(-60f, 60f, completion);
            if (vel == Vector2.Zero)
                vel = dir * 2f;

            int life = (int)MathHelper.Lerp(30, 50, Convert01To010(completion));
            float scale = (int)MathHelper.Lerp(.5f, 2f, Convert01To010(completion));
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, life, scale * 182f, Color.OrangeRed);
        }

        // Sounds and effects
        ParticleRegistry.SpawnBlurParticle(Projectile.Center, 60, .7f, ExplosionRadius * 1.4f);
        ParticleRegistry.SpawnFlash(Projectile.Center, 40, 1.9f, ExplosionRadius, .4f);
        ParticleRegistry.SpawnShockwaveParticle(Projectile.Center, 20, .2f, ExplosionRadius * 3f, 20f, .3f);
        
        AdditionsSound.IkeSpecial5.Play(Projectile.Center, .8f, 0f, .3f, 100, Name);
        AdditionsSound.BraveSpecial1C.Play(Projectile.Center, 1.2f, -.2f);
        Projectile.Kill();
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info) => Explode();
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Explode();
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Explode();
        return true;
    }

    public override bool PreDraw(ref Color lightColor) => false;
}