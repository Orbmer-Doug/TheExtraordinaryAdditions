
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class AbsorbantLight : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public override void SetStaticDefaults()
    {

    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.alpha = 255;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 120;
        Projectile.tileCollide = false;
        Projectile.scale = 1f;
    }

    public override void SafeAI()
    {
        if (Asterlin.Myself is null) return;
        for (int i = 0; i <= 2; i++)
        {
            Vector2 pos = Projectile.Center;
            Vector2 vel = Projectile.velocity * Main.rand.NextFloat(.4f, 1f);
            int lifetime = Main.rand.Next(20, 50);
            float size = Main.rand.NextFloat(.5f, 1.1f);
            Color col = Color.Gold;
            Color col2 = Color.LightGoldenrodYellow;

            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, lifetime, size, col);
            if (Time % 2f == 0)
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel.RotatedByRandom(.1f), lifetime, size, col2, .3f);
        }

        if (Time > 45)
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Asterlin.Myself.Center) * 7f, .09f);
        }

        if (Projectile.Hitbox.Intersects(Asterlin.Myself.Hitbox))
        {
            int ringCount = 3;
            int lightPerRing = 8;

            for (int i = 0; i < ringCount; i++)
            {
                float lightSpeed = MathHelper.Lerp(10f, 5f, i / (float)(ringCount - 1f));
                for (int j = 0; j < lightPerRing; j++)
                {
                    Vector2 vel = (MathHelper.TwoPi * j / lightPerRing).ToRotationVector2() * lightSpeed;
                    if (i % 2 == 0)
                        vel = vel.RotatedBy(MathHelper.Pi / lightPerRing);

                    ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel, 40, Main.rand.NextFloat(.2f, .5f), Color.Gold);
                }
                lightPerRing += 4;
            }

            SoundStyle breaks = SoundID.DD2_WitherBeastHurt;
            breaks.MaxInstances = 0;
            breaks.Pitch = .7f;
            breaks.PitchVariance = 0.12f;
            breaks.Volume = .65f;
            SoundEngine.PlaySound(breaks, (Vector2?)Projectile.Center, null);

            Projectile.Kill();
        }

        Time++;
    }
}
