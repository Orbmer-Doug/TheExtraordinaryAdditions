using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class EbonyNovaBlasterHeld : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<EbonyNovaBlaster>();

    public override int IntendedProjectileType => ModContent.ProjectileType<EbonyNovaBlasterHeld>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.EbonyNovaBlaster);

    public const int WaitTime = 50;
    public ref float Wait => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float Recoil => ref Projectile.ai[2];

    public override void Defaults()
    {
        Projectile.width = 114;
        Projectile.height = 34;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public Vector2 Tip => Projectile.Center + PolarVector(57f, Projectile.rotation);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, 8f, Time);
        Projectile.timeLeft = 2;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());

        float anim = new Animators.PiecewiseCurve()
            .Add(0f, -1.2f, .5f, Animators.MakePoly(9f).OutFunction)
            .Add(-1.2f, 0f, 1f, Animators.MakePoly(3f).InOutFunction)
            .Evaluate(InverseLerp(WaitTime, 10f, Wait));

        Projectile.rotation = Projectile.velocity.ToRotation() + (anim * Dir * Owner.gravDir);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Mouse), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.Center = Center + PolarVector(38f - Recoil, Projectile.rotation);

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait <= 0f)
        {
            AdditionsSound.ImpSmash.Play(Tip, .6f, 0f, .15f, 2, Name);
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);
            if (this.RunLocal())
                Projectile.NewProj(Tip, vel * 2f, ModContent.ProjectileType<EbonySnipe>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
            Recoil = 20f;
            Wait = WaitTime;
            this.Sync();
        }
        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.25f, .03f), 0f, 20f);
        if (Wait > 0f)
            Wait--;

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects effects = FixedDirection();
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(Color.White), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, effects, 0);
        return false;
    }
}