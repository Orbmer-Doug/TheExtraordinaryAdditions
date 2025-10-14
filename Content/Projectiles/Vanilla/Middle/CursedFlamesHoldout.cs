using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class CursedFlamesHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => ItemID.CursedFlames.GetTerrariaItem();
    public override int AssociatedItemID => ItemID.CursedFlames;
    public override int IntendedProjectileType => ModContent.ProjectileType<CursedFlamesHoldout>();
    public override void Defaults()
    {
        Projectile.width = 28;
        Projectile.height = 32;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        Vector2 pos = Projectile.Center + PolarVector(10f, Projectile.rotation);

        if (Time % Item.useAnimation == Item.useAnimation - 1 && Modded.SafeMouseLeft.Current && this.RunLocal() && TryUseMana(false))
        {
            SoundEngine.PlaySound(SoundID.Item20, Projectile.Center);
            Vector2 vel = Projectile.velocity;
            vel *= Item.shootSpeed;
            Projectile.NewProj(pos + vel, vel, ModContent.ProjectileType<RagingCursedFire>(), Item.damage, Item.knockBack, Owner.whoAmI);
        }

        if (this.RunLocal() && Time % Item.useAnimation == Item.useAnimation - 1 && Modded.SafeMouseRight.Current && TryUseMana() && !Modded.MouseLeft.Current)
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Pitch = -.45f, Volume = 1.1f }, Projectile.Center);
            Vector2 vel = Projectile.velocity;
            vel *= Item.shootSpeed * .44f;
            Projectile.NewProj(pos + vel, vel, ModContent.ProjectileType<CursedEruption>(), (int)(Item.damage * 1.42f), (int)(Item.knockBack * 1.1f), Owner.whoAmI);
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
        SpriteEffects fx = Projectile.direction == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Vector2 orig = new(0, tex.Height / 2);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, orig, 1, fx);
        return false;
    }
}
