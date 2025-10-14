using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class EmpyreanRipshot : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public ref float Fade => ref Projectile.ai[1];
    public bool Hit
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public const int Lifetime = 180;
    public const int Fadetime = 20;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1400;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = 2;
        Projectile.extraUpdates = 5;
    }

    public Player Owner => Main.player[Projectile.owner];
    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 20);

        points.Update(Projectile.Center + Projectile.velocity);

        if (Projectile.numHits > 0)
        {
            Projectile.velocity *= .4f;
            Projectile.Opacity = InverseLerp(Fadetime, 0f, Fade);
            if (Projectile.Opacity <= 0f)
                Projectile.Kill();
            Fade++;
        }
        else
            Projectile.Opacity = InverseLerp(0f, 4f * Projectile.MaxUpdates, Time);

        Time++;
    }

    public override bool? CanDamage() => !Hit;
    public override bool? CanHitNPC(NPC target) => !Hit;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.etherealHitBoom2.Play(Projectile.Center, 1.6f, 0f, .1f, 10);
        if (this.RunLocal())
        {
            CosmicBlast blast = Main.projectile[Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<CosmicBlast>(),
                (int)(Projectile.damage * 1.25f), Projectile.knockBack, Owner.whoAmI)].As<CosmicBlast>();
            blast.Target = target;
            blast.Offset = Projectile.position - target.position;
            blast.Offset -= Projectile.velocity;
        }
        Hit = true;
    }

    public Color ColorFunction(SystemVector2 c, Vector2 pos)
    {
        float trailOpacity = GetLerpBump(0f, 0.067f, 0.9f, 0.1f, c.X) * 0.9f;

        Color startingColor = Color.Lerp(Color.Black, Color.Fuchsia, 0.25f);
        Color middleColor = Color.Lerp(Color.Violet, Color.BlueViolet, 0.4f);
        Color endColor = Color.Lerp(Color.Purple, Color.Black, 0.35f);
        Color color = MulticolorLerp(c.X, startingColor, middleColor, endColor);

        color *= trailOpacity;
        color.A = (byte)(trailOpacity * 255);
        return color * Projectile.Opacity;
    }

    private float WidthFunction(float completionRatio) =>
         Projectile.width * 0.6f * MathHelper.SmoothStep(0.6f, 1f, Utils.GetLerpValue(0f, 0.3f, completionRatio, true)) * Projectile.Opacity;

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader glow = ShaderRegistry.DissipatedGlowTrail;
            glow.TrySetParameter("OutlineColor", Color.Fuchsia.ToVector4());

            glow.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Cosmos), 0);
            glow.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.ShadowTrail), 1);
            glow.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.PurpleNebulaBright), 2);

            trail.DrawTrail(glow, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        return false;
    }
}
