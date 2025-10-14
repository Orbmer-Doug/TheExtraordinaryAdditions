using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class DarkArrow : ModProjectile
{
    public override string Texture => ProjectileID.UnholyArrow.GetTerrariaProj();
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 1;
        Projectile.timeLeft = Projectile.ArrowLifeTime;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.arrow = true;
        Projectile.alpha = 255;
    }

    public Player Owner => Main.player[Projectile.owner];
    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[0];
    public override void AI()
    {
        Projectile.FacingUp();

        Vector2 pos = Projectile.Center + Projectile.velocity + PolarVector(12f, Projectile.velocity.ToRotation());
        if (Main.rand.NextBool(5))
        {
            Dust.NewDustPerfect(pos, DustID.Demonite, Main.rand.NextVector2Circular(2f, 2f), 150, default, Main.rand.NextFloat(.9f, 1.15f));
        }

        Projectile.alpha -= 25;
        if (Time >= 5f)
            Projectile.velocity.Y += 0.15f;

        if (Time == 45f)
        {
            Vector2 orbVel = Main.rand.NextVector2Circular(2f, 2f);
            if (this.RunLocal())
                Projectile.NewProj(pos, orbVel, ModContent.ProjectileType<CorruptOrb>(), (int)(Projectile.damage * .4f), Projectile.knockBack * .4f, Projectile.owner);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnSquishyPixelParticle(pos, orbVel.RotatedByRandom(.3f) * Main.rand.NextFloat(.6f, .9f), Main.rand.Next(70, 120), Main.rand.NextFloat(.8f, 1.2f), Color.Violet, Color.DarkViolet, 5);
        }

        if (trail == null || trail.Disposed)
            trail = new(c => Projectile.width, (c, pos) => Color.Violet.Lerp(Color.DarkViolet, MathHelper.SmoothStep(1f, 0f, c.X)) * MathHelper.SmoothStep(1f, 0f, c.X), null, 5);
        points ??= new(5);
        points.Update(Projectile.Center + Projectile.velocity - PolarVector(12f, Projectile.velocity.ToRotation()));

        Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * .35f);

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.Dig.Play(Projectile.Center);

        Vector2 pos = Projectile.RotHitbox().Bottom;
        for (int a = 0; a < 15; a++)
        {
            Dust.NewDust(pos, Projectile.width, Projectile.height, DustID.Demonite, 0f, 0f, 150, default, 1.1f);
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points;
    public override bool PreDraw(ref Color lightColor)
    {
        void prim()
        {
            if (trail != null && !trail.Disposed)
                trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, points.Points, 50, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(prim, PixelationLayer.UnderProjectiles);

        Projectile.DrawBaseProjectile(lightColor);
        float interpolant = GetLerpBump(0f, 45f, 50f, 45f, Time);

        if (interpolant > 0f)
            Projectile.DrawProjectileBackglow(Color.Violet * interpolant, 3f * interpolant, 50, 4);

        return false;
    }
}
