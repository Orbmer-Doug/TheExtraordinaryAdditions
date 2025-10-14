using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class InfernalBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 150;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = SecondsToFrames(2);
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 25;
    }

    public override void AI()
    {
        if (Projectile.ai[0] == 0f)
        {
            SoundID.Item74.Play(Projectile.Center);
            Projectile.ai[0] = 1f;
            Projectile.netUpdate = true;
        }

        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 2.5f);

        for (int i = 0; i < 10; i++)
        {
            Vector2 dustSpawnOffset = Main.rand.NextVector2Circular(1f, 1f) * (float)Math.Pow(Main.rand.NextFloat(), 2.4) * Projectile.Size * 0.5f;
            Vector2 dustVelocity = dustSpawnOffset.SafeNormalize(Vector2.UnitY).RotatedByRandom((double)(MathHelper.PiOver2 * Main.rand.NextFloatDirection()));
            Vector2 val2 = dustSpawnOffset / Projectile.Size / 0.5f;
            dustVelocity = dustVelocity * MathHelper.Lerp(3f, 6f, Utils.GetLerpValue(0.05f, 0.85f, val2.Length(), false));

            Vector2 pos = Projectile.Center + dustSpawnOffset;
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);

            ParticleRegistry.SpawnMistParticle(pos, dustVelocity, Main.rand.NextFloat(.7f, 1.5f), color, Color.Transparent, 190, Main.rand.NextFloat(-.2f, .2f));
            ParticleRegistry.SpawnHeavySmokeParticle(pos, dustVelocity / 2, 50, 1f, color, .4f, true);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, 60 * Main.rand.Next(8, 16));
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.Size.Length() * 0.5f, targetHitbox);
    }
}