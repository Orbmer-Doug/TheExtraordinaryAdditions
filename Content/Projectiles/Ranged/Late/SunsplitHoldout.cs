using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class SunsplitHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SunsplitHorizon);
    public override int AssociatedItemID => ModContent.ItemType<SunsplitHorizon>();
    public override int IntendedProjectileType => ModContent.ProjectileType<SunsplitHoldout>();
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 4;
    }
    public override void Defaults()
    {
        Projectile.Size = new(176, 58);
        Projectile.DamageType = DamageClass.Ranged;
    }

    public SlotId Slot;
    public ref float Time => ref Projectile.ai[0];
    public ref float ShootTime => ref Projectile.ai[1];
    public Vector2 Tip => Projectile.Center + PolarVector(Projectile.width / 2, Projectile.rotation) + PolarVector(9f * Dir * Owner.gravDir, Projectile.rotation + MathHelper.PiOver2);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void PostAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Dir);
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Projectile.Center = Center + PolarVector(62f, Projectile.rotation) + PolarVector(20f * Dir * Owner.gravDir, Projectile.rotation + MathHelper.PiOver2);
        Projectile.SetAnimation(4, 6, true);

        Vector3 col = new Color(255, 153, 0).ToVector3();
        Lighting.AddLight(Projectile.Center - PolarVector(28f, Projectile.rotation) + PolarVector(16f * Dir * Owner.gravDir, Projectile.rotation + MathHelper.PiOver2), col);
        Lighting.AddLight(Projectile.Center + PolarVector(68f, Projectile.rotation) + PolarVector(9f * Dir * Owner.gravDir, Projectile.rotation + MathHelper.PiOver2), col * .4f);

        if (this.RunLocal() && Modded.SafeMouseLeft.Current)
        {
            if (SoundEngine.TryGetActiveSound(Slot, out var t) && t.IsPlaying)
                t.Position = Projectile.Center;
            else
                Slot = AdditionsSound.FireBeamLoop.Play(Projectile.Center, .6f, .2f);

            ShootTime++;
        }
        else
        {
            if (ShootTime > 0f)
            {
                if (SoundEngine.TryGetActiveSound(Slot, out var t) && t.IsPlaying)
                    t.Stop();

                AdditionsSound.FireBeamEnd.Play(Projectile.Center, 1f, 0f, 0f, 1, Name);
                ShootTime = 0f;
            }
        }

        if (ShootTime % 3 == 2)
        {
            if (this.RunLocal())
                Projectile.NewProj(Tip, Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f, ModContent.ProjectileType<IonizedPlasma>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
        }

        Projectile.Opacity = InverseLerp(0f, 8f, Time);
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 origin = frame.Size() * .5f;
        Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, FixedDirection(), 0f);
        return false;
    }
}