using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class CharringBlastBlast : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 74;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 10;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire3, 50);
        Projectile.damage = (int)(Projectile.damage * 0.9);
    }

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Vector2 pos = Projectile.Center;
            ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Projectile.Size, Vector2.Zero, 40, Color.OrangeRed, null, Color.DarkRed * .5f, true);
            for (int i = 0; i < 120; i++)
            {
                Vector2 dustSpawnOffset = Main.rand.NextVector2Unit(0f, MathHelper.TwoPi) * (float)Math.Pow(Main.rand.NextFloat(), 2.4) * Projectile.Size * 0.5f;
                Vector2 vel = dustSpawnOffset.SafeNormalize(Vector2.UnitY).RotatedByRandom((double)(MathHelper.PiOver2 * Main.rand.NextFloatDirection()));
                Vector2 val2 = dustSpawnOffset / Projectile.Size / 0.5f;
                vel *= MathHelper.Lerp(1.25f, 5f, Utils.GetLerpValue(0f, 1f, val2.Length(), false));
                int life = Main.rand.Next(20, 30);
                float scale = Main.rand.NextFloat(.5f, .8f);

                ParticleRegistry.SpawnGlowParticle(pos, vel, life * 2, scale, Color.OrangeRed);
                ParticleRegistry.SpawnMistParticle(pos, vel, scale, Color.OrangeRed, Color.DarkRed, Main.rand.NextByte(100, 150));

                if (Main.rand.NextBool(3))
                {
                    ParticleRegistry.SpawnSquishyPixelParticle(pos, vel * 2.5f, life * 6, scale * 7.4f, Color.OrangeRed, Color.Chocolate * 1.4f, 8, true, true);
                }
            }
            Projectile.localAI[0] = 1f;
        }

        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 3f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.Size.Length() * .5f, targetHitbox);
    }
}