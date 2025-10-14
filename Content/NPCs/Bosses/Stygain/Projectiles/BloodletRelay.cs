using Terraria;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class BloodletRelay : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.friendly = Projectile.hostile = false;
        Projectile.height = Projectile.width = 16;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override void SafeAI()
    {
        if (Owner == null)
        {
            Projectile.Kill();
            return;
        }

        Projectile.netUpdate = true;
        ref float Offset = ref Projectile.ai[2];
        Offset += .09f * (Projectile.identity % 2f == 1f).ToDirectionInt() % MathHelper.TwoPi;
        int arms = 3;
        for (int i = 0; i < arms; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * i / arms + Offset).ToRotationVector2().RotatedBy(4) * Main.rand.NextFloat(2.1f, 4.1f);
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, 20, 42f, Color.DarkRed, 1f);
        }

        Projectile.velocity = Projectile.SafeDirectionTo(Owner.Center) * 12f;
        if (Projectile.Hitbox.Intersects(Owner.Hitbox))
        {
            int healAmount = (int)Projectile.ai[0];
            if (Owner.life < Owner.lifeMax)
                Owner.life += healAmount;
            if (Owner.life > Owner.lifeMax)
                Owner.life = Owner.lifeMax;

            if (this.RunServer())
            {
                Owner.HealEffect(healAmount);
                Owner.netUpdate = true;
            }

            Projectile.Kill();
        }
    }
}
