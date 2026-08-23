using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class EbonySnipe : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.Size = new(20);
        Projectile.friendly = Projectile.usesLocalNPCImmunity = Projectile.ignoreWater = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 1000;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.MaxUpdates = 100;
    }

    public ref float Time => ref Projectile.ai[0];

    public override void AI()
    {
        if ((int) Time % 2 == 1)
        {
            float rot = Projectile.velocity.SafeNormalize(Vector2.Zero).PerpCW().ToRotation();
            MetaballRegistry.SpawnOnyxMetaball(Projectile.Center, Vector2.Zero, 40, 40, rot);
        }

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 50; i++)
            MetaballRegistry.SpawnOnyxMetaball(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f),
                Vector2.Zero, 70, 70);

        Projectile.velocity *= .4f;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        for (int i = 0; i < 50; i++)
            MetaballRegistry.SpawnOnyxMetaball(Projectile.Center + Main.rand.NextVector2Circular(40f, 40f),
                Vector2.Zero, 70, 70);
        return true;
    }
}
