using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Everbladed;

public class HyperSlash : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public ref float Time => ref Projectile.ai[0];

    public ManualTrailPoints Positions;
    public OptimizedPrimitiveTrail Blood;
    public OptimizedPrimitiveTrail Lightning;

    public const int SliceTime = 40;
    public const int FadeTime = 30;
    public const int Lifetime = SliceTime + FadeTime;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.extraUpdates = 2;
        Projectile.timeLeft = Lifetime;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.noEnchantmentVisuals = true;
    }

    public Vector2 Start
    {
        get;
        set;
    }
    public Vector2 Center
    {
        get;
        set;
    }
    public Vector2 End
    {
        get;
        set;
    }

    public List<Vector2> Points = [];
    public override void AI()
    {
        if (Blood == null || Blood._disposed)
            Blood = new(AltWidthFunct, AltColorFunct);
        if (Lightning == null || Lightning._disposed)
            Lightning = new(WidthFunct, ColorFunct);

        Positions ??= new(40);

        Vector2 controlPoint = 2 * Center - 0.5f * Start - 0.5f * End;

        Points = [];
        for (int i = 0; i < 40; i++)
        {
            float t = InverseLerp(0f, SliceTime, Time) * i / (float)(40 - 1);
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float ut2 = 2 * u * t;

            // Quadratic Bézier formula: B(t) = (1-t)^2 * P0 + 2 * (1-t) * t * P1 + t^2 * P2
            Points.Add((uu * Start) + (ut2 * controlPoint) + (tt * End));
        }

        Positions.SetPoints(Points);

        Projectile.scale = Animators.MakePoly(2f).InOutFunction.Evaluate(1f, 0f, InverseLerp(SliceTime, Lifetime, Time));
        if (Projectile.scale <= 0f)
            Projectile.Kill();

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(Positions.Points, WidthFunct);
    }

    private float WidthFunct(float c) => Convert01To010(c) * Projectile.height * .5f * Projectile.scale;
    private Color ColorFunct(SystemVector2 c, Vector2 position) => Color.DarkRed.Lerp(Color.Crimson, InverseLerp(10f, 0f, Time)).Lerp(Color.Black, .2f) * Projectile.Opacity * Convert01To010(c.X);
    private float AltWidthFunct(float c) => MathF.Pow(Convert01To010(c), 2) * Projectile.height * .9f * Projectile.scale;
    private Color AltColorFunct(SystemVector2 c, Vector2 position) => ColorFunct(c, position) * 1.7f;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (Positions == null)
                return;

            if (Blood != null)
            {
                ManagedShader aura = ShaderRegistry.BloodBeacon;
                aura.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1);

                Blood.DrawTrail(aura, Positions.Points, 100, true);
            }

            if (Lightning != null)
            {
                ManagedShader slice = ShaderRegistry.SpecialLightningTrail;
                slice.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1);

                Lightning.DrawTrail(slice, Positions.Points, 100, true);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);

        return false;
    }
}