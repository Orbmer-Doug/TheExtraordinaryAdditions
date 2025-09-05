using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class CharringBarrageHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CharringBarrage);
    public override int AssociatedItemID => ModContent.ItemType<CharringBarrage>();
    public override int IntendedProjectileType => ModContent.ProjectileType<CharringBarrageHoldout>();
    public override void Defaults()
    {
        Projectile.width = 94;
        Projectile.height = 36;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public ref float Wait => ref Projectile.ai[0];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        int time = Item.useTime;
        float rot = Animators.Bump(0f, .4f, 1f, .6f).Evaluate(time - Wait, 0f, time / 2, 0f, -.25f);
        float recoil = Animators.Bump(0f, .3f, 1f, .7f).Evaluate(time - Wait, 0f, time, 0f, 4f);
        Projectile.rotation = Projectile.velocity.ToRotation() + (rot * Dir);
        Projectile.Center = Center + PolarVector((Projectile.height * .43f) - recoil, Projectile.rotation);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);
        Owner.itemRotation = Projectile.rotation;
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        if (Wait <= 0 && Owner.HasAmmo(Item) && (this.RunLocal() && Modded.SafeMouseLeft.Current))
        {
            Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
            Vector2 pos = Projectile.RotHitbox().Right + PolarVector(8f * Dir, Projectile.rotation - MathHelper.PiOver2);
            Vector2 vel = Center.SafeDirectionTo(Modded.mouseWorld) * MathHelper.Clamp(speed, Item.shootSpeed, Item.shootSpeed * 2);
            if (this.RunLocal())
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<CharringBlast>(), dmg, kb, Projectile.owner);

            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustPerfect(pos, DustID.Torch, vel.RotatedByRandom(.3f) * Main.rand.NextFloat(.4f, .6f), 0, default, Main.rand.NextFloat(.6f, .9f));
                ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.5f, .8f), Main.rand.Next(20, 30),
                    Main.rand.NextFloat(.4f, .5f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(.4f, .5f)), false, true);

                ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 8, Main.rand.NextFloat(.2f, .35f), Color.OrangeRed, 1f);
            }
            SoundID.Item11.Play(pos, Main.rand.NextFloat(.8f, 1.1f), 0f, .15f);
            
            Wait = time;
            this.Sync();
        }

        if (Wait > 0f)
            Wait--;
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
