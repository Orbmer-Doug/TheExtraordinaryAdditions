using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Cooldowns;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Early;

public class BoneFlintlockHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BoneFlintlock);
    public override int AssociatedItemID => ModContent.ItemType<BoneFlintlock>();
    public override int IntendedProjectileType => ModContent.ProjectileType<BoneFlintlockHeld>();
    public override void Defaults()
    {
        Projectile.width = 46;
        Projectile.height = 22;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public ref float Wait => ref Projectile.ai[0];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        int time = Item.useTime;
        float rot = Animators.Bump(0f, .4f, 1f, .6f).Evaluate(time - Wait, 0f, time / 2, 0f, -.15f);
        Projectile.rotation = Projectile.velocity.ToRotation() + (rot * Dir);
        Projectile.Center = Center + PolarVector(Projectile.width * .55f, Projectile.rotation);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);
        Owner.itemRotation = Projectile.rotation;
        Owner.SetFrontHandBetter(0, Projectile.rotation);

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Wait <= 0 && Owner.HasAmmo(Item))
        {
            Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
            Vector2 pos = Projectile.RotHitbox().Right + PolarVector(8f * Dir, Projectile.rotation - MathHelper.PiOver2);
            Vector2 vel = Projectile.velocity * MathHelper.Clamp(speed, Item.shootSpeed, Item.shootSpeed * 2);
            if (this.RunLocal())
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<CalciumShot>(), dmg, kb, Projectile.owner);

            for (int i = 0; i < 15; i++)
            {
                ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.7f, 1.2f), Main.rand.Next(10, 20),
                    Main.rand.NextFloat(.4f, .5f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(.4f, .5f)), false, true);
                ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 8, Main.rand.NextFloat(.2f, .35f), Color.OrangeRed, 1f);
            }
            SoundID.Item11.Play(pos, Main.rand.NextFloat(.8f, 1.1f), .1f, .1f);

            Wait = time;
        }

        int bomb = ModContent.ProjectileType<CalciumBomb>();
        if ((this.RunLocal() && Modded.SafeMouseRight.Current) && !CalUtils.HasCooldown(Owner, SkullBombCooldown.ID) && Owner.ownedProjectileCounts[bomb] <= 0 && Wait <= 0)
        {
            Projectile.NewProj(Projectile.Center, Center.SafeDirectionTo(Modded.mouseWorld) * 10f, bomb, Projectile.damage * 2, Projectile.knockBack * 2f, Owner.whoAmI);
            SoundID.Item1.Play(Projectile.Center);
            Wait = time;
            CalUtils.AddCooldown(Owner, SkullBombCooldown.ID, SecondsToFrames(1.5f));
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