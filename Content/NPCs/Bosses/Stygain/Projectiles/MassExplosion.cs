using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class MassExplosion : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.timeLeft = 30;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.width = Projectile.height = 20;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Projectile.damage = DifficultyBasedValue(1500, 2000, 3000, 4000, 5000, 1500);
        if (Time == 0f)
        {
            Vector2 pos = Projectile.Center;
            for (int i = 0; i < 120; i++)
            {
                ParticleRegistry.SpawnBloomPixelParticle(pos, Main.rand.NextVector2CircularLimited(30f, 30f, .6f, 1f), Main.rand.Next(32, 34), Main.rand.NextFloat(.6f, 1.2f), Color.Crimson, Color.DarkRed, null, 1.4f, 10, false, true);
                ParticleRegistry.SpawnGlowParticle(pos, Main.rand.NextVector2Circular(18f, 18f), Main.rand.Next(40, 70), Main.rand.NextFloat(70f, 120f), Color.Crimson * 1.4f, .7f);
                ParticleRegistry.SpawnCloudParticle(pos, Main.rand.NextVector2Circular(4f, 4f), Color.Crimson, Color.DarkRed, Main.rand.Next(40, 60), Main.rand.NextFloat(40f, 100f), Main.rand.NextFloat(.6f, .8f), 1);
            }

            for (int i = 0; i < 6; i++)
            {
                float scale = Utils.Remap(i, 0, 6, 300f, 900f);
                int life = (int)Utils.Remap(i, 0, 6, 20, 40);
                Color col = MulticolorLerp(Main.rand.NextFloat(), Color.Crimson, Color.Crimson * 1.4f, Color.DarkRed, Color.DarkRed * 1.8f);
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Vector2.One * scale, Vector2.Zero, life, col, RandomRotation());
            }
        }

        Projectile.scale = Utils.Remap(Time, 0f, 30f, .1f, 900f);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.scale, targetHitbox);
    }
}
