using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class RagingCursedFire : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults() { }
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.alpha = 100;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 2;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.timeLeft = SecondsToFrames(6);
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        // Vanilla AI
        Time++;
        if (Time > 20)
            Projectile.velocity.Y += .2f;
        if (Projectile.velocity.Y > 16f)
            Projectile.velocity.Y = 16f;

        Lighting.AddLight(Projectile.Center, .35f, 1f, 0f);

        // Do all the fancy fire effects
        for (int i = 0; i < 2; i++)
        {
            Vector2 pos = Projectile.RandAreaInEntity();
            Vector2 vel = Projectile.velocity * .2f;
            float size = Main.rand.NextFloat(.3f, .6f) * Projectile.scale;
            int time = Main.rand.Next(18, 24);
            float opacity = Main.rand.NextFloat(.7f, 1.1f);
            Color color = new(179, 252, 0);
            Color color2 = new(96, 248, 2);

            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, time, size, color, opacity, true);
            if (Main.rand.NextBool(5))
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, time, size, color2, opacity, true);
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        // Bounce off of tiles
        if (Projectile.velocity.X != oldVelocity.X)
            Projectile.velocity.X = -oldVelocity.X;
        if (Projectile.velocity.Y != oldVelocity.Y)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.CursedInferno, 420);
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
    }
}
