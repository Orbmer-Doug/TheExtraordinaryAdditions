using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle.AZ;

// jerma
public class Grub : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Grub);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.timeLeft = SecondsToFrames(10);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        AdditionsSound.explosion_large_08.Play(Projectile.Center);
        for (int i = 0; i < 30; i++)
            ParticleRegistry.SpawnMistParticle(Projectile.Center, Main.rand.NextVector2Circular(4f, 4f), Main.rand.NextFloat(.5f, 1.3f), Color.DarkGray, Color.Black, 255);

        ScreenShakeSystem.New(new(20f, .9f), Projectile.Center);
        for (int i = 0; i < Main.rand.Next(60, 90); i++)
        {
            float rand = Main.rand.NextFloat(5f, 10f);
            Vector2 vel = Main.rand.NextVector2CircularEdge(rand, rand);
            if (this.RunLocal())
                Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<GrubShrapnel>(), Projectile.damage, 0f, Projectile.owner);
        }
    }
}
