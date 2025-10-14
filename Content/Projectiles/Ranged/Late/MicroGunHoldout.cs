using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class MicroGunHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.MicroGun);
    public ref float Timer => ref Projectile.ai[0];
    public ref float Recoil => ref Projectile.ai[1];
    public override void Defaults()
    {
        Projectile.width = 268;
        Projectile.height = 82;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.penetrate = -1;
    }

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
        }
        Projectile.spriteDirection = (Projectile.velocity.X > 0f).ToDirectionInt();
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.spriteDirection == -1)
            Projectile.rotation += MathHelper.Pi;
        Owner.ChangeDir(Projectile.spriteDirection);
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation);

        const float pushBack = 50f;
        float recoil = MathHelper.Clamp(pushBack - Recoil, 0f, Recoil);
        Projectile.Center = Center + Projectile.velocity * (Projectile.width * .43f - recoil);

        if (Timer % 3f == 0f)
        {
            SoundEngine.PlaySound(SoundID.Item38 with { Pitch = 0f, MaxInstances = 0 }, Projectile.Center);
            for (int i = 0; i < 3; i++)
            {
                Vector2 velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(1.9f));
                velocity *= 12f * Main.rand.NextFloat(1f, 1.1f);
                int type = ModContent.ProjectileType<MicroRound>();
                int damage = Projectile.damage;
                float knockback = Item.knockBack;
                Vector2 position = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * Item.width * 0.5f;
                if (this.RunLocal())
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(null), position, velocity, type, damage, knockback, Owner.whoAmI);

                ParticleRegistry.SpawnHeavySmokeParticle(position, velocity.RotatedByRandom(.3f) * .01f, Main.rand.Next(5, 14), Main.rand.NextFloat(.4f, .6f), Color.Chocolate, Main.rand.NextFloat(.7f, 1f));
                ParticleRegistry.SpawnMistParticle(position + Main.rand.NextVector2Circular(4f, 4f), velocity * Main.rand.NextFloat(0.05f, 1.1f), Main.rand.NextFloat(.4f, .8f), Color.OrangeRed, Color.DarkGray, Main.rand.NextByte(100, 220));

                Vector2 vel = new Vector2(Main.rand.NextFloat(4f, 7f) * -Projectile.direction, -Main.rand.NextFloat(5f, 9f)).RotatedBy(Projectile.rotation);
                ParticleRegistry.SpawnBulletCasingParticle(Projectile.Center, vel, 1f);
            }

            Recoil = 20;
            Projectile.position += Utils.NextVector2Circular(Main.rand, 4.5f, 4.5f);
        }

        if (Recoil > 0)
            Recoil -= 4;

        Timer++;
    }
}
