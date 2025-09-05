using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

public class Slug : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Slug);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = SecondsToFrames(10);
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
    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnMistParticle(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), Main.rand.NextFloat(.5f, 1f), Color.DarkGray, Color.DarkSlateGray, 170);
        }
    }
}
