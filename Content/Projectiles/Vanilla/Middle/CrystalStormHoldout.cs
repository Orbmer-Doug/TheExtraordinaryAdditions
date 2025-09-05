using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class CrystalStormHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalStorm;
    public override int AssociatedItemID => ItemID.CrystalStorm;
    public override int IntendedProjectileType => ModContent.ProjectileType<CrystalStormHoldout>();
    public override void Defaults()
    {
        Projectile.width = 24;
        Projectile.height = 28;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];
    private const float ChargeTime = 45f;

    public readonly Color bookCrystal = new(41, 143, 166);
    public readonly Color bookRed = new(207, 42, 52);
    public override void SafeAI()
    {
        Vector2 pos = Projectile.Center + PolarVector(10f, Projectile.rotation);
        int shard = ModContent.ProjectileType<CrystalShard>();
        if (Time % Item.useAnimation == Item.useAnimation - 1 && Modded.SafeMouseLeft.Current && this.RunLocal())
        {
            SoundID.Item9.Play(pos);
            Vector2 vel = Projectile.velocity.RotatedByRandom(.25f) * Item.shootSpeed;

            Projectile.NewProj(pos, vel, shard, Item.damage, Item.knockBack, Owner.whoAmI);
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