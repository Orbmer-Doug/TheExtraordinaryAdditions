using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class EbonySnipe : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(20);
        Projectile.friendly = Projectile.usesLocalNPCImmunity = Projectile.ignoreWater = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 1000;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.MaxUpdates = 100;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        if (Time % 2 == 1)
            MetaballRegistry.SpawnOnyxMetaball(Projectile.Center, Vector2.Zero, 40, 45);
        Time++;
    }
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 50; i++)
        {
            MetaballRegistry.SpawnOnyxMetaball(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), Vector2.Zero, 70, 80);
        }

        if (damageDone > target.life)
            Projectile.penetrate++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        for (int i = 0; i < 50; i++)
        {
            MetaballRegistry.SpawnOnyxMetaball(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f), Vector2.Zero, 70, 80);
        }
        return true;
    }
}
