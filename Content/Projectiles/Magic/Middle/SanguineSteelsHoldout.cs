using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using ParticleRegistry = TheExtraordinaryAdditions.Common.Particles.Particle.ParticleRegistry;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class SanguineSteelsHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LanceOfSanguineSteels);
    public ref float Time => ref Projectile.ai[0];

    public int Wait
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public ref float Rot => ref Projectile.ai[2];
    public ref float Fade => ref Projectile.AdditionsInfo().ExtraAI[0];

    public int BoltWait
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[1];
        set => Projectile.AdditionsInfo().ExtraAI[1] = value;
    }

    public const int FadeIn = 30;
    public float PortalSize = 150f;
    public float ExtraPortalSize = 60f;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 5;
    }

    public override void Defaults()
    {
        Projectile.Size = new(70f);
        Projectile.friendly = Projectile.ignoreWater = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void SafeAI()
    {
        if (this.RunLocal() && (!Modded.MouseLeft.Current || Fade > 0f))
        {
            Fade++;
            if (Fade > 40f)
                Projectile.Kill();
        }

        Projectile.Opacity = InverseLerp(0f, FadeIn, Time) * InverseLerp(40f, 0f, Fade);
        if (this.RunLocal())
        {
            Projectile.velocity =
                Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld), .4f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Projectile.Center = Center + PolarVector(32f, Projectile.rotation - MathHelper.PiOver4);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetDummyItemTime(2);
        Projectile.timeLeft = 100;
        Projectile.SetAnimation(5, 5);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.ThreeQuarters,
            Projectile.rotation - MathHelper.PiOver4);

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Time > FadeIn && Fade <= 0f)
        {
            if (Wait <= 0f && TryUseMana())
            {
                Vector2 pos = Projectile.Center;
                Vector2 vel = pos.SafeDirectionTo((Center + Projectile.velocity * 100f));
                Projectile.NewProj(pos, vel * 10f, ModContent.ProjectileType<SanguineLance>(), Projectile.damage * 4,
                    Projectile.knockBack, Projectile.owner);

                for (int i = 0; i < 20; i++)
                {
                    ParticleRegistry.SpawnHeavySmokeParticle(pos,
                        vel.RotatedByRandom(.2f) * Main.rand.NextFloat(4f, 8f),
                        Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .7f), Color.DarkRed,
                        Main.rand.NextFloat(.2f, .3f));
                }

                SoundID.Item60.Play(pos, .6f, -.1f, 0f, null, 20, Name);

                Wait = 60;
            }

            if (BoltWait <= 0 && Item.CheckManaBetter(Owner, 2, true))
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 pos = Projectile.Center + PolarVector(PortalSize / 4 + ExtraPortalSize,
                        Utils.Remap(i, 0, 3, 0f, MathHelper.TwoPi) + Rot);
                    Vector2 vel = pos.SafeDirectionTo(Modded.MouseWorld) * 12f;
                    Projectile.NewProj(pos, vel, ModContent.ProjectileType<VermillionDart>(),
                        (int) (Projectile.damage / 4f), 0f,
                        Owner.whoAmI);
                }

                BoltWait = 25;
            }
        }

        if (Wait > 0f)
            Wait--;
        if (BoltWait > 0f)
            BoltWait--;

        Time++;
        Rot += .02f;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float rot = Main.GameUpdateCount / 40f;
        ManagedShader effect = ShaderRegistry.MagicRing;
        effect.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.VoronoiShapes2), 1, SamplerState.LinearWrap);
        effect.TrySetParameter("firstCol", Color.DarkRed.ToVector3());
        effect.TrySetParameter("secondCol", Color.BlueViolet.ToVector3());
        effect.TrySetParameter("time", rot);
        effect.TrySetParameter("cosine", (float) Math.Cos(rot));
        effect.TrySetParameter("opacity", 1.5f);

        Main.spriteBatch.EnterShaderRegion(effect.Effect, BlendState.Additive);
        Texture2D portal = AssetRegistry.GetTexture(AdditionsTexture.UnfathomablePortal);

        Main.spriteBatch.DrawBetterRect(portal,
            ToTarget(Projectile.Center,
                new Vector2(PortalSize)),
            null, Color.Crimson * Projectile.Opacity, Projectile.rotation, portal.Size() / 2);

        for (int i = 0; i < 3; i++)
        {
            float extra = Utils.Remap(i, 0, 3, 0f, MathHelper.TwoPi);
            Vector2 pos = Projectile.Center + PolarVector(PortalSize / 4 + ExtraPortalSize,
                extra + Rot);

            Main.spriteBatch.DrawBetterRect(portal,
                ToTarget(pos,
                    new Vector2(ExtraPortalSize)),
                null, Color.DarkRed * Projectile.Opacity, -Projectile.rotation - extra, portal.Size() / 2);
        }

        Main.spriteBatch.ResetToDefault();

        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}

public class VermillionDart : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 24;
        Projectile.friendly = true;
        Projectile.ignoreWater = Projectile.hostile = Projectile.tileCollide = false;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 3;
        Projectile.timeLeft = 1000;
    }

    public ref float Time => ref Projectile.ai[0];

    public GlobalPlayer Modded => Main.player[Projectile.owner].Additions();

    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center,
                Projectile.velocity.SafeNormalize(Vector2.Zero), 35, Projectile.velocity.ToRotation(),
                new(.3f, 1.1f), 0f, 50f, Color.Crimson);
            for (int i = 0; i < 8; i++)
            {
                ParticleRegistry.SpawnSparkleParticle(Projectile.Center,
                    Projectile.velocity * Main.rand.NextFloat(.1f, .3f),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .8f),
                    Color.Crimson, Color.DarkRed, .7f, Main.rand.NextFloat(-.1f, .1f));
            }
        }

        after ??= new(30, () => Projectile.Center);

        float squish = MathHelper.Clamp(Projectile.velocity.Length() / 5f, 1f, 2f);
        after.UpdateFancyAfterimages(new(Projectile.Center, new Vector2(1f, .4f * squish) * Projectile.Opacity * 90f,
            Projectile.Opacity, Projectile.rotation, 0, 255, 0, 0f, null, true, -.1f));

        if (Main.rand.NextBool(9))
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(),
                -Projectile.velocity * Main.rand.NextFloat(.2f, .5f), Main.rand.Next(20, 30),
                Main.rand.NextFloat(.2f, .5f), Color.Red,
                Color.Crimson, null, 1.2f, 3);

        NPC target = NPCTargeting.GetWeakestNPC(new(Projectile.Center, 700, false, true));
        if (target.CanHomeInto())
        {
            Projectile.velocity += Projectile.Center.SafeDirectionTo(target.Center) * .6f;
            Projectile.velocity -= Projectile.Center.SafeDirectionTo(target.Center) * .2f;
            if (Modded.SafeMouseRight.Current)
                Projectile.velocity += Projectile.Center.SafeDirectionTo(target.Center) * 10f;
            Projectile.velocity = Projectile.velocity.ClampMagnitude(-10f, 10f);
        }

        Projectile.Opacity = InverseLerp(0f, 12f, Time) * InverseLerp(0f, 1f, Projectile.velocity.Length());
        Projectile.rotation = Projectile.velocity.ToRotation();

        if (Time % 10 == 0 && Projectile.damage < Projectile.originalDamage * 3)
            Projectile.damage = (int) (Projectile.damage * 1.03f);
        Time++;
    }

    public override bool? CanDamage() => Modded.SafeMouseRight.Current ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 6; i++)
        {
            ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Main.rand.NextVector2Circular(2f, 2f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .7f), Color.DarkRed, Color.Red, .9f);
        }
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        void glow()
        {
            Texture2D soft = AssetRegistry.GetTexture(AdditionsTexture.GlowHarsh);
            Main.spriteBatch.DrawBetterRect(soft, ToTarget(Projectile.Center, Projectile.Size * 2.5f), null,
                Color.Crimson * Projectile.Opacity, 0f, soft.Size() / 2);

            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            after?.DrawFancyAfterimages(tex,
                [
                    Color.Lerp(Color.DarkRed, Color.Crimson,
                        InverseLerp(0f, Projectile.originalDamage * 3, Projectile.damage))
                ], Projectile.Opacity, 1f, 0f,
                true);
        }

        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.UnderProjectiles, BlendState.Additive);
        return false;
    }
}
