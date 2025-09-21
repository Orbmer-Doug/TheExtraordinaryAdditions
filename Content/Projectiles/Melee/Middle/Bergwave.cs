using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class Bergwave : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(1f);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = false;
        Projectile.localNPCHitCooldown = 12;
        Projectile.timeLeft = 100;
        Projectile.penetrate = 4;
        Projectile.MaxUpdates = 1;
    }

    public List<Vector2> points = [];
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        Vector2 c1 = Projectile.Center + PolarVector(120f, Projectile.rotation - MathHelper.PiOver2);
        Vector2 c2 = Projectile.Center + PolarVector(80f, Projectile.rotation);
        Vector2 c3 = Projectile.Center + PolarVector(120f, Projectile.rotation + MathHelper.PiOver2);
        points = Animators.CatmullRomSpline([c1, c2, c3], 30);
        Projectile.velocity *= .98f;

        foreach (Vector2 point in points)
        {
            if (Collision.SolidCollision(point, 1, 1))
                Projectile.Kill();

            float scaling = Convert01To010(InverseLerp(0f, points.Count, points.IndexOf(point)));
            if (Main.rand.NextBool(10))
            {
                ParticleRegistry.SpawnCloudParticle(point, Projectile.velocity * .2f, AuroraGuard.Icey, AuroraGuard.PastelViolet, Main.rand.Next(20, 30),
                    Main.rand.NextFloat(20f, 40f), Main.rand.NextFloat(.6f, 1f) * scaling, Main.rand.NextByte(0, 2));
            }
            if (Main.rand.NextBool(16))
                ParticleRegistry.SpawnBloomPixelParticle(point, -Projectile.velocity * Main.rand.NextFloat(.2f, .4f), Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .6f) * scaling, AuroraGuard.MauveBright, AuroraGuard.Lavender);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points, 40);
    }
}
