using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class HonedTesselesticLightning : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private const int Life = 30;
    public override void SetDefaults()
    {
        Projectile.ignoreWater = Projectile.tileCollide = Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.Size = new(16);
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public ref float Time => ref Projectile.ai[0];
    public Vector2 End
    {
        get => new Vector2(Projectile.ai[1], Projectile.ai[2]);
        set
        {
            Projectile.ai[1] = value.X;
            Projectile.ai[2] = value.Y;
        }
    }

    public float Completion => Animators.MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            List<Vector2> bolt = GetBoltPoints(Projectile.Center, End, 10f, 4f);
            points = new(bolt.Count);
            points.SetPoints(bolt);
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity.BetweenNum(0f, .05f))
            Projectile.Kill();

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c) => 40f * InverseLerp(1.5f, 0f, c) * Projectile.Opacity;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(Completion, Color.White, Color.Cyan) * Projectile.Opacity;
    public TrailPoints points;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.TechyNoise), 1);
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}