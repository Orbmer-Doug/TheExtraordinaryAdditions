using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class GreekBombArrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GreekBombArrow);
    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 42;
        Projectile.aiStyle = 0;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 200;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.CursedInferno, SecondsToFrames(4));
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Projectile.FacingDown();

        if (Time > 15f)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .15f, -20f, 20f);

        Projectile.velocity *= .99f;

        Lighting.AddLight(Projectile.RotHitbox().Bottom, Color.LawnGreen.ToVector3() * .7f);
        ParticleRegistry.SpawnSparkParticle(Projectile.RotHitbox().Bottom, Projectile.velocity * .5f, 40, 1f, Color.Lime);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Projectile.RotatingHitboxCollision(targetHitbox.TopLeft(), targetHitbox.Size());
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item14 with { Pitch = .2f, PitchVariance = .1f, Volume = Main.rand.NextFloat(.8f, 1.1f), Identifier = Name }, Projectile.Center);

        //Particle.Spawn(new TileHeatParticle(Projectile.Center, 2f, 3, Color.Lime, Color.DarkGreen, Color.DarkOliveGreen));
        for (int i = 0; i < Main.rand.Next(3, 5); i++)
            Projectile.NewProj(Projectile.Center, -Projectile.oldVelocity.RotatedByRandom(1.8f) * Main.rand.NextFloat(.2f, .4f), ModContent.ProjectileType<GreekNapalm>(), Projectile.damage / 2, 0f, Projectile.owner);

        for (int i = 0; i < 30; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * Main.rand.Next(0, 11) / 10f + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(4f, 9f);

            ParticleRegistry.SpawnGlowParticle(Projectile.Center, shootVelocity, Main.rand.Next(18, 25), Main.rand.NextFloat(.4f, .6f), Color.LawnGreen, 1f);

            ParticleRegistry.SpawnSparkParticle(Projectile.Center, shootVelocity, Main.rand.Next(28, 34), .6f, Color.Lime);
        }
    }
}