using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.LaserUI;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class HeavyLaserRifleHold : BaseIdleHoldoutProjectile, ILocalizedModType, IModType
{
    public override int AssociatedItemID => ModContent.ItemType<HeavyLaserRifle>();
    public override int IntendedProjectileType => ModContent.ProjectileType<HeavyLaserRifleHold>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HeavyLaserRifle);

    public int Recoil
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float ShootTimer => ref Projectile.ai[1];
    public bool Init
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float Timer => ref Projectile.Additions().ExtraAI[0];

    public const int PlasmaFireTimer = 120;
    public int DrainResource;
    public override void Defaults()
    {
        Projectile.width = 204;
        Projectile.height = 48;
        Projectile.DamageType = DamageClass.Ranged;
        DrainResource = 2;
    }
    public override void SafeAI()
    {
        LaserResource.ApplyLaserOverheating(Owner);

        float dist = 40f;
        float recoil = MathHelper.Clamp(dist - Recoil * 2, 0f, dist);
        if (Recoil > 0)
            Recoil--;

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse) * Projectile.Size.Length(), 0.35f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        Vector2 offset = Utils.ToRotationVector2(Projectile.rotation) * recoil;
        Vector2 armOffset = -Utils.RotatedBy(Vector2.UnitY, Projectile.rotation, default) * Utils.Remap(Vector2.Dot(Utils.ToRotationVector2(Projectile.rotation), -Vector2.UnitY), 0f, -1f, 10f, 3f, true) * Projectile.direction;
        Projectile.Center = Center + offset + armOffset;

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && !Init && LaserResource.CanFire(Owner))
        {
            Init = true;
            this.Sync();
        }
        if (Init == true)
        {
            UpdatePlasmaBeam();
            ShootTimer++;
        }

        Projectile.Opacity = InverseLerp(0f, 10f, Timer);
        Timer++;
    }

    private const float GunLength = 96f;
    private Vector2 ShootPos => Projectile.Center + PolarVector(GunLength, Projectile.rotation) + PolarVector(13f * Projectile.velocity.X.NonZeroSign(), Projectile.rotation - MathHelper.PiOver2);
    public void UpdatePlasmaBeam()
    {
        if (ShootTimer < 40f)
        {
            Vector2 pos = ShootPos + Main.rand.NextVector2Circular(40f, 40f);
            ParticleRegistry.SpawnBloomPixelParticle(pos, RandomVelocity(2f, 3f, 9f), 30,
                Main.rand.NextFloat(.2f, .4f), Color.Orange, Color.Red, ShootPos);
        }
        else if (ShootTimer > 40f)
        {
            if (ShootTimer % 9f == 0f && (ShootTimer - 40f) / 9f <= 6f)
            {
                LaserResource laser = Owner.GetModPlayer<LaserResource>();
                laser.HeatCurrent += DrainResource;

                Vector2 vel = Projectile.rotation.ToRotationVector2();
                AdditionsSound.AstrumDeusLaser.Play(Projectile.Center, 1f, 0f, .2f, 0);

                if (this.RunLocal())
                    Projectile.NewProj(ShootPos, vel, ModContent.ProjectileType<HeavyLaser>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

                ParticleRegistry.SpawnPulseRingParticle(ShootPos, vel, 20, vel.ToRotation(), new(.5f, 1f), 0f, .1f, Color.Red);
                ParticleRegistry.SpawnPulseRingParticle(ShootPos, vel, 10, vel.ToRotation(), new(.5f, 1f), 0f, .2f, Color.DarkRed);
                for (int i = 0; i < 20; i++)
                {
                    ParticleRegistry.SpawnGlowParticle(ShootPos, vel.RotatedByRandom(.35f) * Main.rand.NextFloat(3f, 12f),
                        Main.rand.Next(14, 20), Main.rand.NextFloat(.7f, 1f), Color.Lerp(Color.Red * 1.4f, Color.OrangeRed, Main.rand.NextFloat()));
                }

                Recoil = 6;
                this.Sync();
            }
        }
        if (ShootTimer >= PlasmaFireTimer)
        {
            Init = false;
            ShootTimer = 0f;
            this.Sync();
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() * .5f;
        Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, FixedDirection(), 0f);

        void star()
        {
            Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            float rotation = Projectile.rotation;
            Vector2 drawPosition = ShootPos - Main.screenPosition;
            Vector2 orig = star.Size() * .5f;
            float scale = 1f - Animators.MakePoly(2.5f).InFunction(InverseLerp(0f, 40f, ShootTimer));

            Main.spriteBatch.Draw(star, drawPosition, null, Color.OrangeRed * InverseLerp(0f, 20f, ShootTimer), rotation, orig, new Vector2(.5f, 1f) * scale, 0, 0f);
        }
        PixelationSystem.QueueTextureRenderAction(star, PixelationLayer.Dusts, BlendState.Additive);
        return false;
    }
}