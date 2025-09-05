using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class HellFlame : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.height = Projectile.width = 24;
        Projectile.friendly = true;
        Projectile.timeLeft = 200;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Projectile.velocity *= .975f;

        float timeInterpolant = Utils.GetLerpValue(0f, 200f, Projectile.timeLeft, true);
        // I put blue here to clearly see where the colors were changing and it turned out prettier than the original
        // whatever
        float val = Cos01(timeInterpolant);
        Color col = Color.Lerp(Color.Blue, Color.OrangeRed, val);
        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
            Vector2 vel = Vector2.Zero;
            float scale = Main.rand.NextFloat(.35f, .6f) * Projectile.scale;
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, 50, scale, col, 1f, true);
            if (i % 2 == 1)
            {
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * .9f, 40, scale, col * .5f, .9f, true);
            }
        }

        Projectile.scale = timeInterpolant;
        Lighting.AddLight(Projectile.Center, col.ToVector3() * 1.5f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 7; i++)
        {
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(-1f, -4f), 40, Main.rand.NextFloat(.9f, 1.5f), Color.OrangeRed);
        }
    }
}
