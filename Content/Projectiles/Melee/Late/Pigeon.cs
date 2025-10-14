using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class Pigeon : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Pigeon);

    public override void SetDefaults()
    {
        Projectile.width = 70;
        Projectile.height = 62;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = 2;
        Projectile.timeLeft = 600;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.extraUpdates = 0;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 40;
    }

    public override void AI()
    {
        Projectile.MaxUpdates = 3;
        after ??= new(4 * Projectile.MaxUpdates, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 155));

        if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 200, true, true), out NPC target))
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 14f, .2f);
        else
        {
            if (Projectile.velocity.Y < 30f)
                Projectile.velocity.Y += .3f;
        }

        Projectile.rotation += Projectile.velocity.Y * .1f;
        Projectile.rotation = Projectile.velocity.ToRotation();

        Projectile.velocity *= .99f;
        Projectile.Opacity = InverseLerp(0f, 10f, Projectile.ai[0]++);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.penetrate--;
        if (Projectile.penetrate <= 0)
        {
            Projectile.Kill();
        }
        else
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.BaseRotHitbox().Right, Vector2.Zero, 20, 0f, new(1f, .2f), 0f, Projectile.width, Color.Gray);
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item10, Projectile.position);

            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                Projectile.velocity.X = -oldVelocity.X;
            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                Projectile.velocity.Y = -oldVelocity.Y;
        }

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.NPCDeath1.Play(Projectile.Center, .3f, .3f, .2f, null, 400, Name);
        for (int i = 0; i < 40; i++)
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.Blood, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(.9f, 1.5f));
    }
    
    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        SpriteEffects direction = 0;
        if (Math.Cos(rotation) < 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += MathHelper.Pi;
        }
        after?.DrawFancyAfterimages(texture, [lightColor], Projectile.Opacity * .5f, Projectile.scale);
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, direction, 0f);
        return false;
    }
}
