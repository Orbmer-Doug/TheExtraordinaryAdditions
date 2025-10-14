using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class LazerDrone : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LazerDrone);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 9;

        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 2;
        ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
        ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;

        Main.projPet[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 44;
        Projectile.height = 28;
        Projectile.netImportant = true;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.minionSlots = 1f;
        Projectile.timeLeft *= 5;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.minion = true;
        Projectile.DamageType = DamageClass.Summon;
    }
    public ref float Time => ref Projectile.ai[0];
    public ref float Charge => ref Projectile.ai[1];
    public int Wait
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }
    public const int FireWait = 30;
    public float ChargeCompletion => InverseLerp(0f, 120f, Charge);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public NPC Target;
    public Vector2 Tip;
    public bool RemoteBeingHeld => Owner.ownedProjectileCounts[ModContent.ProjectileType<RemoteHoldout>()] > 0;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        if (!Owner.Available() && this.RunLocal())
        {
            Modded.Minion[GlobalPlayer.AdditionsMinion.LaserDrones] = false;
            return;
        }

        after ??= new(3, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation,
            0, 90, 3, 2f, Projectile.ThisProjectileTexture().Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame)));

        Owner.AddBuff(ModContent.BuffType<LaserDrones>(), 3600);
        if (Modded.Minion[GlobalPlayer.AdditionsMinion.LaserDrones])
            Projectile.timeLeft = 2;

        if (this.RunLocal())
        {
            Projectile.rotation = Projectile.Center.SafeDirectionTo(Modded.mouseWorld).ToRotation();
            if (Projectile.rotation != Projectile.oldRot[1])
                this.Sync();
        }

        Projectile.SetAnimation(9, 3);
        Projectile.Opacity = InverseLerp(0f, 15f, Time);

        if (!RemoteBeingHeld)
        {
            Target = NPCTargeting.GetClosestNPC(new(Projectile.Center, 800, true, false));
            if (Target.CanHomeInto())
                Projectile.rotation = Projectile.Center.SafeDirectionTo(Target.Center).ToRotation();
            if (Projectile.rotation != Projectile.oldRot[1])
                this.Sync();
        }

        Vector2 vel = Projectile.rotation.ToRotationVector2();
        Vector2 tip = Projectile.Center + PolarVector(18f, Projectile.rotation) + PolarVector(10f * vel.X.NonZeroSign(), Projectile.rotation + MathHelper.PiOver2);
        Tip = tip;
        bool notSummoning = !(Modded.SafeMouseLeft.Current && Modded.SafeMouseRight.Current) && this.RunLocal();
        if (RemoteBeingHeld)
        {
            if (Modded.SafeMouseRight.Current && notSummoning && this.RunLocal())
            {
                int frequency = MultiLerp(ChargeCompletion, 5, 4, 2);
                if (Time % frequency == frequency - 1)
                    ParticleRegistry.SpawnSparkleParticle(tip, -vel.RotatedByRandom(.3f) * Main.rand.NextFloat(2f, 8f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f), Color.Cyan, Color.White, ChargeCompletion * 1.6f);
                Charge++;
            }
        }

        Lighting.AddLight(tip, Color.Cyan.ToVector3() * ChargeCompletion);
        if (((RemoteBeingHeld && ((Modded.SafeMouseLeft.Current && Charge == 0f) ||
            (!Modded.SafeMouseRight.Current && Charge > 0f))) || (!RemoteBeingHeld && Target != null)) && Wait <= 0 && Projectile.Opacity >= 1f && notSummoning && this.RunLocal())
        {
            float power = MathHelper.Clamp(ChargeCompletion * 2.5f, 1f, 2.5f);
            int dmg = (int)(Projectile.damage * power);
            float kb = Projectile.knockBack * power;
            Projectile.NewProj(tip, vel, ModContent.ProjectileType<DroneLaser>(), dmg, kb, Owner.whoAmI, 0f, 0f, ChargeCompletion);
            for (int i = 0; i < 20 + (20 * ChargeCompletion); i++)
            {
                ParticleRegistry.SpawnGlowParticle(tip, Vector2.Zero, 9, .4f + (.4f * ChargeCompletion), Color.LightCyan, 1f);
                ParticleRegistry.SpawnSquishyPixelParticle(tip, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(2f, 10f), Main.rand.Next(80, 120), Main.rand.NextFloat(.8f, 1.2f), Color.Cyan, Color.LightCyan, 4, false);
            }
            ScreenShakeSystem.New(new(.09f + (.2f * ChargeCompletion), .1f + (.3f * ChargeCompletion), 512f + (400f * ChargeCompletion)), tip);

            Projectile.velocity -= vel * (9f + (10f * ChargeCompletion));

            if (ChargeCompletion >= 1f)
            {
                AdditionsSound.LaserTwo.Play(tip, 1f, -.1f, .2f);
                Projectile.Kill();
            }
            else
                AdditionsSound.Laser4.Play(tip, .6f, 0f, .3f);

            Wait = FireWait;
            Charge = 0f;
        }

        if (this.RunLocal())
        {
            Projectile.AI_GetMyGroupIndex(out var index, out var total);

            float time = SecondsToFrames(5f);
            float cycle = Modded.GlobalTimer % time / time * MathF.Tau;
            float offset = MathF.Tau * InverseLerp(0f, total, index);
            Vector2 dest = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + GetPointOnRotatedEllipse(300f, 110f, offset + cycle, cycle);
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.Center.SafeDirectionTo(dest) * MathHelper.Min(Projectile.Center.Distance(dest), 20f), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        if (Wait > 0)
            Wait--;

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        if (ChargeCompletion >= 1f)
        {
            for (int i = 0; i < 30; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(20f, 20f);
                ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel, Main.rand.Next(34, 46), Main.rand.NextFloat(.5f, 1.1f), Color.Cyan, true, true);
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, Main.rand.Next(24, 30), Main.rand.NextFloat(.2f, .7f), Color.DarkCyan, 1f, true);
            }

            if (!Main.dedServ)
            {
                // Blast off pieces
                int barrel = Mod.Find<ModGore>($"LaserDroneGore{1}").Type;
                int rotar = Mod.Find<ModGore>($"LaserDroneGore{2}").Type;
                int metal = Mod.Find<ModGore>($"LaserDroneGore{3}").Type;

                float offsetAngle = Main.rand.NextFloat(MathHelper.TwoPi);

                Vector2 shootVelocity = (MathHelper.TwoPi * .1f + offsetAngle).ToRotationVector2() * 9f;
                Gore.NewGore(Projectile.GetSource_Death(null), Projectile.Center, shootVelocity, barrel);

                for (int i = 0; i < 4; i++)
                {
                    shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 9f;
                    Gore.NewGore(Projectile.GetSource_Death(null), Projectile.Center, shootVelocity, rotar);
                }
                for (int i = 0; i < 2; i++)
                {
                    shootVelocity = (MathHelper.TwoPi * i / 10f + offsetAngle).ToRotationVector2() * 9f;
                    Gore.NewGore(Projectile.GetSource_Death(null), Projectile.Center, shootVelocity, metal);
                }
            }
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = frame.Size() / 2;
        SpriteEffects direction = SpriteEffects.None;
        if (Math.Cos(rotation) <= 0.0)
        {
            direction = SpriteEffects.FlipHorizontally;
            rotation += MathHelper.Pi;
        }

        after?.DrawFancyAfterimages(texture, [Color.Cyan]);
        Main.spriteBatch.Draw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, direction, 0f);

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
        Vector2 orig = bloom.Size() / 2;
        Rectangle target = ToTarget(Tip, new(40f));
        Main.spriteBatch.Draw(bloom, target, null, Color.DarkCyan * ChargeCompletion * 1.25f, .75f, orig, 0, 0f);
        Main.spriteBatch.Draw(bloom, target, null, Color.Cyan * ChargeCompletion, 1f, orig, 0, 0f);
        Main.spriteBatch.Draw(bloom, target, null, Color.LightCyan * ChargeCompletion * .75f, 1.25f, orig, 0, 0f);
        Main.spriteBatch.Draw(bloom, target, null, Color.White * ChargeCompletion * .7f, 1.55f, orig, 0, 0f);
        Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        return false;
    }
}