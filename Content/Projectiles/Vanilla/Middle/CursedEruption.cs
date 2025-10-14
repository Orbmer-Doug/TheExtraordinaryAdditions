using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;
namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class CursedEruption : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults() { }
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 3;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.MaxUpdates = 3;
        Projectile.timeLeft = 50;
        Projectile.idStaticNPCHitCooldown = 7;
        Projectile.usesIDStaticNPCImmunity = true;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        Time++;
        Projectile.scale = .5f * Utils.Remap(Time, 6f, 36f, 1f, 3f, true);
        Lighting.AddLight(Projectile.Center, .35f, 1f, 0f);

        for (int i = 0; i < 3; i++)
        {
            Vector2 pos = Projectile.RandAreaInEntity() + Projectile.velocity;
            Vector2 vel = Projectile.velocity;
            float size = Main.rand.NextFloat(.3f, .6f) * Projectile.scale;
            int time = Main.rand.Next(10, 16);
            float opacity = Main.rand.NextFloat(.7f, 1.1f);
            Color color = new(179, 252, 0);
            Color color2 = new(96, 248, 2);
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, time, size, color, opacity, true);
            if (Main.rand.NextBool(3))
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, time, size, color2, opacity, true);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.width * 3f * Projectile.scale, targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.CursedInferno, 420);
    }
}
