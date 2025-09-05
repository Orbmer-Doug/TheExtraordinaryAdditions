
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class MatterPincerHoldout : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CosmicImplosion);
    private const int FramesPerFireRateIncrease = 36;
    private Player Owner => Main.player[Projectile.owner];

    private bool OwnerCanShoot
    {
        get
        {
            if (Owner.channel && Owner.HasAmmo(Owner.HeldItem) && !Owner.noItems)
            {
                return !Owner.CCed;
            }
            return false;
        }
    }

    private ref float DeployedFrames => ref Projectile.ai[0];

    private ref float AnimationRate => ref Projectile.ai[1];

    private ref float LastShootAttemptTime => ref Projectile.localAI[0];

    private ref float LastAnimationTime => ref Projectile.localAI[1];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 1;
    }

    public override void SetDefaults()
    {
        Projectile.width = 64;
        Projectile.height = 156;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override void AI()
    {
        Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        Vector2 gunBarrelPos = armPosition + Projectile.velocity * Projectile.height * 0.4f;
        if (!OwnerCanShoot)
        {
            Projectile.Kill();
            return;
        }
        if (DeployedFrames <= 60f)
        {
            SoundStyle reload = SoundID.Item149;
            SoundStyle dD2_DarkMageCastHeal2 = SoundID.DD2_DarkMageCastHeal;
            //((SoundStyle)(dD2_DarkMageCastHeal)).Volume = ((SoundStyle)(dD2_DarkMageCastHeal2)).Volume * 1.5f;
            SoundEngine.PlaySound(reload, (Vector2?)Projectile.Center, null);
        }
        Item weaponItem = Owner.HeldItem;
        Projectile.damage = weaponItem != null ? Owner.GetWeaponDamage(weaponItem, false) : 0;
        int itemUseTime = weaponItem?.useAnimation ?? 36;
        int framesPerShot = itemUseTime * 1;
        DeployedFrames += 1f;
        AnimationRate = DeployedFrames >= itemUseTime ? 2f : MathHelper.Lerp(7f, 2f, DeployedFrames / itemUseTime);
        if (DeployedFrames - LastAnimationTime >= AnimationRate)
        {
            LastAnimationTime = DeployedFrames;
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
        }
        if (DeployedFrames - LastShootAttemptTime >= framesPerShot)
        {
            LastShootAttemptTime = DeployedFrames;
            bool actuallyShoot = DeployedFrames >= itemUseTime;
            if (actuallyShoot)
            {
            }
            int projID = 1;
            float shootSpeed = weaponItem.shootSpeed;
            Vector2 val = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 shootVelocity = val * shootSpeed;
            float waveSideOffset = Main.rand.NextFloat(18f, 28f);
            Vector2 perp = val.RotatedBy(-1.5707963705062866, default) * waveSideOffset;
            float dustInaccuracy = 0.045f;
            {
                Vector2 laserStartPos = gunBarrelPos;
                Vector2 dustOnlySpread = Main.rand.NextVector2Circular(shootSpeed, shootSpeed);
                Vector2 dustVelocity = shootVelocity + dustInaccuracy * dustOnlySpread;
                if (actuallyShoot)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(null), laserStartPos, shootVelocity, projID, Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f, 0f).localAI[1] = 1 * 0.5f;
                }
                SpawnFiringDust(gunBarrelPos, dustVelocity);
            }
        }
        UpdateProjectileHeldVariables(armPosition);
        ManipulatePlayerVariables();
    }

    private void UpdateProjectileHeldVariables(Vector2 armPosition)
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 25f, Projectile.Distance(Main.MouseWorld), true);
            Vector2 oldVelocity = Projectile.velocity;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, ((Entity)(object)Projectile).SafeDirectionTo(Main.MouseWorld), interpolant);
            if (Projectile.velocity != oldVelocity)
            {
                Projectile.netSpam = 0;
                Projectile.netUpdate = true;
            }
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Projectile.Center = armPosition + Projectile.velocity.SafeNormalize(Vector2.UnitX * Owner.direction) * 50f;
        Projectile.spriteDirection = Projectile.direction;
        Projectile.timeLeft = 2;
    }

    private void ManipulatePlayerVariables()
    {
        Owner.ChangeDir(Projectile.direction);
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = 2;
        Owner.itemAnimation = 2;
        Owner.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
    }

    private void SpawnFiringDust(Vector2 gunBarrelPos, Vector2 laserVelocity)
    {
        int dustID = DustID.Vortex;
        int dustRadius = 12;
        float dustRandomness = 11f;
        int dustDiameter = 2 * dustRadius;
        Vector2 dustCorner = gunBarrelPos - Vector2.One * dustRadius;
        for (int i = 0; i < 50; i++)
        {
            Vector2 dustVel = laserVelocity + Main.rand.NextVector2Circular(dustRandomness, dustRandomness);
            Dust obj = Dust.NewDustDirect(dustCorner, dustDiameter, dustDiameter, dustID, dustVel.X, dustVel.Y, 0, default, 1f);
            obj.velocity *= 1.18f;
            obj.noGravity = true;
            obj.scale = 1.6f;
        }
    }

    public override bool? CanDamage()
    {
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return true;
    }
}
