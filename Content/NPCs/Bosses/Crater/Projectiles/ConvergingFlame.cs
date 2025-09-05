using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class ConvergingFlame : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float OrbitOffsetAngle => ref Projectile.ai[1];
    public ref float OrbitSquish => ref Projectile.ai[2];
    public ref float OrbitRadius => ref Projectile.Additions().ExtraAI[0];

    public static int FormationTime => 50;
    public static int SpeedUp => 350;

    public override void SetDefaults()
    {
        Projectile.Size = new(200f);
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void AI()
    {
        if (FindProjectile(out Projectile star, ModContent.ProjectileType<VaporizingSupergiant>()))
        {
            VaporizingSupergiant giant = star.As<VaporizingSupergiant>();

            if (trail == null || trail._disposed)
                trail = new(c => 200f * Projectile.scale, (c, pos) => Color.White, null, 25);
            Vector2 target = star.Center;
            Projectile.scale = Animators.MakePoly(2.4f).OutFunction(InverseLerp(0f, FormationTime, Time));
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Time >= FormationTime)
            {
                Projectile.ProjAntiClump(.1f, false);
                float dist = Projectile.Distance(target);
                
                float speed = MathF.Min(dist, Utils.Remap(Time, FormationTime, FormationTime + SpeedUp, 14f, 40f));
                float amt = 0.1f;
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target) * speed, amt);
                Projectile.velocity *= .98f;
                
                Projectile.scale = Animators.MakePoly(3.3f).OutFunction.Evaluate(dist, Projectile.width, Projectile.width + giant.Projectile.scale, 0f, 1f);
                if (Projectile.scale <= 0f)
                {
                    giant.ToScale += VaporizingSupergiant.MaxScale / VaporizingSupergiant.TotalFlames;
                    Projectile.Kill();
                }
            }
            else
            {
                Projectile.velocity *= .94f;
            }
            Projectile.velocity = Projectile.velocity.ClampMagnitude(5f, 2000f);

            points.Update(Projectile.Center);
            Time++;
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(25);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail._disposed || points == null)
                return;
            ManagedShader shader = AssetRegistry.GetShader("ConvergingFlameShader");
            shader.TrySetParameter("time", Main.GlobalTimeWrappedHourly * 1.2f);
            shader.TrySetParameter("opacity", Projectile.scale);
            trail.DrawTrail(shader, points.Points, 200, true, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles, null);
        return false;
    }
}