using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class IchorSwirl : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(32);
        Projectile.hostile = false;
        Projectile.friendly = Projectile.tileCollide = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.extraUpdates = 2;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.localNPCHitCooldown = 10;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Projectile.scale -= 0.002f;
        if (Projectile.scale <= 0f)
            Projectile.Kill();

        if (Time > 3f)
        {
            Projectile.velocity.Y += 0.075f;
            if (Main.rand.NextBool(5))
            {
                float scale = Main.rand.NextFloat(.4f, .8f);
                ParticleRegistry.SpawnBloodParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.3f, .6f), Main.rand.Next(25, 40), scale, Color.Gold);
            }

            int offset = 16;
            Vector2 pos = new(Projectile.position.X + offset, Projectile.position.Y + offset);
            ref float Offset = ref Projectile.ai[2];
            Offset += .09f * (Projectile.identity % 2f == 1f).ToDirectionInt() % MathHelper.TwoPi;
            int arms = 6;
            for (int i = 0; i < arms; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / arms + Offset).ToRotationVector2().RotatedBy(20) * 12f;
                Dust swirl = Dust.NewDustPerfect(pos, DustID.Ichor, vel, 100, default, 1.1f);
                swirl.velocity *= 0.25f;
                swirl.velocity += Projectile.velocity / 2;
                swirl.noGravity = true;
            }
        }
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // convenient the ichor id is 69...
        target.AddBuff(BuffID.Ichor, 600);
        Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 72f, Vector2.Zero, 15, Color.Gold, null, Color.Goldenrod);
        for (int i = 0; i < 10; i++)
            ParticleRegistry.SpawnMistParticle(Projectile.RandAreaInEntity(), RandomVelocity(4f, 2f, 10f), Main.rand.NextFloat(.2f, .4f), Color.Gold, Color.DarkGoldenrod, Main.rand.NextByte(130, 190));

        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<IchorStreamBlast>(), (int)(Projectile.damage * .75f), 0f, Projectile.owner);
    }
}
