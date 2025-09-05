using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class LightripBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 5;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 2.5f);
        if (!Main.dedServ)
        {
            for (int i = 0; i < 6; i++)
            {
                Vector2 off = Main.rand.NextVector2Unit(0f, MathHelper.TwoPi) * (float)Math.Pow(Main.rand.NextFloat(), 2.4) * Projectile.Size * 0.5f;
                Vector2 vel = off.SafeNormalize(Vector2.UnitY).RotatedByRandom((double)(MathHelper.PiOver2 * Main.rand.NextFloatDirection()));
                Vector2 val2 = off / Projectile.Size / 0.5f;
                vel *= MathHelper.Lerp(3f, 9f, Utils.GetLerpValue(0.05f, 0.85f, val2.Length(), false));

                Vector2 pos = Projectile.Center + off;
                Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue, Color.SkyBlue, Color.LightCyan);

                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel / 2, 50, 1f, color, .4f, true);
                ParticleRegistry.SpawnMistParticle(pos, vel.RotatedByRandom(.3f), Main.rand.NextFloat(.7f, 1.1f), color, Color.Transparent, Main.rand.NextFloat(160f, 190f), Main.rand.NextFloat(-.2f, .2f));
            }
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 center = Projectile.Center;
        Vector2 size = Projectile.Size;
        return CircularHitboxCollision(center, size.Length() / 2, targetHitbox);
    }
}