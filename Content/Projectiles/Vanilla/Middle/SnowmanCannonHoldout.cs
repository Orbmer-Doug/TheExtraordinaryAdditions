using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class SnowmanCannonHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.SnowmanCannon;
    public override int IntendedProjectileType => ModContent.ProjectileType<SnowmanCannonHoldout>();
    public override string Texture => ItemID.SnowmanCannon.GetTerrariaItem();
    public override void Defaults()
    {
        Projectile.width = 58;
        Projectile.height = 32;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public ref float Time => ref Projectile.ai[0];
    private const float Offset = 30f;
    public ref float OffsetLength => ref Projectile.localAI[0];
    public override void SafeAI()
    {
        if (Projectile.localAI[1] == 0f)
        {
            OffsetLength = Offset;
            Projectile.localAI[1] = 1f;
        }

        Vector2 right = Projectile.BaseRotHitbox().Right;

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse) * Projectile.Size.Length(), 0.4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Time % Item.useTime == Item.useTime - 1 && Owner.HasAmmo(Item))
        {
            Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero) * speed;
            int typeOf = type switch
            {
                ProjectileID.RocketSnowmanI => (int)SnowmanRocket.RocketType.One,
                ProjectileID.RocketSnowmanII => (int)SnowmanRocket.RocketType.Two,
                ProjectileID.RocketSnowmanIII => (int)SnowmanRocket.RocketType.Three,
                ProjectileID.RocketSnowmanIV => (int)SnowmanRocket.RocketType.Four,
                ProjectileID.DrySnowmanRocket => (int)SnowmanRocket.RocketType.Dry,
                ProjectileID.WetSnowmanRocket => (int)SnowmanRocket.RocketType.Wet,
                ProjectileID.HoneySnowmanRocket => (int)SnowmanRocket.RocketType.Honey,
                ProjectileID.LavaSnowmanRocket => (int)SnowmanRocket.RocketType.Lava,
                ProjectileID.ClusterSnowmanRocketI => (int)SnowmanRocket.RocketType.Cluster1,
                ProjectileID.ClusterSnowmanRocketII => (int)SnowmanRocket.RocketType.Cluster2,
                ProjectileID.MiniNukeSnowmanRocketI => (int)SnowmanRocket.RocketType.MiniNuke1,
                ProjectileID.MiniNukeSnowmanRocketII => (int)SnowmanRocket.RocketType.MiniNuke2,
                _ => (int)SnowmanRocket.RocketType.One
            };
            Projectile.NewProj(right, vel, ModContent.ProjectileType<SnowmanRocket>(), dmg, kb, Owner.whoAmI, typeOf);

            for (int i = 0; i < 12; i++)
            {
                Vector2 veloc = vel.RotatedByRandom(.5f) * Main.rand.NextFloat(.2f, .5f);
                int life = Main.rand.Next(20, 30);
                float scale = Main.rand.NextFloat(.4f, .8f);
                Color col = Color.DeepSkyBlue.Lerp(Color.BlueViolet, Main.rand.NextFloat());
                ParticleRegistry.SpawnGlowParticle(right, veloc, life, scale * 80f, col, Main.rand.NextFloat(.5f, .8f));
                ParticleRegistry.SpawnDustParticle(right, veloc, life, scale, col, .1f, true, true);
            }

            SoundEngine.PlaySound(SoundID.Item61 with { Volume = .9f, Pitch = .1f }, right);
            OffsetLength = 0f;
            this.Sync();
        }

        if (OffsetLength != Offset)
            OffsetLength = MathHelper.SmoothStep(OffsetLength, Offset, 0.2f);

        Vector2 armPosition = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        Vector2 offset = Projectile.rotation.ToRotationVector2() * OffsetLength;
        Vector2 unitY = Vector2.UnitY;
        double num4 = Projectile.rotation;
        Vector2 armOffset = -Utils.RotatedBy(unitY, num4, default)
            * Utils.Remap(Vector2.Dot(Projectile.rotation.ToRotationVector2(), -Vector2.UnitY), 0f, -1f, 10f, 3f, true)
            * Projectile.direction;

        Projectile.Center = armPosition + offset + armOffset;
        Owner.itemRotation = Utils.ToRotation(Projectile.velocity * Projectile.direction);
        Projectile.spriteDirection = Projectile.direction;
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        SpriteEffects effects = Projectile.direction.ToSpriteDirection();
        float rotation = Projectile.rotation + ((Projectile.spriteDirection == -1) ? ((float)Math.PI) : 0f);
        Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity, rotation, tex.Size() / 2, Projectile.scale, effects, 0f);
        return false;
    }
}