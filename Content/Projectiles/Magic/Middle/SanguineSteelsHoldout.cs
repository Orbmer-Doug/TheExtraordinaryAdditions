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

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class SanguineSteelsHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LanceOfSanguineSteels);
    public ref float Time => ref Projectile.ai[0];
    public ref float Wait => ref Projectile.ai[1];
    public ref float PortalDist => ref Projectile.ai[2];
    public ref float Fade => ref Projectile.Additions().ExtraAI[0];

    public const int WaitTime = 8;
    public const float Length = 99f;
    public const int FadeIn = 50;

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
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .2f);
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
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation - MathHelper.PiOver4);
        PortalDist = Animators.MakePoly(4f).InOutFunction.Evaluate(0f, 120f, Projectile.Opacity, true);

        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Wait <= 0f && Time > FadeIn && Fade <= 0f)
        {
            float rot = Projectile.rotation - MathHelper.PiOver4;
            Vector2 rand = NextVector2Ellipse(100f, 200f, rot);
            Vector2 pos = Projectile.Center - PolarVector(PortalDist, rot) + rand;

            if (!Collision.SolidCollision(pos, 20, 20))
            {
                Vector2 vel = pos.SafeDirectionTo((Center + Projectile.velocity * 100f) + rand);
                Projectile.NewProj(pos, vel * 10f, ModContent.ProjectileType<SanguineLance>(), Projectile.damage, Projectile.knockBack, Projectile.owner);

                for (int i = 0; i < 20; i++)
                {
                    ParticleRegistry.SpawnHeavySmokeParticle(pos, vel.RotatedByRandom(.2f) * Main.rand.NextFloat(4f, 8f),
                        Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .7f), Color.DarkRed, Main.rand.NextFloat(.2f, .3f));
                }
                SoundID.Item60.Play(pos, .6f, -.1f, 0f, null, 20, Name);

                Wait = WaitTime;
            }
        }

        if (Wait > 0f)
            Wait--;

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float rot = Main.GameUpdateCount / 40f;
        ManagedShader effect = ShaderRegistry.MagicRing;
        effect.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.VoronoiShapes2), 1, SamplerState.LinearWrap);
        effect.TrySetParameter("firstCol", Color.DarkRed.ToVector3());
        effect.TrySetParameter("secondCol", Color.Crimson.ToVector3());
        effect.TrySetParameter("time", rot);
        effect.TrySetParameter("cosine", (float)Math.Cos(rot));
        effect.TrySetParameter("opacity", 2.5f);

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, effect.Effect);
        Texture2D portal = AssetRegistry.GetTexture(AdditionsTexture.GlowParticle);

        Main.spriteBatch.DrawBetterRect(portal, ToTarget(Projectile.Center - PolarVector(PortalDist, Projectile.rotation - MathHelper.PiOver4), new Vector2(200f, 300f)),
            null, Color.Crimson * .18f * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver4, portal.Size() / 2);

        portal = AssetRegistry.GetTexture(AdditionsTexture.UnfathomablePortal);
        
        Main.spriteBatch.DrawBetterRect(portal, ToTarget(Projectile.Center - PolarVector(PortalDist, Projectile.rotation - MathHelper.PiOver4), new Vector2(100f, 200f)),
            null, Color.DarkRed * Projectile.Opacity, Projectile.rotation - MathHelper.PiOver4, portal.Size() / 2);

        portal = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
        Main.spriteBatch.DrawBetterRect(portal, ToTarget(Projectile.Center - PolarVector(PortalDist, Projectile.rotation - MathHelper.PiOver4), new Vector2(50f, 100f)),
            null, Color.DarkRed * Animators.MakePoly(3f).InFunction(Projectile.Opacity), Projectile.rotation - MathHelper.PiOver4, portal.Size() / 2);

        Main.spriteBatch.ExitShaderRegion();

        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}