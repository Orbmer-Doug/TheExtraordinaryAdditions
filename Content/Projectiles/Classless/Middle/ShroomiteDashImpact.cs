using Terraria;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class ShroomiteDashImpact : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 120;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 110;
        Projectile.timeLeft = 10;
        Projectile.extraUpdates = 0;
        Projectile.penetrate = -1;
        Projectile.ownerHitCheck = false;
        Projectile.light = 0f;
        Projectile.netImportant = true;
        Projectile.netUpdate = true;
    }
}