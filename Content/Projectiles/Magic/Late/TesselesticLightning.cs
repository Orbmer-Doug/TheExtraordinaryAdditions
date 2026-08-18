using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class TesselesticLightning : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    private const int Life = 30;

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Magic;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.timeLeft = Life;
        Projectile.penetrate = -1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float PosX => ref Projectile.ai[1];
    public ref float PosY => ref Projectile.ai[2];

    public Color MainColor;
    public float Completion => MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));

    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct);

        if (Time == 0f)
        {
            points = new(100);
            points.SetPoints(GetBoltPoints(Projectile.Center, new(PosX, PosY), 10f, 7f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity is > 0f and < .05f)
            Projectile.Kill();

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int) (Projectile.damage * .4f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c) => 40f * InverseLerp(1.5f, 0f, c) * Projectile.Opacity;

    public Color ColorFunct(SystemVector2 c, Vector2 pos) =>
        MulticolorLerp(Completion, Color.White, MainColor) * Projectile.Opacity;

    public TrailPoints points;
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = AssetRegistry.GennedShaders.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GennedTextures.TechyNoise, 1);
                trail.DrawTrail(shader, points.Points);
            }
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
