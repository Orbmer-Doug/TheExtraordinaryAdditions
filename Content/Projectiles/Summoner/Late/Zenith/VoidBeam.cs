using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late.Avia;

public class VoidBeam : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public ref float Time => ref Projectile.ai[0];
    public Projectile Proj
    {
        get
        {
            foreach (Projectile p in Main.ActiveProjectiles)
            {
                if (p != null && p.identity == Projectile.ai[1] && p.owner == Projectile.owner)
                    return p;
            }
            return null;
        }
    }

    public ref float Length => ref Projectile.ai[2];
    public const float MaxLength = 3000f;

    public bool Fade
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public ref float FadeTime => ref Projectile.AdditionsInfo().ExtraAI[1];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 120;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 5;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public override void AI()
    {
        if (Proj == null || FadeTime > 30f)
        {
            Projectile.Kill();
            return;
        }
        else
            Projectile.timeLeft = 2;

        points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Proj.rotation.ToRotationVector2() * Length, 50));
        if (laser == null || laser.Disposed)
            laser = new(WidthFunct, ColorFunct, null, 50);
        Projectile.Center = Proj.Center;

        if (Proj.As<AvragenMinion>().Phase != AvragenMinion.AvraPhases.CurvedBeam || Proj.As<AvragenMinion>().Idling)
            Fade = true;

        if (Fade)
            FadeTime++;

        Length = Animators.MakePoly(4f).InOutFunction.Evaluate(0f, MaxLength, Fade ? FadeInterpolant : InverseLerp(0f, 30f, Time));
        Time++;
    }

    public float FadeInterpolant => InverseLerp(30f, 0f, FadeTime);
    public float WidthFunct(float c)
    {
        return OptimizedPrimitiveTrail.HemisphereWidthFunct(c,
            15f + Animators.MakePoly(10f).OutFunction(MathHelper.SmoothStep(0f, 1f, c)) * Projectile.height * FadeInterpolant);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 position)
    {
        return MulticolorLerp(c.X, Color.Purple, Color.Violet, Color.BlueViolet, Color.DarkViolet);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public OptimizedPrimitiveTrail laser;
    public TrailPoints points = new(50);
    public override bool PreDraw(ref Color lightColor)
    {
        void beam()
        {
            if (laser != null && !laser.Disposed)
            {
                ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise2), 1);

                laser.DrawTrail(shader, points.Points, 100);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(beam, PixelationLayer.OverProjectiles);
        return false;
    }

    public override bool ShouldUpdatePosition() => false;
}