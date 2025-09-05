
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class HeatRayHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.HeatRay;
    public override int AssociatedItemID => ItemID.HeatRay;
    public override int IntendedProjectileType => ModContent.ProjectileType<HeatRayHoldout>();
    public override void Defaults()
    {
        Projectile.width = 36;
        Projectile.height = 24;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        LaserResource.ApplyLaserOverheating(Owner);

        Projectile.extraUpdates = 0;
        Time++;
        UpdatePlayerVisuals();
        Item item = Owner.HeldItem;

        Vector2 vel = Projectile.velocity * item.shootSpeed;
        Vector2 pos = Projectile.Center + PolarVector(Projectile.width * .5f * Projectile.direction, Projectile.rotation);
        var laser = Owner.GetModPlayer<LaserResource>();
        if (Modded.SafeMouseLeft.Current && Time % item.useTime == item.useTime - 1 && LaserResource.CanFire(Owner))
        {
            laser.HeatCurrent++;

            SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);
            Projectile.NewProj(pos, vel, ModContent.ProjectileType<ScorchRay>(), item.damage, item.knockBack / 5, Owner.whoAmI);
        }

        int wait = item.useTime * 2;
        if (Modded.SafeMouseRight.Current && Time % wait == wait - 1 && LaserResource.CanFire(Owner))
        {
            if (item.CheckManaBetter(Owner, item.mana, true))
            {
                laser.HeatCurrent += 2;

                SoundEngine.PlaySound(SoundID.Item12 with { Pitch = -.1f, Volume = 1.2f }, Projectile.Center);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<MeltRay>(), item.damage, item.knockBack, Owner.whoAmI);
            }
        }
    }
    private void UpdatePlayerVisuals()
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Main.MouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Main.MouseWorld), interpolant);
        }

        // Tie projectile to player
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + Projectile.velocity * Projectile.width * .5f;

        // Update damage dynamically, in case item stats change during the projectile's lifetime.
        Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);

        Projectile.rotation = Projectile.AngleTo(Main.MouseWorld);
        if (Projectile.direction == -1)
            Projectile.rotation += MathHelper.Pi;

        Owner.ChangeDir(Projectile.direction);
        Owner.heldProj = Projectile.whoAmI;
        Projectile.timeLeft = 2;
        Owner.itemRotation = WrapAngle90Degrees(Projectile.rotation);

        float armPointingDirection = Owner.itemRotation;
        if (Owner.direction < 0)
        {
            armPointingDirection += MathHelper.Pi;
        }
        Owner.SetCompositeArmFront(true, 0, armPointingDirection - MathHelper.PiOver2);
        Owner.SetCompositeArmBack(true, 0, armPointingDirection - MathHelper.PiOver2);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(tex, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() * .5f, 1, Projectile.direction.ToSpriteDirection());

        return false;
    }
}
