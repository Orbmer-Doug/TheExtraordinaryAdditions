using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class EpidemicLob : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private bool HitTarget
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    private bool HitGround
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    private ref float Timer => ref Projectile.ai[1];
    private ref float Counter => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float EnemyID => ref Projectile.ai[2];

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.timeLeft = SecondsToFrames(4);

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 8;
    }

    private Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(offset);
    public override void ReceiveExtraAI(BinaryReader reader) => offset = reader.ReadVector2();
    private const int FadeIn = 15;

    public static readonly int Charge = SecondsToFrames(2);
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.Olive.ToVector3() * Projectile.scale);

        float fadeInter = Utils.GetLerpValue(0f, FadeIn, Timer, true);
        float inter = InverseLerp(0f, Charge, Counter, true);
        Projectile.scale = fadeInter * (1f - inter);

        ShaderParticleRegistry.SpawnEpidemicParticle(Projectile.Center, Main.rand.NextVector2Circular(1f, 1f) * Projectile.scale, Projectile.scale * 50f);

        if (Timer % FadeIn == FadeIn - 1f)
        {
            int amt = 20;
            for (int i = 0; i < amt; i++)
            {
                Vector2 vel = Utility.GetPointOnRotatedEllipse(3f, 8f, Projectile.velocity.ToRotation(), Utils.Remap(i, 0, amt, 0f, MathHelper.TwoPi));
                Vector2 pos = Projectile.Center + vel;
                ShaderParticleRegistry.SpawnEpidemicParticle(pos, vel * Projectile.scale, Projectile.scale * 40f);
            }
        }

        if (Timer > FadeIn)
        {
            if (HitTarget == false && HitGround == false)
            {
                if (Projectile.velocity.Y < 16f)
                    Projectile.velocity.Y += .4f;
            }
        }

        if (HitTarget)
        {
            NPC target = Main.npc[(int)EnemyID];

            if (target == null || target.active == false)
                return;

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.timeLeft = 120;
                Projectile.position = target.position + offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
        }

        if (HitTarget || HitGround)
        {
            if (Counter > Charge)
                Projectile.Kill();
            if (Counter % 2f == 0f)
            {
                float scale = (1f - inter) * 100f;
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(scale, scale);
                ParticleRegistry.SpawnCloudParticle(Projectile.Center, RandomVelocity(2f, 1f, 4f), Color.LimeGreen, Color.DarkOliveGreen, 20, 1f - inter, .8f);
            }

            Counter++;
        }

        Timer++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            Projectile.velocity *= 0;
            Projectile.Center += oldVelocity * .8f;
            HitGround = true;
        }

        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HitTarget)
        {
            Projectile.tileCollide = false;
            Projectile.friendly = false;

            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;
            Projectile.velocity *= 0;

            HitTarget = true;
            this.Sync();
        }
    }

    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 12, 0f, Vector2.One, 0f, 500f, Color.DarkOliveGreen, true);

        float dustCount = MathHelper.TwoPi * 200 / 8f;

        // Spawn the main blast
        for (int i = 0; i < dustCount; i++)
        {
            float angle = MathHelper.TwoPi * i / dustCount;
            Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(6f, 10f);

            ShaderParticleRegistry.SpawnEpidemicParticle(Projectile.Center, vel, 50f);
        }

        // Create spikes
        for (int x = 0; x < 10; x++)
        {
            Vector2 vel = Projectile.Center.SafeDirectionTo(Projectile.Center + Main.rand.NextVector2CircularEdge(200f, 200f));

            const int Amt = 80;
            for (int i = 0; i < Amt; i++)
            {
                float interpolant = InverseLerp(0, Amt, i);

                Vector2 pos = Vector2.Lerp(Projectile.Center, Projectile.Center + vel * 350f, interpolant);

                float scale = (1f - interpolant) * 50f;
                ShaderParticleRegistry.SpawnEpidemicParticle(pos, vel, scale);
            }
        }

        AdditionsSound.etherealChargeBoom2.Play(Projectile.Center, 1f, -.2f, 0f, 20, Name);
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.ExpandHitboxBy(350, 350);
        Projectile.Damage();
    }
}
