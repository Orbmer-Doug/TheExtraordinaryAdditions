using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class TheLightripBullet : ProjOwnedByNPC<Asterlin>
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
        Projectile.friendly = Projectile.tileCollide = false;
        Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = true;
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

    public override void SafeAI()
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

    public override void OnHitPlayer(Player target, Player.HurtInfo info) => HitEffects();
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => HitEffects();

    public void HitEffects()
    {
        if (!Main.dedServ)
            AdditionsSound.FireImpact.Play(Projectile.Center, .8f, 0f, .1f, 40, Name);

        for (int i = 0; i < 30; i++)
        {
            Vector2 off = Main.rand.NextVector2Unit(0f, MathHelper.TwoPi) * (float)Math.Pow(Main.rand.NextFloat(), 2.4) * Projectile.Size * 0.5f;
            Vector2 vel = off.SafeNormalize(Vector2.UnitY).RotatedByRandom((double)(MathHelper.PiOver2 * Main.rand.NextFloatDirection()));
            Vector2 val2 = off / Projectile.Size / 0.5f;
            vel *= MathHelper.Lerp(3f, 9f, Utils.GetLerpValue(0.05f, 0.85f, val2.Length(), false));

            Vector2 pos = Projectile.Center + off;
            Color color = MulticolorLerp(Main.rand.NextFloat(0.2f, 0.8f), Color.Cyan, Color.DeepSkyBlue, Color.SkyBlue, Color.LightCyan);

            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel / 2, 50, 1f, color, .4f, true);
            ParticleRegistry.SpawnMistParticle(pos, vel.RotatedByRandom(.3f), Main.rand.NextFloat(.7f, 1.1f), color, Color.Transparent, Main.rand.NextFloat(160f, 190f), Main.rand.NextFloat(-.2f, .2f));
        }

        Time = 0;
        Wait = true;
        Projectile.velocity *= 0f;
        this.Sync();
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