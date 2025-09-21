using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using static TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora.AuroraGuard;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

public class HeavyFrostBlast : ProjOwnedByNPC<AuroraGuard>
{
    public override string Texture => AssetRegistry.Invis;
    public const int Lifetime = 80;
    public override void SetDefaults()
    {
        Projectile.Size = new(20f);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.MaxUpdates = 4;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        float interpolant = InverseLerp(0f, Lifetime, Time);
        float size = Animators.MakePoly(2.4f).InFunction.Evaluate(Time, 0f, Lifetime, .2f, 1.5f);
        Projectile.ExpandHitboxBy((int)(size * 170));

        Color col = MulticolorLerp(interpolant, Icey, LightCornflower, MauveBright, Lavender, BrightViolet);
        for (int i = 0; i < 5; i++)
        {
            int life = (int)(30 + (size * 10));
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.RandAreaInEntity(), Vector2.Zero, life, size * 3f, col);
            ParticleRegistry.SpawnCloudParticle(Projectile.RandAreaInEntity(), Vector2.Zero, col, Color.DarkViolet, life - 9, size * .1f, Main.rand.NextFloat(.5f, .8f), Main.rand.NextByte(0, 2));
        }

        foreach (Player player in Main.ActivePlayers)
        {
            if (player != null && player.Hitbox.Intersects(Projectile.Hitbox) && player.velocity.Length() < 40f)
            {
                player.velocity += Projectile.velocity * .82f / Projectile.MaxUpdates;
            }
        }
        Time++;
    }

    public override bool? CanCutTiles()
    {
        return true;
    }
}
