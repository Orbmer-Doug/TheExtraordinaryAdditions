using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class DissipatingLight : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.width =
        Projectile.height = 60;
        Projectile.alpha = 255;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 10;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.penetrate = -1;
    }
    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            ParticleOrchestrator.RequestParticleSpawn(false,
                ParticleOrchestraType.Excalibur,
                new ParticleOrchestraSettings { PositionInWorld = Projectile.Center },
                Projectile.owner);
            Projectile.localAI[0] = 1f;
        }
    }
}
