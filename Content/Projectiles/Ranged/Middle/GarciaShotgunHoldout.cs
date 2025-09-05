using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class GarciaShotgunHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GarciaShotgun);
    public override int AssociatedItemID => ModContent.ItemType<GarciaShotgun>();
    public override int IntendedProjectileType => ModContent.ProjectileType<GarciaShotgunHoldout>();
    public override void Defaults()
    {
        Projectile.width = 96;
        Projectile.height = 22;
        Projectile.DamageType = DamageClass.Ranged;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public ref float Wait => ref Projectile.ai[0];
    public ref int Shells => ref Modded.GarciaOverload;
    public bool Taunting
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Mouse), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Dir);

        int time = Owner.HeldItem.useTime;
        float animProgress = 1f - InverseLerp(0f, time, Wait);

        float shell = Taunting ? .6f : .5f;
        float backInterpol = Convert01To010((animProgress - shell) / 0.36f);

        float rotation = Projectile.velocity.ToRotation() * Owner.gravDir + MathHelper.PiOver2;

        if (!Taunting)
        {
            float amt = ((0.5f - animProgress) / 0.5f).Squared();
            if (animProgress < 0.5)
                rotation += (Shells > 0 ? -1f : -0.45f) * amt * Dir;
        }

        Owner.SetCompositeArmFront(true, 0, rotation - MathHelper.Pi);
        Projectile.rotation = Owner.compositeFrontArm.rotation + MathHelper.PiOver2 * Owner.gravDir;
        Projectile.Center = Center + PolarVector(25f - (backInterpol * 8f), Projectile.rotation);

        if (animProgress == shell)
        {
            if (!Taunting)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    string goreType = "GarciaCartridge";
                    Vector2 pos = Projectile.Center + PolarVector(14f, Projectile.rotation) + PolarVector(8f * Dir, Projectile.rotation - MathHelper.PiOver2);
                    Vector2 vel = Projectile.rotation.ToRotationVector2().SafeNormalize(Vector2.Zero).RotatedBy(2f * -Dir) * Main.rand.NextFloat(2f, 6f);
                    Gore.NewGore(Projectile.GetSource_FromThis(), pos, vel, Mod.Find<ModGore>(goreType).Type);
                }
            }
            else
                Shells += 1;
        }

        if (animProgress > shell)
        {
            float backArmRotation = rotation + 0.52f * Dir;
            Player.CompositeArmStretchAmount stretch = backInterpol.ToStretchAmount();
            Owner.SetCompositeArmBack(true, stretch, backArmRotation - MathHelper.Pi);
        }

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait <= 0 && Owner.HasAmmo(Item))
        {
            Vector2 pos = Projectile.RotHitbox().Right + PolarVector(2f * Dir, Projectile.rotation - MathHelper.PiOver2);
            Owner.PickAmmo(Item, out int type, out float speed, out int dmg, out float kb, out int ammoID, Owner.IsAmmoFreeThisShot(Item, Owner.ChooseAmmo(Item), Owner.ChooseAmmo(Item).type));
            Vector2 vel = Center.SafeDirectionTo(Modded.mouseWorld) * MathHelper.Clamp(speed, Item.shootSpeed, Item.shootSpeed * 2);

            if (this.RunLocal())
            {
                int amount = (Shells + 1) * 8;
                for (int i = 0; i < amount; i++)
                {
                    float reduction = Shells > 0 ? Main.rand.NextFloat(.5f, .8f) : Main.rand.NextFloat(.7f, 1f);
                    Vector2 perturbedSpeed = vel.RotatedByRandom(MathHelper.ToRadians(Shells > 0 ? 10 : 5)) * reduction;
                    Projectile.NewProj(pos, perturbedSpeed, ModContent.ProjectileType<ShotgunBullet>(), dmg, kb, Owner.whoAmI);
                }
            }

            for (int i = 0; i < 20; i++)
            {
                ParticleRegistry.SpawnSparkParticle(pos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(.6f, .8f), Main.rand.Next(20, 30), Main.rand.NextFloat(.7f, .8f), Color.Chocolate.Lerp(Color.OrangeRed, Main.rand.NextFloat(.4f, .5f)), false, true);

                ParticleRegistry.SpawnGlowParticle(pos, Vector2.Zero, 10, Main.rand.NextFloat(.3f, .45f), Color.OrangeRed, 1f);

                ParticleRegistry.SpawnMistParticle(pos, vel.SafeNormalize(Vector2.Zero).RotatedByRandom(.45f) * Main.rand.NextFloat(1f, 4f), Main.rand.NextFloat(.1f, .3f), Color.Chocolate * 1.2f, Color.DarkGray, Main.rand.NextFloat(130f, 160f));
            }

            AdditionsSound.Garciaboom.Play(pos, Shells > 0 ? 1.2f : .9f, Shells > 0 ? -.3f : 0f, .1f, 2);

            // Push back the player
            float playerSpeed = Owner.velocity.Length();
            Vector2 pushback = vel.SafeNormalize(Vector2.UnitX) * (Shells > 0 ? -8f : -4f);
            Vector2 newPlayerVelocity = Owner.velocity + pushback;
            float newPlayerSpeed = ((Vector2)newPlayerVelocity).Length();
            if (playerSpeed < 4f || newPlayerSpeed < playerSpeed)
                Owner.velocity = newPlayerVelocity;
            else
                Owner.velocity = newPlayerVelocity.SafeNormalize(Vector2.UnitX) * playerSpeed;

            ScreenShakeSystem.New(new(Shells > 0 ? .15f : .09f, Shells > 0 ? .14f : .1f), pos);
            Wait = time;
            Shells = 0;
            this.Sync();
        }

        if ((this.RunLocal() && Modded.SafeMouseRight.Current) && Wait <= 0f && !Taunting && Shells <= 0)
        {
            AdditionsSound.Afraid.Play(Owner.Center, .7f);
            Wait = time * 1.8f;
            Taunting = true;
            this.Sync();
        }

        if (Wait <= 0)
        {
            Taunting = false;
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
