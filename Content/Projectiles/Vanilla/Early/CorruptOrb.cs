using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class CorruptOrb : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 11;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 600;
        Projectile.penetrate = 1;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool Released
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        if (Time >= 20f && !Released)
        {
            Projectile.velocity *= .94f;

            NPC npc = NPCTargeting.GetClosestNPC(new(Projectile.Center, 350, true));
            if (npc.CanHomeInto())
            {
                Projectile.velocity = Projectile.SafeDirectionTo(npc.Center) * 10f;
                Released = true;
            }
        }

        Vector2 vel = Released ? -Projectile.velocity * Main.rand.NextFloat(-.04f, .04f) : -Vector2.UnitY.RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 4f);
        for (int i = 0; i < 3; i++)
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, Main.rand.Next(20, 30), Projectile.width * 1.3f, Color.Violet, Main.rand.NextFloat(1f, 1.5f));

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        if (Released)
        {
            for (int i = 0; i < 12; i++)
            {
                ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f), Color.DarkViolet, Color.MediumPurple, null, 1.8f, 8);
            }
        }
    }
}
