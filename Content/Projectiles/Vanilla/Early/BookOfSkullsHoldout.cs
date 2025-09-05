using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class BookOfSkullsHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => ItemID.BookofSkulls.GetTerrariaItem();
    public override int AssociatedItemID => ItemID.BookofSkulls;
    public override int IntendedProjectileType => ModContent.ProjectileType<BookOfSkullsHoldout>();

    public override void Defaults()
    {
        Projectile.width = 28;
        Projectile.height = 32;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Charge => ref Projectile.ai[1];
    private const float ChargeTime = 60f;
    public override void SafeAI()
    {
        Item item = Owner.HeldItem;
        Vector2 pos = Projectile.Center + PolarVector(10f, Projectile.rotation);

        if (Time % item.useAnimation == item.useAnimation - 1 && Modded.SafeMouseLeft.Current && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * item.shootSpeed;
            SoundEngine.PlaySound(SoundID.Item8, pos);
            Projectile.NewProj(pos + vel, vel, ModContent.ProjectileType<HomingSkull>(), item.damage, item.knockBack, Owner.whoAmI);
        }

        if (Modded.SafeMouseRight.Current && Charge < ChargeTime && !Modded.MouseLeft.Current && this.RunLocal())
        {
            Charge++;
            this.Sync();
        }
        else if (Charge > 0f && Charge != ChargeTime)
        {
            Charge--;
            this.Sync();
        }

        ref float indic = ref Projectile.localAI[0];
        if (indic == 0f && Charge == ChargeTime)
        {
            float offsetAngle = RandomRotation();
            int amount = 6;
            for (int i = 0; i < amount; i++)
            {
                Vector2 velo = (MathHelper.TwoPi * i / amount + offsetAngle).ToRotationVector2() * Main.rand.NextFloat(4f, 7f);
                ParticleRegistry.SpawnSparkParticle(pos, velo, Main.rand.Next(16, 22), Main.rand.NextFloat(.6f, .7f), Color.Chocolate);
            }
            indic = 1f;
        }

        if (!Modded.MouseLeft.Current && Charge == ChargeTime && Owner.HeldItem.CheckManaBetter(Owner, item.mana * 2, true) && this.RunLocal())
        {
            int radius = 22;
            for (int i = -radius; i <= radius; i += radius)
            {
                Vector2 rotVel = Projectile.velocity.RotatedBy(MathHelper.ToRadians(i)) * item.shootSpeed * 2;
                Projectile.NewProj(pos, rotVel, ModContent.ProjectileType<BlastSkull>(), item.damage, item.knockBack, Owner.whoAmI, 0f, 1f);
            }
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 1.4f, Pitch = -.2f }, Projectile.Center);
            indic = 0f;
            Charge = 0f;
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.RotatedRelativePoint(Owner.MountedCenter, false, true).SafeDirectionTo(Modded.mouseWorld), 1f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Projectile.Center = Owner.GetFrontHandPositionImproved() - PolarVector(5f, Projectile.rotation);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        SpriteEffects fx = Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Vector2 orig = new(0, tex.Height / 2);
        if (Charge > 0f)
            Projectile.DrawProjectileBackglow(Color.Orange, InverseLerp(0f, ChargeTime, Charge) * 2f, 0, 20, fx, null, null, orig);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, orig, 1, fx);
        return false;
    }
}