using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class MeltGlobule : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.width =
        Projectile.height = 26;
        Projectile.penetrate = 5;
        Projectile.timeLeft = 360;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void AI()
    {
        MoltenBall.Spawn(Projectile.Center, Main.rand.NextFloat(40f, 65f));

        Projectile.StickyProjAI(15);

        if (Projectile.ai[0] == 0f)
        {
            Projectile.velocity.Y += .3f;
        }

        if (Projectile.ai[0] == 2f)
        {
            Projectile.velocity *= 0f;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Projectile.ModifyHitNPCSticky(20);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.ai[0] = 2f;
        Projectile.timeLeft = 300;
        return false;
    }
}
