using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class SuperheatedPlasmaArrayHoldout : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SuperheatedPlasmaArray);
    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = 198;
        Projectile.height = 42;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.hide = true;
        Projectile.timeLeft = 90000;
    }

    private const int ChargeUpTime = 120;
    public override void AI()
    {
        bool allowContinuedUse = Time % 12f != 11f || Owner.HeldItem.CheckManaBetter(Owner, Owner.HeldItem.mana, true);
        if (!(Owner.channel && allowContinuedUse) || Owner.noItems || Owner.CCed)
        {
            Projectile.Kill();
            return;
        }

        Vector2 tipOfGun = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * Owner.HeldItem.width * .5f;
        if (Time < ChargeUpTime)
        {
            Vector2 pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.45f) * Main.rand.NextFloat(200f, 260f);
            Vector2 vel = RandomVelocity(3f, 8f, 12f);
            int life = Main.rand.Next(14, 20);
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, Main.rand.NextFloat(.4f, .8f), Color.Orange, Color.OrangeRed, tipOfGun, 1.5f, 9);
        }
        if (Time == ChargeUpTime)
        {
            AdditionsSound.etherealBlazeStart.Play(tipOfGun, 1.2f, .2f);
            for (float i = 1f; i < 1.5f; i += .1f)
                ParticleRegistry.SpawnDetailedBlastParticle(tipOfGun, Vector2.Zero, Vector2.One * 110f * i, Vector2.Zero, (int)(17 * i), Color.Orange.Lerp(Color.OrangeRed, (i * 1.2f) - 1f), 0f, Color.OrangeRed, true);

            if (this.RunLocal())
                Projectile.NewProj(tipOfGun, Vector2.Zero, ModContent.ProjectileType<SuperheatedPlasmaBeam>(), Projectile.damage, Projectile.knockBack, Projectile.owner, Projectile.whoAmI);
        }
        if (Time > ChargeUpTime)
        {
            SoundStyle style = AssetRegistry.GetSound(AdditionsSound.FireBeamLoop);
            slot ??= new LoopedSound(style, new ProjectileAudioTracker(Projectile).IsActiveAndInGame);
            slot.Update(() => Projectile.Center, () => 1.2f, () => 0f);

            if (Main.rand.NextBool())
            {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.48f) * Main.rand.NextFloat(4.3f, 18.7f);
                float size = Main.rand.NextFloat(.5f, .8f);
                ParticleRegistry.SpawnGlowParticle(tipOfGun, vel * 1.2f, Main.rand.Next(10, 20), size * .9f, Color.OrangeRed);
            }
            Lighting.AddLight(tipOfGun, Color.OrangeRed.ToVector3() * 2f);
        }

        AdjustPlayerHoldValues();
        Time++;
    }

    public LoopedSound slot;
    public override void OnKill(int timeLeft)
    {
        if (Time > ChargeUpTime)
            AdditionsSound.FireBeamEnd.Play(Projectile.Center, 1.1f, 0f, .1f);
    }

    public void AdjustPlayerHoldValues()
    {
        Projectile.timeLeft = 2;
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = 2;
        Owner.itemAnimation = 2;
        Owner.itemRotation = 0f;

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, (Owner.Additions().mouseWorld - Owner.MountedCenter).SafeNormalize(Vector2.Zero) * Projectile.Size.Length() * .3f, 0.3f);

            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
        }

        Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.spriteDirection == -1)
            Projectile.rotation += MathHelper.Pi;
        Owner.ChangeDir(Projectile.spriteDirection);

        float frontArmRotation = Projectile.rotation + Owner.direction * -0.4f;
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, frontArmRotation);

        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter) + Projectile.rotation.ToRotationVector2();
    }

    public override bool? CanDamage()
    {
        return false;
    }
}
