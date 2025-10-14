using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class InsigniaBlaze : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 21;
        Projectile.timeLeft = 10;
        Projectile.penetrate = 1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.DamageType = Owner.GetBestClass();
    }

    public NPC Target => Main.npc[(int)Projectile.ai[0]];
    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        Vector2 pos = Projectile.Center;
        Vector2 vel = -Vector2.UnitY.RotatedByRandom(.4f) * Main.rand.NextFloat(2f, 5f);
        ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 14, 50f, Color.OrangeRed);
        ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 10, 40f, Color.Orange, 1f);
        ParticleRegistry.SpawnSparkParticle(pos, vel * 2, Main.rand.Next(12, 15), Main.rand.NextFloat(.7f, .9f), Color.Chocolate);
        ParticleRegistry.SpawnCloudParticle(pos, vel, Color.OrangeRed, Color.Gray, 50, 40f, .4f);
    }

    public override bool? CanDamage() => Projectile.numHits > 0 ? false : null;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, SecondsToFrames(2));
    }
}
