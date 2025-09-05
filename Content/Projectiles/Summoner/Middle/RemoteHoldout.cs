using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Weapons.Summoner.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class RemoteHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.RemoteHoldout);
    public override int AssociatedItemID => ModContent.ItemType<HiTechRemote>();
    public override int IntendedProjectileType => ModContent.ProjectileType<RemoteHoldout>();
    public override void Defaults()
    {
        Projectile.width = 16;
        Projectile.height = 24;
        Projectile.DamageType = DamageClass.Summon;
    }

    public ref float Wait => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true).SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        int dir = Projectile.velocity.X.NonZeroSign();
        Owner.ChangeDir(dir);
        float rot = MathHelper.PiOver4 + (dir == -1 ? MathHelper.Pi - MathHelper.PiOver2 : 0f);
        float armRot = rot - MathHelper.PiOver2;
        Projectile.rotation = rot;
        Owner.SetCompositeArmBack(true, 0, armRot);
        Owner.SetCompositeArmFront(true, InverseLerp(0f, Item.useTime, Wait).ToStretchAmount(), armRot);
        Projectile.Center = Owner.GetBackHandPositionImproved();
        Owner.heldProj = Projectile.whoAmI;
        Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * .4f);

        // Summon drones
        if (Wait <= 0 && Modded.SafeMouseLeft.Current && Modded.SafeMouseRight.Current && this.RunLocal())
        {
            SoundID.Item44.Play(Projectile.Center, 1f, 0f, .2f);

            Vector2 pos = Projectile.Center - new Vector2(Main.rand.NextFloat(-Main.screenWidth / 3, Main.screenWidth / 3), 800f);
            Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<LazerDrone>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
            for (int i = 0; i < 7; i++)
                ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 25, 0f, Vector2.One, 0f, .05f, Color.SkyBlue);

            Wait = Item.useTime;
        }
        else
        {
            // Holding charge
            if (!Modded.SafeMouseLeft.Current && Modded.SafeMouseRight.Current && this.RunLocal())
                Wait = Item.useTime;

            // Pressing fire button
            if (Wait <= 0 && Modded.SafeMouseLeft.Current && !Modded.SafeMouseRight.Current && this.RunLocal())
                Wait = Item.useTime;
        }

        if (Wait > 0)
            Wait--;

        Time++;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() / 2;
        SpriteEffects direction = SpriteEffects.None;
        if (Math.Cos(rotation) <= 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += MathHelper.Pi;
        }
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, direction, 0f);
        return false;
    }
}
