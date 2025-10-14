using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class HeatRayHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => ItemID.HeatRay.GetTerrariaItem();
    public override int AssociatedItemID => ItemID.HeatRay;
    public override int IntendedProjectileType => ModContent.ProjectileType<HeatRayHoldout>();

    public override void Defaults()
    {
        Projectile.width = 36;
        Projectile.height = 24;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        LaserResource.ApplyLaserOverheating(Owner);

        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + PolarVector(15f, Projectile.rotation) + PolarVector(5f * Owner.direction * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

        Vector2 vel = Projectile.velocity * Item.shootSpeed;
        Vector2 pos = Projectile.Center + PolarVector(Projectile.width * .5f, Projectile.rotation);
        LaserResource laser = Owner.GetModPlayer<LaserResource>();

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Time % Item.useTime == Item.useTime - 1 && LaserResource.CanFire(Owner) && TryUseMana(false))
        {
            laser.HeatCurrent++;
            SoundEngine.PlaySound(SoundID.Item12, Projectile.Center);
            Projectile.NewProj(pos, vel, ModContent.ProjectileType<ScorchRay>(), Item.damage, Item.knockBack / 5, Owner.whoAmI);
            for (int i = 0; i < 8; i++)
                ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.1f, .6f), Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .5f), Color.Yellow);
        }

        int wait = Item.useTime * 2;
        if (this.RunLocal() && Modded.SafeMouseRight.Current && Time % wait == wait - 1 && LaserResource.CanFire(Owner))
        {
            if (TryUseMana())
            {
                laser.HeatCurrent += 2;
                SoundEngine.PlaySound(SoundID.Item12 with { Pitch = -.1f, Volume = 1.2f }, Projectile.Center);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<MeltRay>(), Item.damage, Item.knockBack, Owner.whoAmI);
                for (int i = 0; i < 15; i++)
                    ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.1f, .6f), Main.rand.Next(30, 40), Main.rand.NextFloat(.5f, .6f), Color.OrangeRed);
            }
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects effects = FixedDirection();
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, effects, 0);
        return false;
    }
}
