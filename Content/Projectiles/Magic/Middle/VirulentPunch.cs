using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class VirulentPunch : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 140;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 10;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.985);
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        if (Time == 0f)
        {
            for (float i = .8f; i <= 1f; i += .1f)
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Projectile.Size * i, Vector2.Zero, 40, Color.LawnGreen);

            for (int i = 0; i < 12; i++)
            {
                ParticleRegistry.SpawnMistParticle(Projectile.Center, Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextFloat(0.85f, 1.3f), Color.LimeGreen, Color.DarkGreen, Main.rand.NextFloat(120f, 190f));

                for (int j = 0; j < 4; j++)
                    ParticleRegistry.SpawnDustParticle(Projectile.Center, Main.rand.NextVector2Circular(30f, 30f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .9f), Color.LawnGreen, Main.rand.NextFloat(-.1f, .1f), false, true, true, false);
            }
        }
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.Size.Length() * .5f, targetHitbox);
    }
}