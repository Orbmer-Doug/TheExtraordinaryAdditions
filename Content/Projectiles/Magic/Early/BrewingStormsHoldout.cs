using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Early;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class BrewingStormsHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BrewingStorms);
    public override int AssociatedItemID => ModContent.ItemType<BrewingStorms>();
    public override int IntendedProjectileType => ModContent.ProjectileType<BrewingStormsHoldout>();
    public override void Defaults()
    {
        Projectile.width = 34;
        Projectile.height = 38;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref int Charge => ref Owner.GetModPlayer<BrewingStormsPlayer>().Counter;
    public static readonly int ChargeNeeded = SecondsToFrames(15);
    public float Completion => Utils.GetLerpValue(0f, ChargeNeeded, Charge, true);
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Modded.mouseWorld), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Modded.mouseWorld), interpolant);
            if (Projectile.oldVelocity != Projectile.velocity)
                this.Sync();
        }

        // Tie projectile to player
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + Projectile.velocity * Projectile.width * .5f;

        // Update damage dynamically, in case item stats change during the projectile's lifetime
        Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);

        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.direction == -1)
            Projectile.rotation += MathHelper.Pi;

        Owner.ChangeDir(Projectile.direction);
        Owner.heldProj = Projectile.whoAmI;
        Projectile.timeLeft = 2;

        float armPointingDirection = Owner.itemRotation;
        if (Owner.direction < 0)
            armPointingDirection += MathHelper.Pi;
        Owner.SetCompositeArmFront(true, 0, armPointingDirection - MathHelper.PiOver2);
        Owner.SetCompositeArmBack(true, 0, armPointingDirection - MathHelper.PiOver2);

        if (this.RunLocal() && Time % Item.useTime == Item.useTime - 1 && Modded.SafeMouseLeft.Current && Item.CheckManaBetter(Owner, 3, true))
        {
            SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { PitchVariance = .1f }, Projectile.Center);
            for (int i = 0; i <= 1; i++)
            {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(MathHelper.ToRadians(16)) * Item.shootSpeed;
                ParticleRegistry.SpawnLightningArcParticle(Projectile.Center, vel.RotatedByRandom(.25f).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(80f, 120f),
                    Main.rand.Next(18, 22), Main.rand.NextFloat(.3f, .4f), Color.LightPink);

                Projectile.NewProj(Projectile.Center + vel, vel, ModContent.ProjectileType<LightningNimbusSparks>(), Item.damage, Item.knockBack, Owner.whoAmI);
            }
        }

        Charge++;

        if (Main.rand.NextBool(2 + (int)Completion * 5))
        {
            Vector2 vel = Vector2.UnitY * Completion * -Main.rand.NextFloat(3f, 9f);
            ParticleRegistry.SpawnSparkParticle(Projectile.RandAreaInEntity(), vel, Main.rand.Next(18, 24), Main.rand.NextFloat(.4f, .8f) * Completion, Color.LightPink);
        }
        if (this.RunLocal() && Charge >= ChargeNeeded)
        {
            Vector2 pos = Modded.mouseWorld - new Vector2(Main.rand.NextFloat(-200f, 200f), Main.screenHeight);
            Vector2 vel = (Modded.mouseWorld - pos + Projectile.velocity * 7.5f).SafeNormalize(Vector2.UnitY) * 28f;
            Projectile.NewProj(pos, vel, ModContent.ProjectileType<BrewingLightningStrike>(), (int)(Item.damage * 3f), 0f, Projectile.owner, vel.ToRotation(), Main.rand.Next(150), 0f);

            AdditionsSound.LightningStrike.Play(Owner.Center, 1f);
            Charge = 0;
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPos = Projectile.Center - Main.screenPosition;

        Main.EntitySpriteDraw(tex, drawPos, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, tex.Size() * .5f, 1, Projectile.direction.ToSpriteDirection());
        return false;
    }
}

public sealed class BrewingStormsPlayer : ModPlayer
{
    public int Counter;
    public override void UpdateDead() => Counter = 0;
}