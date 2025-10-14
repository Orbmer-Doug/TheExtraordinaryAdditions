using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late.Zenith;

public class CoalescedAmalgam : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 16;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 600;
        Projectile.penetrate = 1;
    }

    public NPC Target;
    public ref float Time => ref Projectile.ai[0];
    private const int TimeToHome = 60;
    public override void AI()
    {
        if (Target == null || Target.active == false)
            Target = NPCTargeting.GetClosestNPC(new(Projectile.Center, 3000));

        if (Time % 2f == 1f)
        {
            ParticleRegistry.SpawnLightningArcParticle(Projectile.Center,
                Main.rand.NextVector2CircularLimited(180f, 180f, .5f, 1f), Main.rand.Next(6, 8), Main.rand.NextFloat(.6f, .8f) * Projectile.scale, Color.Gold);
        }
        for (int i = 0; i < 5; i++)
        {
            float angularVelocity = Main.rand.NextFloat(0.045f, 0.09f);
            Vector2 vel = Projectile.velocity.RotatedByRandom(.1f) * Main.rand.NextFloat(.1f, 1f);
            Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel, Main.rand.Next(14, 23), Main.rand.NextFloat(.7f, 1f) * Projectile.scale, fireColor, 1f, true, angularVelocity);
        }

        if (Time < TimeToHome)
        {
            Projectile.scale = Animators.MakePoly(3).OutFunction(InverseLerp(0f, TimeToHome, Time));
            Projectile.velocity = Projectile.velocity.RotatedBy(.08f * (1f - Projectile.scale) * (Projectile.identity % 2 == 1).ToDirectionInt());
            Projectile.velocity *= .975f;

            Projectile.ExpandHitboxBy((int)(Projectile.scale * 64f));
        }
        if (Time == TimeToHome)
        {
            for (int i = 0; i < 30; i++)
                ParticleRegistry.SpawnSquishyLightParticle(Projectile.Center, Main.rand.NextVector2Circular(9f, 9f),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .5f), Color.LightGoldenrodYellow, Main.rand.NextFloat(.7f, 1f));
        }
        if (Time > TimeToHome + 20f)
        {
            if (Target.CanHomeInto())
            {
                float interpol = Animators.MakePoly(4).OutFunction(InverseLerp(TimeToHome + 20f, TimeToHome + 50f, Time));
                float speed = 26f * interpol;
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(Target.Center) * speed, interpol * .2f);
            }
        }

        Time++;
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (Target == null || !Target.active)
            return false;
        if (Time < 30f)
            return false;

        return null;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.etherealHit4.Play(Projectile.Center, .8f, -.1f, .2f, 10);
        RotatedRectangle rect = Projectile.RotHitbox(Projectile.velocity.ToRotation());
        for (int i = 0; i < 60; i++)
        {
            Vector2 pos = i < 30 ? rect.Right : rect.RandomPoint();
            Vector2 vel = i < 30 ? -Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.4f, .9f) : -Projectile.velocity * Main.rand.NextFloat(.2f, .8f);
            int life = Main.rand.Next(20, 30);
            float scale = Main.rand.NextFloat(.4f, .6f);
            Color col = MulticolorLerp(Main.rand.NextFloat(), Color.Gold, Color.Goldenrod, Color.DarkGoldenrod);

            if (i < 30)
            {
                if (i % 4 == 3)
                    ParticleRegistry.SpawnLightningArcParticle(pos, vel.RotatedByRandom(.8f) * 6f, life - 10, scale, col);

                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, col, Color.White);
            }
            else
            {
                Color fireColor = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Red, Color.OrangeRed, Color.IndianRed, Color.DarkRed, Color.Orange, Color.DarkOrange, Color.OrangeRed * 1.6f);
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, Main.rand.Next(19, 28), Main.rand.NextFloat(.7f, 1f), fireColor, 1f);
                ParticleRegistry.SpawnSquishyLightParticle(pos, vel, life, scale, col);
            }
        }
    }
}
