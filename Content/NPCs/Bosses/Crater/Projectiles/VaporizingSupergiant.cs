using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Pets;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class VaporizingSupergiant : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public const int MaxScale = 2200;
    public int Timer
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int FireTimer
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public int FireCounter
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }
    public int CollapseCounter
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }

    public ref float ToScale => ref Projectile.Additions().ExtraAI[1];
    public static int TotalFlames => 300;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = MaxScale * 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 200;
        Projectile.hostile = true;
        Projectile.scale = 0f;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 7000;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void AI()
    {
        Vector2 TEMPTARGET = new Vector2(Main.spawnTileX * 16, Main.spawnTileY * 16);
        Projectile.Center = TEMPTARGET;
        if (Timer == 0)
        {
            float clumpingFactor = .21f;
            for (int i = 0; i < TotalFlames; i++)
            {
                float radiusInterpolant = i / (float)(TotalFlames - 1f) * 0.85f + MathF.Sqrt(Main.rand.NextFloat()) * 0.15f;
                float starOffsetAngle = MathHelper.TwoPi * i / (clumpingFactor * TotalFlames);
                float starRadius = MathHelper.Lerp(100, 700, MathHelper.Clamp(radiusInterpolant, 0f, 1f));

                Projectile.NewProj(Projectile.Center, PolarVector(starRadius, starOffsetAngle), ModContent.ProjectileType<ConvergingFlame>(), 0, 0f);
            }
        }

        Projectile.timeLeft = 7000;

        float dist = Projectile.Center.Distance(TEMPTARGET);
        Vector2 idealPosition = TEMPTARGET - Vector2.UnitY * MathHelper.Lerp(20f, 40f, Sin01(Timer * .06f));
        Vector2 goalVelocity = (idealPosition - Projectile.Center) * 0.03f;
        float approachAcceleration = 0.1f + Animators.MakePoly(2.6f).InFunction(InverseLerp(70, 0, dist)) * 0.3f;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, goalVelocity, approachAcceleration);
        Projectile.velocity *= 0.98f;

        if (AnyProjectile(ModContent.ProjectileType<ConvergingFlame>()))
            Projectile.scale = MathHelper.Lerp(Projectile.scale, ToScale, .1f);
        else
        {
            Projectile.scale = Utils.Remap(CollapseCounter, 0f, 50f, Projectile.scale, 0f);
            if (Projectile.scale <= 0f)
            {
                Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<DisintegrationNova>(), Asterlin.SuperHeavyAttackDamage, 0f);
                Projectile.Kill();
            }
            CollapseCounter++;
        }

        foreach (Player p in Main.ActivePlayers)
        {
            Vector2 gaussian = p.Center.SafeDirectionTo(Projectile.Center) * Utility.GaussianFalloff2D(Projectile.Center, p.Center, .2f, Projectile.scale * 10f);
            //p.velocity += gaussian;
        }

        Timer++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float rad = Projectile.scale;
        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.OrganicNoise);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(rad)), null, Color.White, 0f, tex.Size() / 2, 0);
        }

        ManagedShader shader = AssetRegistry.GetShader("IntenseFireball");
        shader.TrySetParameter("time", Timer * .01f);
        shader.TrySetParameter("resolution", new Vector2(rad));
        shader.TrySetParameter("opacity", Animators.MakePoly(3f).OutFunction(Utils.Remap(Projectile.scale, 0f, MaxScale, 0f, 1f)));

        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderPlayers, null, shader);


        
        return false;
    }
}

public class DisintegrationNova : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override bool IgnoreOwnerActivity => true;
    public static int Lifetime = SecondsToFrames(1.2f);
    public static int MaxRadius = VaporizingSupergiant.MaxScale * 5;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 14000;
    }
    public override void SetDefaults()
    {
        Projectile.Size = new(1);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        if (Time == 0)
        {
            ScreenShakeSystem.New(new(2f, 2.2f, 9000f), Projectile.Center);
            ParticleRegistry.SpawnBlurParticle(Projectile.Center, Lifetime / 2, 1.5f, MaxRadius * 2);
            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, Lifetime / 2, .5f, MaxRadius * 2);
            ParticleRegistry.SpawnFlash(Projectile.Center, 20, .2f, MaxRadius * 2);
            AdditionsSound.MomentOfCreation.Play(Projectile.Center, 1.5f, -1f);
        }
        Projectile.scale = InverseLerp(0f, Lifetime, Time);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, MaxRadius * Projectile.scale, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader fireball = AssetRegistry.GetShader("FireballExplosion");
        fireball.TrySetParameter("scale", Projectile.scale);

        Main.spriteBatch.EnterShaderRegion(null, fireball.Effect);
        Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.TurbulentNoise);
        fireball.Render();
        Main.spriteBatch.DrawBetterRect(noise, ToTarget(Projectile.Center, new Vector2(MaxRadius)), null, Color.Goldenrod, 0f, noise.Size() / 2f);
        Main.spriteBatch.ExitShaderRegion();
        return false;
    }
}