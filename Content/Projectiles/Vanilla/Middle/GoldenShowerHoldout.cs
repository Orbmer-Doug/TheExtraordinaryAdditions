using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class GoldenShowerHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.GoldenShower;
    public override int AssociatedItemID => ItemID.GoldenShower;
    public override int IntendedProjectileType => ModContent.ProjectileType<GoldenShowerHoldout>();
    public override void Defaults()
    {
        Projectile.width = 24;
        Projectile.height = 28;
        Projectile.scale = .9f;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Delay => ref Projectile.ai[1];
    public override void SafeAI()
    {
        Item item = Owner.HeldItem;

        if (Modded.SafeMouseLeft.Current && Delay <= 0 && this.RunLocal())
        {
            SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);
            Delay = item.useAnimation;
        }
        if (Delay > 0)
            Delay--;

        Vector2 pos = Projectile.Center + PolarVector(10f, Projectile.rotation);
        if (Delay % item.useTime == item.useTime - 1 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity;

            vel *= item.shootSpeed;

            Projectile.NewProj(pos, vel, ModContent.ProjectileType<IchorStream>(), item.damage, item.knockBack, Owner.whoAmI);
        }

        int wait = item.useAnimation * 2;
        if (Time % wait == wait - 1 && Modded.SafeMouseRight.Current && item.CheckManaBetter(Owner, item.mana, true) && !Modded.MouseLeft.Current && this.RunLocal())
        {
            SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);

            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = -Vector2.UnitY.RotatedByRandom(.36f) * item.shootSpeed * Main.rand.NextFloat(.66f, 1f);
                SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<IchorSwirl>(), item.damage, item.knockBack * 2, Owner.whoAmI, 0f, 1f);
            }
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
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
        Vector2 orig = new(0, tex.Height / 2);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, orig, 1, FixedDirection());
        return false;
    }
}