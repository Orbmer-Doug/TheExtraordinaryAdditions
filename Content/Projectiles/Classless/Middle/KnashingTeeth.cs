using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class KnashingTeeth : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    private const int Lifetime = 200;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.ignoreWater =Projectile.friendly = true;
        Projectile.tileCollide = Projectile.hostile =false;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 3;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float RotationX => ref Projectile.ai[1];
    public ref float RotationY => ref Projectile.ai[2];
    public ref float Pullback => ref Projectile.localAI[0];
    public override void AI()
    {
        Time++;

        Vector2 projCenter = Projectile.Center;
        Projectile.scale = 1.1f - Pullback;
        Projectile.width = (int)(20f * Projectile.scale);
        Projectile.height = Projectile.width;
        Projectile.position.X = projCenter.X - Projectile.width / 2;
        Projectile.position.Y = projCenter.Y - Projectile.height / 2;
        if (Pullback < 0.1)
            Pullback += 0.01f;
        else
            Pullback += 0.025f;

        if (Pullback >= 0.95f)
            Projectile.Kill();

        if (Projectile.velocity.Length() > 16f)
        {
            Projectile.velocity.Normalize();

            // Gives the snappy poke at the end
            Projectile.velocity *= 20f;
        }
        Projectile.velocity = Projectile.velocity.RotatedBy(RotationX);
        RotationX *= 1.05f;

        if (Projectile.scale < 1f)
        {
            Vector2 vel = Projectile.velocity * (1.3f - Projectile.scale) * .1f;
            StygainEnergy.Spawn(Projectile.Center, vel, Projectile.scale * 150f);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 18; i++)
        {
            if (i < 8)
            {
                ParticleRegistry.SpawnBloodStreakParticle(Projectile.Center,
                    -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.4f),
                    30, Main.rand.NextFloat(.3f, .5f), Color.DarkRed);
            }

            ParticleRegistry.SpawnBloodParticle(Projectile.Center, -Projectile.velocity.RotatedByRandom(.5f) * Main.rand.NextFloat(.7f, 2f),
                Main.rand.Next(48, 56), Main.rand.NextFloat(.5f, .9f),
                Color.Lerp(Color.Crimson, Color.Red, Main.rand.NextFloat(.2f, .9f)));

            ParticleRegistry.SpawnBloomLineParticle(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f),
                -Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.3f, 1.2f),
                Main.rand.Next(20, 35), Main.rand.NextFloat(.4f, .6f), Color.Crimson);
        }

        AdditionsSound.MimicryLand.Play(Projectile.Center, .8f, 0f, .3f, 10, Name);
    }
}
