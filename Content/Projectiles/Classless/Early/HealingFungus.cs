using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Early;

public class HealingFungus : ModProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.GlowingMushroom;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5;
    }
    public override void SetDefaults()
    {
        Projectile.width = 22; Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = SecondsToFrames(8);
    }
    public override bool? CanDamage() => false;
    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public FancyAfterimages fancy;
    public override void AI()
    {
        fancy ??= new(5, () => Projectile.Center);
        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .1f, -22f, 22f);

        // Spawn some mushroom-y dust
        if (Projectile.ai[0]++ % 7f == 0f)
        {
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.GlowingMushroom, Main.rand.NextVector2Circular(3f, 3f), 0, default, Main.rand.NextFloat(.8f, 1.1f));
        }

        Projectile.rotation += Projectile.velocity.X * .03f;
        fancy?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity * .4f, Projectile.rotation, 0, 170, 5, 2f, null, true, -.2f));

        // Heal effects
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.active && !player.dead && player != null && player.Hitbox.Intersects(Projectile.Hitbox))
            {
                player.Heal(Main.rand.Next(3, 6));
                Projectile.Kill();
            }
        }

        Projectile.Opacity = InverseLerp(0f, 10f, Time);
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);
        int count = 20;
        for (int i = 0; i < count; i++)
        {
            Vector2 shootVelocity = (MathHelper.TwoPi * i / count + offsetAngle).ToRotationVector2() * 5f;
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, shootVelocity, 20, 30f, Color.DarkBlue);
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X / 2;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y / 2;
        
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        fancy?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.Black, Color.DarkBlue, Color.AliceBlue, Color.LightCyan]);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
