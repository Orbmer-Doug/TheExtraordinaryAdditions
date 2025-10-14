using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class LokiBoom : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = 60;
        Projectile.height = 60;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 10;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.75);
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 2f);
        if (Projectile.localAI[0] == 0f)
        {
            SoundID.DD2_FlameburstTowerShot.Play(Projectile.Center);
            Projectile.localAI[0] += 1f;
        }
        for (int i = 0; i < 10; i++)
        {
            Vector2 dustSpawnOffset = Main.rand.NextVector2Unit(0f, MathHelper.TwoPi) * (float)Math.Pow(Main.rand.NextFloat(), 2.4) * Projectile.Size * 0.5f;
            Vector2 dustVelocity = dustSpawnOffset.SafeNormalize(Vector2.UnitY).RotatedByRandom((double)(MathHelper.PiOver2 * Main.rand.NextFloatDirection()));
            Vector2 val = dustVelocity;
            Vector2 val2 = dustSpawnOffset / Projectile.Size / 0.5f;
            dustVelocity = val * MathHelper.Lerp(1f, 5f, Utils.GetLerpValue(0.05f, 0.85f, ((Vector2)val2).Length(), false));

            Vector2 pos = Projectile.Center + dustSpawnOffset;
            ParticleRegistry.SpawnMistParticle(pos, dustVelocity, Main.rand.NextFloat(.7f, 1.5f), Color.Yellow, Color.Orange, 190, Main.rand.NextFloat(-.2f, .2f));
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.Size.Length() * .5f, targetHitbox);
    }
}