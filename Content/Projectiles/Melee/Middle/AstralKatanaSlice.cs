using Terraria;
using Terraria.ModLoader;
using ParticleRegistry = TheExtraordinaryAdditions.Common.Particles.Particle.ParticleRegistry;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class AstralKatanaSlice : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = false;
        Projectile.timeLeft = 60;
        Projectile.MaxUpdates = 4;
        Projectile.penetrate = 4;
    }

    private static (Color, Color) GetRandomColor()
    {
        bool blue = Main.rand.NextBool();
        Color randBlue =
            AstralKatanaSweep.AstralBluePalette[Main.rand.Next(0, AstralKatanaSweep.AstralBluePalette.Length)];
        Color randOrange =
            AstralKatanaSweep.AstralOrangePalette[Main.rand.Next(0, AstralKatanaSweep.AstralOrangePalette.Length)];
        return blue ? (randBlue, randOrange) : (randOrange, randBlue);
    }
    public override void AI()
    {
        (Color, Color) pixelCol = GetRandomColor();
        ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center + Main.rand.NextVector2Circular(10, 10),
            -Projectile.velocity * Main.rand.NextFloat(.2f, .5f),
            Main.rand.Next(50, 58), Main.rand.NextFloat(.8f, 1.9f), pixelCol.Item1, pixelCol.Item2,
            4, false, false, Main.rand.NextFloat(-.1f, .1f));
        
        if (Main.rand.NextBool(2))
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, 50, 3.5f, 30f, .3f);

        (Color, Color) cloudCol = GetRandomColor();
        ParticleRegistry.SpawnCloudParticle(Projectile.Center + Main.rand.NextVector2Circular(20, 20), 
            Main.rand.NextVector2Circular(.3f, .3f), cloudCol.Item1,  cloudCol.Item2, 
            Main.rand.Next(20, 35),Main.rand.NextFloat(20f, 35f), Main.rand.NextFloat(.65f, 1f), 2);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        (Color, Color) sparkCol = GetRandomColor();
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center,
                -Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.2f, .4f) +
                Main.rand.NextVector2Circular(4f, 4f),
                Main.rand.Next(25, 36), Main.rand.NextFloat(.4f, .7f),
                sparkCol.Item1, Main.rand.NextFloat(.7f, 1.1f), 1.1f);
        }
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 16; i++)
        {
            ParticleRegistry.SpawnGlowParticle(Projectile.Center,
                Main.rand.NextVector2Circular(2f, 2f),
                Main.rand.Next(30, 46), Main.rand.NextFloat(28f, 42f), GetRandomColor().Item1, .8f);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}