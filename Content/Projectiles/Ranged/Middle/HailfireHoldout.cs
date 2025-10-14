using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class HailfireHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<Hailfire>();

    public override int IntendedProjectileType => ModContent.ProjectileType<HailfireHoldout>();

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Hailfire);

    public const int WaitTime = 32;
    public ref float Wait => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float Recoil => ref Projectile.ai[2];

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public Vector2 Tip => Projectile.Center + PolarVector(60f, Projectile.rotation) + PolarVector(6f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, 14f, Time);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        float anim = new Animators.PiecewiseCurve()
            .Add(0f, -.5f, .3f, Animators.MakePoly(6f).OutFunction)
            .Add(-.5f, 0f, 1f, Animators.MakePoly(3f).InOutFunction)
            .Evaluate(InverseLerp(WaitTime, 10f, Wait));
        Projectile.rotation = Projectile.velocity.ToRotation() + (anim * Dir * Owner.gravDir);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        Projectile.Center = Center + PolarVector(22f - Recoil, Projectile.rotation);

        int shell = ModContent.ProjectileType<HailfireShell>();
        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait <= 0f && Owner.ownedProjectileCounts[shell] < 6 && TryUseAmmo(out _, out _, out _, out _, out _))
        {
            SoundID.Item61.Play(Tip, 1.1f, -.1f, .1f);
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);

            for (int i = 0; i < 10; i++)
                ParticleRegistry.SpawnGlowParticle(Tip, Vector2.Zero, 10, Main.rand.NextFloat(20f, 60f), Color.OrangeRed, 1.3f);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnMistParticle(Tip, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(.4f, .8f), Color.OrangeRed, Color.DarkGray, Main.rand.NextFloat(190f, 244f));

            if (this.RunLocal())
                Projectile.NewProj(Tip, vel * 22f, shell, Projectile.damage, Projectile.knockBack, Owner.whoAmI);

            Recoil = 10f;
            Wait = WaitTime;
            this.Sync();
        }
        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.25f, .03f), 0f, 40f);
        if (Wait > 0f)
            Wait--;

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, FixedDirection(), 0f);
        return false;
    }
}