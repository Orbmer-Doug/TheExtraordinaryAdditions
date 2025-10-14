using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using static TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora.AuroraGuard;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

public class OverheatedBlast : ProjOwnedByNPC<AuroraGuard>
{
    public override string Texture => AssetRegistry.Invis;
    public override bool IgnoreOwnerActivity => true;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 300;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 10;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        if (Projectile.ai[0] == 0f)
        {
            Vector2 pos = Projectile.Center;

            for (int i = 0; i < 4; i++)
                ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, Vector2.One * Projectile.width, Vector2.Zero, 40 - (i * 4), SlateBlue, null, Icey, true);
            for (int i = 0; i < 100; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(20f, 20f);
                int life = Main.rand.Next(40, 60);
                float scale = Main.rand.NextFloat(.4f, .8f);
                Color col = MulticolorLerp(Main.rand.NextFloat(), Icey, LightCornflower, PastelViolet, Lavender);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scale, col, Icey, null, 2f, 9);
                ParticleRegistry.SpawnBloomLineParticle(pos, vel * 2.2f, life - 15, scale, col);
                ParticleRegistry.SpawnCloudParticle(pos, vel, col, DeepBlue, life * 2, scale * .6f, Main.rand.NextFloat(.4f, .8f), Main.rand.NextByte(0, 2));
            }

            Projectile.ai[0] = 1f;
        }
    }

    public override bool ShouldUpdatePosition() => false;
}