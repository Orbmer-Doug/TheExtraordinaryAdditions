using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class BreakerStorm : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.height = Projectile.width = 120;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 50;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.penetrate = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        if (Time % 10 == 9)
            AdditionsSound.BreakerStorm.Play(Projectile.Center, 1.1f, 0f, .1f, 20);

        if (Time % 2 == 1)
        {
            Vector2 size = new Vector2(Main.rand.NextFloat(.2f, .5f) * 230f);
            Projectile.CreateFriendlyExplosion(Projectile.Center, size, Projectile.damage, Projectile.knockBack, 5, 4);
            ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, size,
                Main.rand.NextVector2CircularLimited(3f, 3f, .5f, 1f), Main.rand.Next(28, 35), Color.LightBlue);
        }

        ParticleRegistry.SpawnSparkParticle(Projectile.Center, Main.rand.NextVector2Circular(10f, 10f), Main.rand.Next(40, 60), Main.rand.NextFloat(.5f, .9f), Color.LightCyan);
        ParticleRegistry.SpawnGlowParticle(Projectile.Center, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(30, 40), Main.rand.NextFloat(.6f, 1f), Color.Cyan.Lerp(Color.White, .5f));

        Time++;
    }
}
