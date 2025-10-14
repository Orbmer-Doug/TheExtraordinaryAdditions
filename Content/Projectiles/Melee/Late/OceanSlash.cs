using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class OceanSlash : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    private Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 300;
        Projectile.timeLeft = 400;
        Projectile.penetrate = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.extraUpdates = 3;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
    }

    public ref float Time => ref Projectile.ai[0];
    public TrailPoints points = new(20);
    public float Fade => Animators.MakePoly(4f).InFunction(InverseLerp(0f, 20f * Projectile.MaxUpdates, Projectile.timeLeft));
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, Colorfunct, null, 20);

        Lighting.AddLight(Projectile.Center, Color.Aqua.ToVector3() * 1f);
        Projectile.rotation = Projectile.velocity.ToRotation();

        Vector2 a = Projectile.Center - PolarVector(Projectile.width * Fade, Projectile.rotation - MathHelper.PiOver2);
        Vector2 b = Projectile.Center + PolarVector(Projectile.width, Projectile.rotation);
        Vector2 c = Projectile.Center - PolarVector(Projectile.width * Fade, Projectile.rotation + MathHelper.PiOver2);

        for (int i = 0; i < 20; i++)
        {
            float lerp = InverseLerp(0f, 20, i);
            Vector2 pos = QuadraticBezier(a, b, c, lerp);
            points.SetPoint(i, pos);
        }
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.DefenseEffectiveness *= 0f;
    }

    public OptimizedPrimitiveTrail trail;
    public float WidthFunct(float c) => Convert01To010(c) * 150f * Animators.MakePoly(3f).InOutFunction(InverseLerp(0f, 20f * Projectile.MaxUpdates, Time)) * Fade;
    public Color Colorfunct(SystemVector2 c, Vector2 pos) => Color.DarkBlue.Lerp(Color.Black, c.Y) * (1f + Convert01To010(c.X) * 4f)
        * Animators.MakePoly(3f).OutFunction(InverseLerp(Projectile.MaxUpdates, 20f * Projectile.MaxUpdates, Time));
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.WaterCurrent;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DendriticNoiseZoomedOut), 1);
                trail.DrawTrail(shader, points.Points, 100, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}