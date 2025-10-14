using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class IchorStream : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 32;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = 5;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Projectile.scale -= 0.002f;
        if (Projectile.scale <= 0f)
            Projectile.Kill();

        if (Time > 1f)
        {
            Projectile.velocity.Y += 0.075f;
            if (Main.rand.NextBool(5))
            {
                float scale = Main.rand.NextFloat(.4f, .8f);
                ParticleRegistry.SpawnBloodParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.3f, .6f),
                    Main.rand.Next(25, 40), scale, Color.Gold);
            }

            for (int i = 0; i < 3; i++)
            {
                Dust ichor = Dust.NewDustPerfect(Projectile.Center, DustID.Ichor, null, 100);
                ichor.noGravity = true;
                ichor.velocity *= .25f;
                ichor.velocity += Projectile.velocity / 2;
            }

            if (Main.rand.NextBool(8))
            {
                Dust fall = Dust.NewDustPerfect(Projectile.Center, DustID.Ichor, null, 100, default, .5f);
                fall.velocity *= .25f;
                fall.velocity += Projectile.velocity / 2;
            }
        }
        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Vanilla
        if (target.IsDestroyer() || target.type == NPCID.Probe)
            modifiers.FinalDamage *= .75f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // convenient the ichor id is 69...
        target.AddBuff(BuffID.Ichor, 600);
    }
}
