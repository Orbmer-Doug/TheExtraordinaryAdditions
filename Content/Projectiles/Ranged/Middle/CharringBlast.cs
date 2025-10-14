using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class CharringBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = true;
        Projectile.extraUpdates = 1;
    }

    public ref float Time => ref Projectile.ai[0];

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, 100);
    }

    public override void AI()
    {
        float interpol = Projectile.scale = Projectile.Opacity = GetLerpBump(0f, 5f, 300f, 290f, Time);
        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * interpol);

        Color col = Color.Lerp(Color.Red, Color.Lerp(Color.OrangeRed, Color.Chocolate, Main.rand.NextFloat(.4f, .7f)), Projectile.scale);
        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = Projectile.Center;
            Vector2 vel = Projectile.velocity.RotatedByRandom(.1f) * Main.rand.NextFloat(0.2f, 0.4f);

            float size = Main.rand.NextFloat(.25f, .3f) * interpol;
            int life = Main.rand.Next(20, 30);
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, size, col, Main.rand.NextFloat(.5f, 1f), true);

            if (Main.rand.NextBool(12))
                Dust.NewDustPerfect(pos, DustID.Smoke, vel * .5f + Vector2.UnitY * -Main.rand.NextFloat(1f, 2f));
        }

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item20 with { Volume = Main.rand.NextFloat(.8f, .9f), Pitch = -.1f, PitchVariance = .05f, MaxInstances = 10 }, Projectile.Center);
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = Main.rand.NextFloat(1.1f, 1.25f), Pitch = .15f, PitchVariance = .1f, MaxInstances = 10 }, Projectile.Center);
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CharringBlastBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f, 0f);
    }

    public override bool PreDraw(ref Color lightColor) => false;
}
