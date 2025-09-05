using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Fulmina;

internal class HonedLightning : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int Life = 30;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
    }
    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }
    public ref float Time => ref Projectile.ai[0];
    public ref float Power => ref Projectile.ai[1];
    public float Width => Utils.Remap(Power, 0f, CondereFulminaHoldout.TotalReelTime, 32f, 100f);
    public Vector2 End { get; set; }
    public float Completion => Animators.MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            points = new(100);
            points.SetPoints(GetBoltPoints(Projectile.Center, End, 150f, 4f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity.BetweenNum(0f, .05f))
            Projectile.Kill();

        Time++;
    }

    public override bool? CanDamage() => Projectile.numHits <= 0;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c) => Width * Projectile.Opacity;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(Completion, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan) * Projectile.Opacity;
    public ManualTrailPoints points;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CausticNoise), 1);
                trail.DrawTrail(shader, points.Points);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}
