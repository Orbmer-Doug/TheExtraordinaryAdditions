using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

public class Laser : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 8;
        Projectile.extraUpdates = 100;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.timeLeft = 10 * Projectile.extraUpdates;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
    }
    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        Color col = Color.Transparent;
        if (Owner.team == (int)Team.None)
        {
            col = Color.Green;
        }
        if (Owner.team == (int)Team.Red)
        {
            col = Color.Red;
        }
        if (Owner.team == (int)Team.Green || Main.netMode == NetmodeID.SinglePlayer)
        {
            col = Color.LimeGreen;
        }
        if (Owner.team == (int)Team.Blue)
        {
            col = Color.Blue;
        }
        if (Owner.team == (int)Team.Yellow)
        {
            col = Color.Gold;
        }
        if (Owner.team == (int)Team.Pink)
        {
            col = Color.Pink;
        }

        ParticleRegistry.SpawnSparkParticle(Projectile.Center, Projectile.velocity * Main.rand.NextFloat(-.01f, .01f), 30, .5f, col);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
        {
            Projectile.velocity.X = -oldVelocity.X;
        }
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
        {
            Projectile.velocity.Y = -oldVelocity.Y;
        }
        return false;
    }
}