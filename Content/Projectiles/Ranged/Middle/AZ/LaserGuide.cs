using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

public class LaserGuide : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.extraUpdates = 100;
        Projectile.timeLeft = 5 * Projectile.extraUpdates;
    }

    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (Projectile.Hitbox.Intersects(npc.Hitbox))
            {
                Projectile.Kill();
            }
        }
        if (Projectile.ai[0]++ % Main.rand.Next(1, 4) == 0f)
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, Projectile.velocity * .01f, 20, 4f, TankHeadHoldout.GetTeamColor(Owner));
    }

    public override bool? CanCutTiles() => false;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }
}
