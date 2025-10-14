using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class InfernalFork : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.width =
        Projectile.height = 12;
        Projectile.alpha = 255;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.timeLeft = SecondsToFrames(3);
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.penetrate = 1;
        Projectile.extraUpdates = 1;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        for (int i = 0; i < 3; i++)
        {
            float scale = InverseLerp(0f, 10f * Projectile.MaxUpdates, Time) * Main.rand.NextFloat(.3f, .6f);
            Vector2 vel = Projectile.velocity * Main.rand.NextFloat(.1f, .2f);
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.Orange, Color.OrangeRed * 1.6f);
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel, 50, scale, color, .7f, true);
        }
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, 60 * Main.rand.Next(8, 16));
    }

    public override void OnKill(int timeLeft)
    {
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<InfernalBlast>(), (int)(Projectile.damage * .75f), Projectile.knockBack, Projectile.owner);
    }
}
