using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Multi.Early;

public class FulgurZap : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
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
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.width = Projectile.height = 16;
    }

    public ref float Time => ref Projectile.ai[0];
    public Vector2 End { get; set; }
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(End);
    public override void ReceiveExtraAI(BinaryReader reader) => End = reader.ReadVector2();
    public float Completion => Animators.MakePoly(6f).OutFunction(InverseLerp(0f, Life, Time));
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null);

        if (Time == 0f)
        {
            points = new(100);
            points.SetPoints(GetBoltPoints(Projectile.Center, End, 10f, 10f));
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

    public float WidthFunct(float c) => 10f * Convert01To010(c) * Projectile.Opacity;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(Completion, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan) * Projectile.Opacity;
    public TrailPoints points;
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail != null && points != null)
            {
                ManagedShader shader = ShaderRegistry.SpecialLightningTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkRidgeNoise), 1);
                trail.DrawTrail(shader, points.Points);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}