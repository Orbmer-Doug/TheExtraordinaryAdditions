using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class CometStormHoldout : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CometStormHoldout);
    public Player Owner => Main.player[Projectile.owner];

    public ref float Time => ref Projectile.ai[0];

    public override void SetDefaults()
    {
        Projectile.width = 102;
        Projectile.height = 166;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
        Projectile.penetrate = -1;
    }

    public Vector2 center => Owner.RotatedRelativePoint(Owner.MountedCenter);
    public override void AI()
    {
        Item heldItem = Owner.HeldItem;

        // Die if no longer holding the click button or otherwise cannot use the item.
        if (!Owner.channel || Owner.dead || !Owner.active || Owner.noItems || Owner.CCed || heldItem is null)
        {
            Projectile.Kill();
            return;
        }
        if (Owner.statMana <= 0)
        {
            Owner.statMana = 0;
            Projectile.Kill();
            return;
        }

        AdjustPlayerValues();

        // Update damage dynamically, in case item stats change during the projectile's lifetime.
        Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);

        Vector2 top = Projectile.Center + PolarVector(Projectile.height / 2, Projectile.velocity.ToRotation()) + PolarVector(16f, Projectile.velocity.ToRotation() - MathHelper.PiOver2);

        // Release comets
        if (this.RunLocal() && Time % 7f == 6f && Owner.HeldItem.CheckManaBetter(Owner, 3, true))
        {
            Vector2 mouse = Owner.Additions().mouseWorld;
            int type = ModContent.ProjectileType<Comet>();

            const float speed = 22f;
            SoundEngine.PlaySound(SoundID.Item88 with { MaxInstances = 0 }, Projectile.Center);

            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Owner.Center - new Vector2(Main.rand.NextFloat(-Main.screenWidth / 4, Main.screenWidth / 4), 800f + (Main.rand.NextFloat(20f, 50) * i));
                Vector2 velocity = mouse - position;

                if (velocity.Y < 0f)
                {
                    velocity.Y *= -1f;
                }

                if (velocity.Y < 20f)
                {
                    velocity.Y = 20f;
                }

                velocity.Normalize();
                velocity *= speed;
                velocity.Y += Main.rand.NextFloat(-.1f, .1f);
                velocity.X += Main.rand.NextFloat(-2f, 2f);
                Projectile.NewProj(position, velocity, type, Projectile.damage, 0f, Owner.whoAmI);

                for (int a = 0; a < 8; a++)
                {
                    Color col = Color.Lerp(Color.DeepSkyBlue, Color.LightCyan, Main.rand.NextFloat(.2f, .9f));
                    ParticleRegistry.SpawnSquishyLightParticle(position, velocity.RotatedByRandom(.39f) * Main.rand.NextFloat(.8f, 1.7f),
                        Main.rand.Next(25, 35), Main.rand.NextFloat(.7f, 1.2f), col, Main.rand.NextFloat(1f, 1.4f));
                }
            }
            ParticleRegistry.SpawnPulseRingParticle(top, Vector2.Zero, 20, RandomRotation(), new(Main.rand.NextFloat(.3f, .7f), 1f), 0f, Main.rand.NextFloat(.1f, .2f), Color.Cyan);
        }

        ParticleRegistry.SpawnBloomPixelParticle(top + Main.rand.NextVector2Circular(100f, 100f), RandomVelocity(2f, 4f, 10f), Main.rand.Next(18, 24),
            Main.rand.NextFloat(.4f, .8f), Color.Cyan, Color.DarkCyan * .6f, top, 1.2f, 8);

        Projectile.Opacity = InverseLerp(0f, 12f, Time);
        Time++;
    }

    public void AdjustPlayerValues()
    {
        Projectile.timeLeft = 2;
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = 2;
        Owner.itemAnimation = 2;
        Owner.itemRotation = (Projectile.direction * Projectile.velocity).ToRotation();

        // Aim towards the mouse.
        Vector2 mouse = Owner.Additions().mouseWorld;
        if (this.RunLocal())
        {
            float aimInterpolant = InverseLerp(0f, 100f, mouse.Distance(center));
            Projectile.velocity = Projectile.velocity.Lerp(center.SafeDirectionTo(mouse), aimInterpolant * .3f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.Center = center + PolarVector(Projectile.width * .4f, Projectile.velocity.ToRotation()) + PolarVector(27f, Projectile.velocity.ToRotation());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(63.4f);

        Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();

        Owner.ChangeDir(Projectile.spriteDirection);

        float armPointingDirection = Owner.itemRotation;
        if (Owner.direction < 0)
        {
            armPointingDirection += (float)Math.PI;
        }
        Owner.SetCompositeArmFront(true, 0, armPointingDirection - MathHelper.PiOver2);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);

        return false;
    }
    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
}
