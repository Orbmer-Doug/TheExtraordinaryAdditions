using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class MicroRound : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = 35;
        Projectile.height = 35;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 2;
        Projectile.timeLeft = 600;
        Projectile.extraUpdates = 5;
        Projectile.tileCollide = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
    }
    public Player Owner => Main.player[Projectile.owner];
    public ref float Timer => ref Projectile.ai[0];
    public override void AI()
    {
        if (Projectile.velocity.Length() < 32f)
            Projectile.velocity *= 1.00095f;

        Timer++;

        Color color = Color.Lerp(Color.Chocolate, Color.Red, .3f);
        Lighting.AddLight(Projectile.Center, color.ToVector3());
        if (Projectile.timeLeft < 596 && Projectile.timeLeft % 2 == 1)
        {
            int positionVariation = (Projectile.timeLeft < 565) ? 25 : ((Projectile.timeLeft < 585) ? 12 : 5);
            Vector2 pos = Projectile.Center - Projectile.velocity * 0.75f + Utils.NextVector2Circular(Main.rand, positionVariation, positionVariation);
            Vector2 vel = -Projectile.velocity * Utils.NextFloat(Main.rand, 0.003f, 0.001f);
            ParticleRegistry.SpawnBloomLineParticle(pos, vel, 4, 1.45f, color);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.ResetLocalNPCHitImmunity();

        if (Projectile.numHits <= 0)
        {
            ParticleRegistry.SpawnSparkleParticle(Projectile.Center + Utils.RotatedByRandom(Projectile.velocity, 0.3),
                -Projectile.velocity / 3, Main.rand.Next(11, 18), Main.rand.NextFloat(.65f, 1.5f), Color.White, Color.Chocolate, 2.5f, Main.rand.NextFloat(-.04f, .04f));

            int life = Main.rand.Next(45, 60);
            float scale = Utils.NextFloat(Main.rand, 1.2f, Utils.NextFloat(Main.rand, .3f, 1.3f)) * 0.75f;
            Color col = Color.Lerp(Color.OrangeRed, Color.Orange * 1.2f, Utils.NextFloat(Main.rand, 0.7f));
            col = Color.Lerp(col, Color.Chocolate, Utils.NextFloat(Main.rand));
            Vector2 vel = Utils.RotatedByRandom(-Projectile.velocity, 0.699) * Utils.NextFloat(Main.rand, .5f, 1.2f);
            Vector2 pos = Projectile.Center + Utils.RotatedByRandom(Projectile.velocity, 0.3);
            ParticleRegistry.SpawnSparkParticle(pos, vel, life, scale, col);

            for (int i = 0; i <= 7; i++)
            {
                Dust obj = Dust.NewDustPerfect(Projectile.Center + Projectile.velocity * 1.5f + Utils.NextVector2Circular(Main.rand, 9f, 9f), Utils.NextBool(Main.rand, 3) ? 303 : 244, (Vector2?)(Utils.RotatedByRandom(-Projectile.velocity * Utils.NextFloat(Main.rand, 0.5f, 3f), (double)MathHelper.ToRadians(20f)) * Utils.NextFloat(Main.rand, 0.1f, 0.8f)), 0, default(Color), 1.5f);
                obj.noGravity = true;
                obj.scale = ((obj.type == 244) ? Utils.NextFloat(Main.rand, 1.8f, 2.5f) : Utils.NextFloat(Main.rand, 1.4f, 1.8f));
                obj.fadeIn = ((obj.type == 244) ? 1.2f : 0f);
            }
        }
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f + Main.rand.NextVector2CircularLimited(5f, 5f, .7f, 1f);
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.09f) * Main.rand.NextFloat(.05f, .1f);
            ParticleRegistry.SpawnGlowParticle(pos, vel, Main.rand.Next(10, 20), Main.rand.NextFloat(.4f, .8f), Color.Lerp(Color.DarkRed, Color.White, .3f));

            if (Main.rand.NextBool())
            {
                ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(10f, 20f),
                    Main.rand.Next(70, 100), Main.rand.NextFloat(.5f, .9f), Color.OrangeRed, true);
            }
        }
        return true;
    }
}
