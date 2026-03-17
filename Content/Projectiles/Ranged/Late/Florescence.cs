using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Utilities;
using ParticleRegistry = TheExtraordinaryAdditions.Common.Particles.Particle.ParticleRegistry;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class Florescence : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Florescence);

    public GlobalPlayer Modded => Main.player[Projectile.owner].Additions();

    public int Timer
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int Wait
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public const int MaxWait = 20;

    public bool Hit
    {
        get => (int)Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.timeLeft = 40;
        Projectile.penetrate = 2;
        Projectile.friendly = Projectile.ignoreWater = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 2;
        Projectile.MaxUpdates = 2;
    }

    public override void AI()
    {
        if (Hit)
        {
            Wait++;
            if (this.RunLocal() && Wait.BetweenNum(MaxWait, MaxWait + 14))
            {
                Projectile.velocity = Projectile.Center.SafeDirectionTo(Modded.MouseWorld) * 20f;
            }
            
            if (Wait >= MaxWait)
                Projectile.MaxUpdates = 3;
            else
                Projectile.timeLeft = 120;
        }

        if (Timer % 2 == 1)
        {
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 vel = Projectile.velocity.RotatedBy(i == -1 ? -.2f : .2) * Main.rand.NextFloat(.3f, .5f);
                ParticleRegistry.SpawnSparkParticle(Projectile.Center,
                    vel,
                    20, 1.4f, Color.HotPink);
            }

            
        }
        ParticleRegistry.SpawnSquishyPixelParticle(Projectile.Center,
            Projectile.velocity * Main.rand.NextFloat(.1f, .2f), Main.rand.Next(30, 50),
            Main.rand.NextFloat(1.7f, 2.5f), Color.HotPink, Color.Pink, 5);
        Timer++;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return null;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Hit)
        {
            ParticleRegistry.SpawnPulseRingParticle(target.RotHitbox().GetClosestPoint(Projectile.Center),
                -Projectile.velocity * .05f, 30, Projectile.velocity.ToRotation(), new Vector2(.5f, 1f), 0f, 40f,
                Color.Pink);
            Projectile.velocity = -Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.5f, .6f);
            Hit = true;
            this.Sync();
        }

        Projectile.damage = (int)(Projectile.damage * .75f);
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 18; i++)
        {
            ParticleRegistry.SpawnGlowParticle(Projectile.RandAreaInEntity(),
                Projectile.velocity * Main.rand.NextFloat(.2f, .6f) + Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(18, 24),
                Projectile.width * Main.rand.NextFloat(.5f, 1.5f), Color.Pink);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }
}