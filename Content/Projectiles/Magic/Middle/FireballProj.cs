using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class FireballProj : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int TotalTime = 180;
    private const int TotalSize = 26;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = TotalSize;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = TotalTime;
    }
    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        float completion = 1f - InverseLerp(0f, TotalTime, Time);
        Projectile.scale = GetLerpBump(0f, .1f, 1f, .98f, completion);
        Projectile.Resize((int)(TotalSize * Projectile.scale), (int)(TotalSize * Projectile.scale));
        Projectile.damage = (int)(Owner.GetWeaponDamage(Owner.HeldItem) * Projectile.scale);
        Projectile.knockBack = Projectile.scale * Owner.HeldItem.knockBack;

        for (int i = 0; i < 4; i++)
        {
            Vector2 pos = Projectile.Center;
            Vector2 vel = Projectile.velocity.RotatedByRandom(.1f) * Main.rand.NextFloat(.1f, 1f);
            int life = Main.rand.Next(30, 40);
            float scale = Main.rand.NextFloat(.4f, .9f) * Projectile.scale;
            Color color = FireballHoldout.FireColors[Main.rand.Next(FireballHoldout.FireColors.Length - 1)];
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, color);

            if (i.BetweenNum(0, 2))
            {
                ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale, color);
                if (Main.rand.NextBool())
                    ParticleRegistry.SpawnSparkParticle(pos, -vel.RotatedByRandom(.4f), life / 2, scale, color, true, true);
            }

            if (i == 0 && Main.rand.NextBool())
                ParticleRegistry.SpawnBloomPixelParticle(pos, -vel.RotatedByRandom(.17f), life, scale * .7f, color, Color.White, null, 1.4f);
        }

        Time++;
    }
    public void Kaboom()
    {
        Vector2 pos = Projectile.Center;
        for (int i = 0; i < 35; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * i / 30 + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(.5f, 7f) * Projectile.scale;
            int life = Main.rand.Next(30, 50);
            float scale = Main.rand.NextFloat(.5f, 1f) * Projectile.scale;
            Color color = FireballHoldout.FireColors[Main.rand.Next(FireballHoldout.FireColors.Length - 1)];

            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, color);
            ParticleRegistry.SpawnCloudParticle(pos, vel * .4f, color, Color.DarkGray, life, scale * 60f, Main.rand.NextFloat(.5f, .8f));
            ParticleRegistry.SpawnGlowParticle(pos, vel, life, scale * 150, color, .4f);
            ParticleRegistry.SpawnSparkParticle(pos, vel * 3.4f + Vector2.UnitY * -5f, life / 2, scale * 1.8f, color * 1.2f);
        }

        SoundEngine.PlaySound(SoundID.Item14 with { Pitch = -.2f, Volume = Projectile.scale + .1f, MaxInstances = 0 }, Projectile.Center);
        Projectile.CreateFriendlyExplosion(Projectile.Center, Projectile.Size * 3.5f, Projectile.damage, Projectile.knockBack, 5, 7);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.OnFire, 180);
        target.AddBuff(BuffID.OnFire3, 120);
        Kaboom();
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Kaboom();
        return true;
    }
}
