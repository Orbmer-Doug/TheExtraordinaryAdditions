using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class BouncyIcicle : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DiscIceProjectile);

    public override void SetDefaults()
    {
        Projectile.width = 42;
        Projectile.height = 30;
        Projectile.timeLeft = 1200;
        Projectile.penetrate = 2;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.extraUpdates = 0;
        Projectile.active = true;
        Projectile.noEnchantmentVisuals = true;
        Projectile.reflected = true;
        Projectile.scale = 1f;
        Projectile.aiStyle = 0;
    }

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        Lighting.AddLight(Projectile.Center, Color.DarkBlue.ToVector3() * .6f);

        if (Projectile.velocity.Length() < 30f)
            Projectile.velocity *= 1.015f;
        
        if (Projectile.ai[0]++ % 2 == 1)
            ParticleRegistry.SpawnMistParticle(Projectile.Center, Projectile.velocity * Main.rand.NextFloat(.2f, .5f), Main.rand.NextFloat(.4f, .8f), Color.DarkBlue, Color.DarkSlateBlue, 190);

        Projectile.FacingRight();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.ColdHitBig.Play(Projectile.Center, .5f, 0f, .1f, 10);
        Projectile.Kill();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.penetrate--;
        AdditionsSound.ColdBounce.Play(Projectile.Center, 1.1f, 0f, .2f, 10);

        for (int i = 0; i < 10; i++)
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceGolem, 0f, 0f, 100, default, 2f);
            dust.noGravity = true;
            dust.velocity *= 3f;
        }

        // Bouncy
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        AdditionsSound.ColdBallThrow.Play(Projectile.Center, .4f, 0f, .2f, 10);

        for (int i = 0; i < 20; i++)
            ParticleRegistry.SpawnDustParticle(Projectile.BaseRotHitbox().Right, -Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(.2f, .3f),
                Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .8f), Color.DarkSlateBlue, .1f, false, true);
    }
    
    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.LightCyan], Projectile.Opacity);

        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, direction, 0);
        return false;
    }
}