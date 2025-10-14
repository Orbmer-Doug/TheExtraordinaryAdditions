using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class ThePropane : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ThePropane);
    private const int ExplosionWidthHeight = 250;
    public ref float Time => ref Projectile.ai[0];
    private bool Big
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override void SetDefaults()
    {
        Projectile.width = 86;
        Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.ignoreWater = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 150;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Main.expertMode)
        {
            if (target.type >= NPCID.EaterofWorldsHead && target.type <= NPCID.EaterofWorldsTail)
                modifiers.FinalDamage /= 5;
        }

        if (Big)
            modifiers.FinalDamage *= 2;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.soundDelay == 0)
        {
            SoundStyle impactSound = SoundID.DD2_FlameburstTowerShot;
            SoundEngine.PlaySound(impactSound with { Volume = .9f, PitchVariance = .15f }, Projectile.Center);
        }
        Projectile.soundDelay = 20;

        return false;
    }

    public override void AI()
    {
        if (Projectile.timeLeft <= 3)
        {
            Projectile.tileCollide = false;
            Projectile.Opacity = 0f;

            Projectile.Resize(ExplosionWidthHeight, ExplosionWidthHeight);

            Projectile.penetrate = -1;
            Projectile.knockBack = 10f;
            Big = true;
            this.Sync();
        }
        else
        {
            Projectile.Opacity = InverseLerp(0f, 15f, Time);

            if (Main.rand.NextBool())
            {
                Vector2 val = Projectile.Center + PolarVector(38f, Projectile.rotation);

                Dust.NewDustPerfect(val, DustID.Torch, -Vector2.UnitY.RotatedByRandom(.2f) * Main.rand.NextFloat(1f, 2f), 0, default, Main.rand.NextFloat(1.5f, 1.8f)).noGravity = true;
                Dust.NewDustPerfect(val, DustID.Smoke, -Vector2.UnitY.RotatedByRandom(.45f) * Main.rand.NextFloat(2.4f, 4f), 0, default, Main.rand.NextFloat(2f, 2.2f)).noGravity = true;
            }
        }

        Time++;
        if (Time > 10f)
        {
            // Roll speed dampening.
            if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
            {
                Projectile.velocity.X *= 0.96f;

                if (Projectile.velocity.X > -0.01 && Projectile.velocity.X < 0.01)
                {
                    Projectile.velocity.X = 0f;
                    Projectile.netUpdate = true;
                }
            }

            // Delayed gravity
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .16f, -50f, 40f);
        }

        Projectile.rotation += Projectile.velocity.X * .04f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Big)
            Projectile.damage = (int)(Projectile.damage * .85f);
    }

    public override void OnKill(int timeLeft)
    {
        // Play explosion sound
        SoundID.DD2_ExplosiveTrapExplode.Play(Projectile.Center, 1.1f, 0f, .2f);

        for (int i = 0; i < 180; i++)
        {
            Color color = Color.Lerp(Color.OrangeRed, Color.Chocolate, Main.rand.NextFloat(.2f, .6f));
            float opacity = Main.rand.NextFloat(2f, 3f);
            Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width / 2, Projectile.width / 2);
            ParticleRegistry.SpawnHeavySmokeParticle(pos, Main.rand.NextVector2Circular(2f, 2f), 40, Main.rand.NextFloat(.5f, 1f), color, opacity);
            ParticleRegistry.SpawnGlowParticle(pos, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, 1f), color, opacity);
        }

        Projectile.Resize(86, 20);
    }
}