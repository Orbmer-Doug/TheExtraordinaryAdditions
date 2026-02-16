using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class HailfireShell : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HailfireShell);
    public Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 14;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.extraUpdates = 0;
        Projectile.timeLeft = 900;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = 1;
    }

    public bool HitGround
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[5];

    public override void AI()
    {
        if (Projectile.velocity.Y < 16f)
            Projectile.velocity.Y += .5f;

        Projectile.FacingUp();

        after ??= new(7, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0,
            255, 0, 0f, null, false, .4f));

        if (this.RunLocal() && Owner.Additions().SafeMouseRight.JustPressed)
        {
            for (int i = 0; i < 12; i++)
            {
                Vector2 pos = Projectile.RandAreaInEntity();
                Vector2 vel = Projectile.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.5f, 1.4f);
                int type = ModContent.ProjectileType<HailfireRockets>();
                Projectile.NewProj(pos, vel,
                    type, (int)(Projectile.damage * .25f),
                    Projectile.knockBack * .4f, Owner.whoAmI);

                for (int j = 0; j < 5; j++)
                {
                    ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.3f, 1.4f),
                        Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f),
                        Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(0f, .4f)) * 4f, true);
                    
                    ParticleRegistry.SpawnGlowParticle(Projectile.Center, Vector2.Zero, 14, Main.rand.NextFloat(20f, 40f), Color.OrangeRed, 1.4f);
                }

                SoundID.NPCHit4.Play(Projectile.Center, .6f, .3f, .1f);
            }

            Projectile.Kill();
        }
    }

    public override bool? CanHitNPC(NPC target) => !HitGround;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.Center, oldVelocity, Projectile.width, Projectile.height);
        Boom();
        return true;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Boom();
    }

    private void Boom()
    {
        AdditionsSound.crosscodeExplosion.Play(Projectile.Center, .8f, 0f, .1f, 10, Name);
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HailfireExplosion>(),
                Projectile.damage, Projectile.knockBack, Projectile.owner, 1f);
    }

    private FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Color drawCol = Projectile.GetAlpha(lightColor);
        after?.DrawFancyAfterimages(texture, [lightColor]);
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, drawCol, Projectile.rotation,
            texture.Size() * 0.5f, Projectile.scale, direction);

        return false;
    }
}

public class HailfireRockets : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HailfireRocket);

    public override void SetDefaults()
    {
        Projectile.Size = new(8);
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public const int TimeNeeded = 20;

    public override void AI()
    {
        after ??= new(12, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, .8f, Projectile.rotation));

        if (Time >= TimeNeeded)
        {
            Projectile.MaxUpdates = 2;
        }
        Projectile.velocity *= 1.02f;
        Projectile.FacingUp();
        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .45f, -30f, 30f);

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        AdditionsSound.crosscodeExplosion.Play(Projectile.Center, .4f, .3f, .2f, 30, "small");
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<HailfireExplosion>(),
                Projectile.damage, Projectile.knockBack, Projectile.owner, .3f);
    }

    public override bool? CanDamage() => null;

    private FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        after?.DrawFancyAfterimages(tex, [lightColor], InverseLerp(TimeNeeded, TimeNeeded + 40, Time));
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, lightColor * Projectile.Opacity, Projectile.rotation,
            tex.Size() / 2, 1f);
        return false;
    }
}

public class HailfireExplosion : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public ref float RadiusInterpolant => ref Projectile.ai[0];

    public int Time
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public float Radius => 90f * RadiusInterpolant;

    public override void SetDefaults()
    {
        Projectile.width = 1;
        Projectile.height = 1;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 20;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
    }

    public override void AI()
    {
        if (Time == 0)
        {
            for (int i = 0; i < (int)MathHelper.Lerp(30, 100, RadiusInterpolant); i++)
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = (Main.rand.NextVector2Circular(2f, 2f) + Main.rand.NextVector2Circular(12f, 12f)) *
                              RadiusInterpolant;
                Color color = Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(0f, .4f));
                int life = Main.rand.Next(30, 40);
                float scale = Main.rand.NextFloat(.7f, 1.2f) * RadiusInterpolant;
                ParticleRegistry.SpawnGlowParticle(pos, vel * .2f, life / 2, scale * 115f, color.Lerp(Color.White, .3f),
                    .9f);
                
                ParticleRegistry.SpawnSparkParticle(pos, vel * 2f, life / 3, scale * .8f, color);
                
                ParticleRegistry.SpawnSquishyPixelParticle(pos, vel * Main.rand.NextFloat(.5f, .9f), life * 3, scale * 2f, color, Color.Chocolate, 5, false, true);

                if (i % 3 == 2)
                    ParticleRegistry.SpawnCloudParticle(pos, vel * .3f - Vector2.UnitY * Main.rand.NextFloat(0f, 4f), Color.DarkGray, color, life, scale * 50f,
                        Main.rand.NextFloat(.6f, 1.1f));
            }
        }

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);
    }
}