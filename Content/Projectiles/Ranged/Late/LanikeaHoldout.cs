using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class LanikeaHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<Lanikea>();
    public override int IntendedProjectileType => ModContent.ProjectileType<LanikeaHoldout>();
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Lanikea);

    public const float MaxCharge = 50f;

    public const float ChargeNeeded = 2.5f;
    public ref float Charge => ref Projectile.ai[0];
    public ref float ReloadTimer => ref Projectile.ai[1];
    public ref float Time => ref Projectile.ai[2];
    public int Recoil
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }
    public ref bool Reloading => ref Owner.GetModPlayer<LanikeaPlayer>().Reloading;
    public float ChargeProgress => MathHelper.Clamp(Charge, 0f, MaxCharge) / MaxCharge;
    public float FullChargeProgress => MathHelper.Clamp(Charge, 0f, MaxCharge * ChargeNeeded) / (MaxCharge * ChargeNeeded);
    public float Spread => MathHelper.PiOver2 * (1f - (float)Math.Pow(ChargeProgress, ChargeNeeded) * 0.98f);

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 9;
    }

    public override void Defaults()
    {
        Projectile.width = 104;
        Projectile.height = 32;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public Vector2 Tip => Projectile.Center + PolarVector(32f, Projectile.rotation) + PolarVector(6f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
    public override void SafeAI()
    {
        Projectile.SetAnimation(9, 10);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .3f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation);

        Projectile.Center = Center + PolarVector(40f - Recoil, Projectile.rotation) + PolarVector(10f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);

        if (Charge == (int)(MaxCharge * ChargeNeeded) && Owner.whoAmI == Main.myPlayer)
        {
            AdditionsSound.WeaponFail.Play(Owner.MountedCenter, 1f, 0f, .1f);
        }

        if (FullChargeProgress >= 1f && Main.rand.NextBool())
        {
            Vector2 direction = Owner.MountedCenter.SafeDirectionTo(Modded.mouseWorld);
            ParticleRegistry.SpawnSparkleParticle(Tip, Projectile.velocity.RotatedByRandom(MathHelper.Pi / 4.6f) * Main.rand.NextFloat(3f, 8f), 32, 1f, Color.Beige, Color.Wheat, 1.3f);
        }

        if ((this.RunLocal() && Modded.SafeMouseLeft.JustPressed) && !Reloading)
        {
            HandleFire();
        }

        if (Reloading)
        {
            ReloadTimer++;
            if (Recoil > 0)
                Recoil--;

            if (ReloadTimer == 20f)
            {
                SoundEngine.PlaySound(SoundID.Item149 with { Volume = .7f, MaxInstances = 1 }, Projectile.Center);
            }

            // Stay at 0
            Charge = 0f;
            if (ReloadTimer > 45)
            {
                ReloadTimer = 0;
                Reloading = false;
            }
            this.Sync();
        }
        else
        {
            Charge++;
        }

        Projectile.Opacity = InverseLerp(0f, 6f, Time);
        Time++;
    }

    private void HandleFire()
    {
        Vector2 veloc = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        float velRot = veloc.ToRotation();

        if (FullChargeProgress < 1f)
        {
            AdditionsSound.SniperShot.Play(Tip, .5f + .3f * ChargeProgress, 0f, .2f);

            int amount = 7;

            bool rand = Main.rand.NextBool();
            float val1 = rand ? 15f : 9f;
            float val2 = rand ? 9f : 15f;

            for (int i = 0; i < amount; i++)
            {
                float angleOffset = MathHelper.Lerp(Spread * -0.3f, Spread * 0.3f, i / (amount - 1f));
                Vector2 direction = Utils.ToRotationVector2(velRot + angleOffset);
                int realDamage = Projectile.damage + (int)(100 * Math.Pow(ChargeProgress * 1.5f, 3f));

                float val = MathHelper.Lerp(val1, val2, i / (float)(amount - 1f));
                float speed = (val + 11f * ChargeProgress);
                Vector2 vel = direction * speed;
                int type = ModContent.ProjectileType<VolatileStar>();
                Projectile.NewProj(Tip, vel, type, realDamage / 2, Projectile.knockBack, Owner.whoAmI, 0f, 0f, 0f);

                Vector2 velocity = direction * 10f;
                for (int j = 0; j < 12; j++)
                {
                    Vector2 pos = Tip + Utils.NextVector2Circular(Main.rand, 5f, 5f);
                    float scale = Main.rand.NextFloat(.64f, 1.1f);
                    ParticleRegistry.SpawnSparkParticle(pos, velocity.RotatedByRandom(.45f) * Main.rand.NextFloat(.2f, 2.1f), 90, scale, Color.Wheat);
                }
            }

            Recoil = 6;
        }
        else
        {
            AdditionsSound.Laser3.Play(Projectile.Center, 1.4f);
            Vector2 direction2 = Utils.ToRotationVector2(velRot);
            if (Owner.whoAmI == Main.myPlayer)
            {
                Owner.velocity = Owner.SafeDirectionTo(Projectile.Center) * -8f;
                Vector2 velocity = direction2 * 10f;

                for (int i = 0; i < 27; i++)
                {
                    Vector2 pos = Tip + Utils.NextVector2Circular(Main.rand, 5f, 5f);
                    ParticleRegistry.SpawnMistParticle(pos, velocity.RotatedByRandom(.55f) * Main.rand.NextFloat(.4f, 4.2f), Main.rand.NextFloat(.9f, 2.1f), Color.BlueViolet, Color.SlateBlue, 180);
                }

                for (int i = 0; i < 8; i++)
                {
                    int type = ModContent.ProjectileType<CosmicSlugCharge>();
                    int damage = Projectile.damage;
                    Vector2 FinalVelocity = velocity.RotatedByRandom(.36f) * Main.rand.NextFloat(.5f, .7f);
                    Projectile.NewProj(Tip, FinalVelocity, type, damage, 6f, Owner.whoAmI, 0f, 0f, 0f);
                }
                Projectile.NewProj(Projectile.Center, new Vector2(5 * -Projectile.direction, -5f), ModContent.ProjectileType<GalaxyShell>(), 0, 0f, -1, 0f, 0f, 0f);
            }

            Color pulseColor2 = (Utils.NextBool(Main.rand) ? Color.BlueViolet : Color.SlateGray);
            ParticleRegistry.SpawnPulseRingParticle(Tip, direction2 * 5, 40, direction2.ToRotation(), new(.5f, 1f), 160f, 0f, pulseColor2);
            Recoil = 12;
        }

        Reloading = true;
        Charge = 0f;
        this.Sync();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float blinkage = 0f;
        if (Charge >= MaxCharge * ChargeNeeded)
            blinkage = (float)Math.Sin(MathHelper.Clamp((Charge - MaxCharge * ChargeNeeded) / 15f, 0f, 1f) * MathHelper.Pi);

        ManagedShader effect = ShaderRegistry.SpreadTelegraph;
        effect.TrySetParameter("centerOpacity", 0.7f);
        effect.TrySetParameter("mainOpacity", (float)Math.Sqrt(ChargeProgress) * 2);
        effect.TrySetParameter("halfSpreadAngle", Spread / 3f);
        effect.TrySetParameter("edgeColor", Color.Lerp(Color.SlateBlue, Color.BlueViolet, blinkage).ToVector3());

        effect.TrySetParameter("centerColor", Color.Lerp(Color.MediumSlateBlue, Color.BlueViolet, blinkage).ToVector3());
        effect.TrySetParameter("edgeBlendLength", 0.09f);
        effect.TrySetParameter("edgeBlendStrength", 13f);

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, effect.Effect);
        Texture2D invis = AssetRegistry.InvisTex;
        Main.EntitySpriteDraw(invis, Tip - Main.screenPosition, null, Color.White, Projectile.rotation, new Vector2(invis.Width / 2f, invis.Height / 2f), 700f, 0, 0f);
        Main.spriteBatch.ExitShaderRegion();

        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, FixedDirection(), 0);
        return false;
    }
}

public class LanikeaPlayer : ModPlayer
{
    public bool Reloading;
}