using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class LightripBullet : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public const int Lifetime = 50;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2000;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = false;
        Projectile.localNPCHitCooldown = 2;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 8;
        Projectile.timeLeft = Lifetime * Projectile.MaxUpdates;

        Projectile.stopsDealingDamageAfterPenetrateHits = true;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public bool Wait
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override void AI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 50);

        points.Update(Projectile.Center + Projectile.velocity);

        if (Wait)
        {
            Projectile.MaxUpdates = 2;
            if (points.Points.AllPointsEqual())
                Projectile.Kill();

            Projectile.timeLeft = 200;
        }

        Time++;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        HitEffects();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        HitEffects();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        HitEffects();
        return false;
    }

    public void HitEffects()
    {
        if (!Main.dedServ)
            AdditionsSound.FireImpact.Play(Projectile.Center, .8f, 0f, .1f, 40, Name);
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<LightripBlast>(), Projectile.damage / 2, 0f, Main.myPlayer);
        Time = 0;
        Wait = true;
        Projectile.velocity *= 0f;
    }

    public float Completion => InverseLerp(0f, Lifetime * Projectile.MaxUpdates, Time);
    public float WidthFunct(float c) => Animators.MakePoly(3.6f).OutFunction.Evaluate(10f, 0f, Completion) * Animators.MakePoly(5f).OutFunction(c);
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color.Cyan * Animators.MakePoly(2.3f).InOutFunction.Evaluate(1f, 0f, Completion);

    public TrailPoints points = new(60);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise), 1, SamplerState.LinearWrap);
            trail.DrawTrail(shader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
