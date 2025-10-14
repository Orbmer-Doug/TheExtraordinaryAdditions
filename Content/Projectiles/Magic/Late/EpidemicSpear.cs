using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class EpidemicSpear : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public Projectile Proj => Main.projectile[(int)Projectile.ai[1]];
    public Player Owner => Main.player[Projectile.owner];
    private bool Released
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 64;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.timeLeft = 1200;
        Projectile.netImportant = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;

        Projectile.scale = StartingScale;
    }
    public const float StartingScale = 0f;
    public const float IdealScale = 2f;

    public static readonly int TotalCharge = SecondsToFrames(5f);

    public override bool? CanDamage()
    {
        if (Time < TotalCharge)
            return false;
        return null;
    }

    public override void AI()
    {
        Projectile.scale = InverseLerp(0f, TotalCharge, Time) * 1.2f;
        Projectile.ExpandHitboxBy((int)(Projectile.scale * 20f));

        if (this.RunLocal() && Owner.Additions().MouseRight.Current && !Released)
        {
            Projectile.timeLeft = 180;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Proj.Center + Vector2.UnitY * -(Proj.Size.Length() * .5f + 80f)) * MathHelper.Max(10f, Projectile.velocity.Length()), .2f);
        }

        if (this.RunLocal() && Owner.Additions().MouseRight.Current == false && Time >= TotalCharge && !Released)
        {
            Projectile.extraUpdates = 2;
            Projectile.velocity = Projectile.SafeDirectionTo(Owner.Additions().mouseWorld) * 15f;
            AdditionsSound.etherealLoose.Play(Projectile.Center, 1.2f, -.3f);

            Released = true;
        }

        if (Projectile.velocity != Projectile.oldVelocity)
            this.Sync();

        if (this.RunLocal() && Owner.Additions().MouseRight.Current == false && Time < TotalCharge && !Released)
            Projectile.Kill();

        ref float Offset = ref Projectile.AdditionsInfo().ExtraAI[0];
        Offset += .05f % MathHelper.TwoPi;
        int arms = 3;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int i = 0; i < arms; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / arms + Offset).ToRotationVector2() * Main.rand.NextFloat(2f, 4f) * Projectile.scale;
                if (x == -1)
                    vel = (MathHelper.TwoPi * i / arms - Offset).ToRotationVector2() * Main.rand.NextFloat(4f, 8f) * Projectile.scale;

                ShaderParticleRegistry.SpawnEpidemicParticle(Projectile.Center, vel, x == -1 ? 45f : 40f * Projectile.scale);
            }
        }

        if (Released)
        {
            const int Amt = 25;
            for (int i = 0; i < Amt; i++)
            {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero) * 2f;

                float interpolant = InverseLerp(0, Amt, i);

                Vector2 pos = Vector2.Lerp(Projectile.Center, Projectile.Center + vel * 150f, interpolant);

                float scale = (1f - interpolant) * 40f;
                ShaderParticleRegistry.SpawnEpidemicParticle(pos, vel, scale * Projectile.scale);
            }
        }

        if (Time == TotalCharge && Projectile.localAI[0] == 0f)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 30, RandomRotation(), Vector2.One, 0f, 220f, Color.DarkOliveGreen);
            AdditionsSound.HeatTail.Play(Projectile.Center, .8f, -.3f);
            Projectile.localAI[0] = 1f;
        }
        if (Time < TotalCharge)
            Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnCloudParticle(Projectile.Center, Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.4f, .6f),
                Color.LimeGreen, Color.DarkOliveGreen, Main.rand.Next(20, 50), Main.rand.NextFloat(.5f, .7f), Main.rand.NextFloat(.5f, 1.2f));
        }

        AdditionsSound.etherealHit2.Play(Projectile.Center, .8f, -.2f, 0f, 10, Name);
        target.AddBuff(BuffID.Poisoned, SecondsToFrames(6));
        target.AddBuff(BuffID.Venom, 120);
    }
}
