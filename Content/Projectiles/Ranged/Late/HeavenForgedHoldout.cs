using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class HeavenForgedHoldout : BaseIdleHoldoutProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavenForgedCannon);
    public override int AssociatedItemID => ModContent.ItemType<HeavenForgedCannon>();
    public override int IntendedProjectileType => ModContent.ProjectileType<HeavenForgedHoldout>();
    public ref float Wait => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float Recoil => ref Projectile.ai[2];
    public const int WaitTime = 70;

    public override void Defaults()
    {
        Projectile.Size = new();
        Projectile.DamageType = DamageClass.Ranged;
    }

    public Vector2 Tip => Projectile.Center + PolarVector(51f, Projectile.rotation) + PolarVector(4f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
    public int Dir => Projectile.velocity.X.NonZeroSign();

    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, 8f, Time);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        float anim = new Animators.PiecewiseCurve()
            .Add(0f, -1.1f, .3f, Animators.MakePoly(9f).OutFunction)
            .Add(-1.1f, 0f, 1f, Animators.MakePoly(3f).InOutFunction)
            .Evaluate(InverseLerp(WaitTime, 20f, Wait));
        Projectile.rotation = Projectile.velocity.ToRotation() + (anim * Dir * Owner.gravDir);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        Projectile.Center = Center + PolarVector(35f - Recoil, Projectile.rotation) + PolarVector(10f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait <= 0f && Owner.HasAmmo(Item))
        {
            Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));

            SoundID.Zombie103.Play(Tip, 1f, -.2f, .1f);
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);

            for (int i = 0; i < 10; i++)
                ParticleRegistry.SpawnGlowParticle(Tip, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(2f, 5f), 12, Main.rand.NextFloat(50f, 80f), Color.Cyan, 1.3f);
            for (int i = 0; i < 50; i++)
            {
                float comp = InverseLerp(0f, 50f, i);
                float lerp = Convert01To010(comp);

                int life = (int)(Main.rand.Next(30, 40) * lerp);
                float scale = MathHelper.Lerp(.8f, 1.8f, lerp);
                Color col = Color.DeepSkyBlue.Lerp(Color.Cyan, Main.rand.NextFloat());

                // SHAPESSS
                Vector2 velocity = vel.RotatedBy(MathHelper.Lerp(-.9f, .9f, comp)) * MathHelper.Lerp(2f, 12f, lerp);
                velocity *= MathHelper.Lerp(1f, 2f, Convert01To010(InverseLerp(20f, 30f, i)));
                velocity *= MathHelper.Lerp(1f, 2f, Convert01To010(InverseLerp(0f, 10f, i)));
                velocity *= MathHelper.Lerp(1f, 2f, Convert01To010(InverseLerp(40f, 50f, i)));
                velocity *= Main.rand.NextFloat(.9f, 1.1f);

                ParticleRegistry.SpawnSquishyLightParticle(Tip, velocity, life, scale, col, 1f, .9f, 3.3f);
                ParticleRegistry.SpawnBloomPixelParticle(Tip, velocity * 1.4f, life, scale * .6f, col, Color.DarkCyan, null, 1.8f);
            }

            ScreenShakeSystem.New(new(.22f, .21f), Tip);

            if (this.RunLocal())
                Projectile.NewProj(Tip, vel * 15f, ModContent.ProjectileType<LuminiteRocket>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
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
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, FixedDirection(), 0f);
        return false;
    }
}