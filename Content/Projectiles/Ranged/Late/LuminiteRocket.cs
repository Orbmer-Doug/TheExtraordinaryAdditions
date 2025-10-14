using System;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class LuminiteRocket : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LuminiteRocket);
    public ref float Time => ref Projectile.ai[0];

    public Player Owner => Main.player[Projectile.owner];
    public override void SetDefaults()
    {
        Projectile.width = 20;
        Projectile.height = 34;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.MaxUpdates = 3;
    }

    public Vector2 Back => Projectile.Center + PolarVector(Projectile.width / 2, Projectile.rotation - MathHelper.PiOver2);
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 30);
        points.Update(Back + Projectile.velocity);

        Projectile.FacingUp();

        Lighting.AddLight(Back, Color.DarkCyan.ToVector3() * 1.1f);
        Time++;
    }

    public Color ColorFunction(SystemVector2 c, Vector2 pos)
    {
        Color startingColor = Color.Lerp(Color.Cyan, Color.White, 0.4f);
        Color middleColor = Color.Lerp(Color.DarkCyan, Color.LightCyan, 0.2f);
        Color endColor = Color.Lerp(Color.DarkCyan, Color.LightCyan, 0.67f);
        return MulticolorLerp(c.X, startingColor, middleColor, endColor) * GetLerpBump(0f, .07f, .8f, .25f, c.X);
    }

    public float WidthFunction(float completionRatio)
    {
        return MathHelper.SmoothStep(Projectile.height * .5f, 4f, completionRatio);
    }


    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Time < 10f)
            modifiers.SetCrit();
    }

    public override void OnKill(int timeLeft)
    {
        AdditionsSound.charExplo.Play(Projectile.Center, Main.rand.NextFloat(.9f, 1.1f), 0f, .1f);

        int count = 50;
        for (int a = 0; a <= count; a++)
        {
            if (a < 20)
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, Vector2.Zero, 22, Main.rand.NextFloat(90f, 100f), Color.LightCyan);

            float scale = Main.rand.NextFloat(.3f, 1f);
            Vector2 vel = (MathHelper.TwoPi * a / count + Main.rand.NextFloat(MathHelper.TwoPi)).ToRotationVector2() * Main.rand.NextFloat(2f, 12f);
            ParticleRegistry.SpawnMistParticle(Projectile.Center, vel, scale, Color.Cyan, Color.Transparent, Main.rand.NextByte(100, 255));

            for (int i = 0; i <= count; i++)
            {
                ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel * .4f, 50, scale * .5f, Color.DarkCyan, .3f, true);
            }
        }

        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LuminiteBlast>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
    }
    
    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(30);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseDim), 1);
            trail.DrawTrail(shader, points.Points);
        }

        Projectile.DrawBaseProjectile(Color.White);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}

public class LuminiteBlast : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(180f);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 10;
        Projectile.timeLeft = 6;
        Projectile.penetrate = -1;
    }
}