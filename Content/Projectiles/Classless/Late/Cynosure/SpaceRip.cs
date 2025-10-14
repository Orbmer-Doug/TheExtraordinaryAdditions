using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Metaball;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class SpaceRip : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.noise);
    public const int MaxUpdates = 3;
    public ref float Time => ref Projectile.ai[0];

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.localNPCHitCooldown = -1;
        Projectile.MaxUpdates = MaxUpdates;
        Projectile.timeLeft = 120 * MaxUpdates;
        Projectile.penetrate = -1;
        Projectile.Opacity = 0f;
    }

    public override void AI()
    {
        MetaballRegistry.SpawnGenediesMetaball(Projectile.Center, Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(50, 70), Main.rand.Next(360, 460));
        Time++;
    }
}