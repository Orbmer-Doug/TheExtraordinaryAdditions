using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class HemoglobBarrier : ProjOwnedByNPC<StygainHeart>
{
    private List<Vector2> cache;
    private TrailPoints points;
    public ref float CurrentWidth => ref Projectile.ai[0];
    public bool FadeOut
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public override string Texture => AssetRegistry.Invis;
    private const int Points = 150;

    public override void SetDefaults()
    {
        Projectile.timeLeft = 9000;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.width = 100;
        Projectile.height = 100;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
    }

    public override void SafeAI()
    {
        points ??= new(Points * 2);
        if (cache is null)
        {
            cache = [];

            for (int i = 0; i < Points; i++)
            {
                cache.Add(Projectile.Center);
            }
        }

        for (int k = 0; k < Points; k++)
            cache[k] = Projectile.Center + ((MathF.Tau + .1f) * InverseLerp(0f, Points, k)).ToRotationVector2() * StygainHeart.BarrierSize;

        if (trail == null || trail.Disposed)
            trail = new(WidthFunction, ColorFunction, null, 5);
        points.SetPoints(cache);

        for (int k = 0; k < 4; k++)
        {
            float rot = RandomRotation();
            Vector2 pos = Projectile.Center + (MathHelper.TwoPi * Main.rand.NextFloat()).ToRotationVector2() * StygainHeart.BarrierSize;
            Vector2 vel = Vector2.One.RotatedBy(rot + Main.rand.NextFloat(1.1f, 1.3f)) * 2 * Main.rand.NextBool().ToDirectionInt();
            Color color = Color.Crimson;
            ParticleRegistry.SpawnSquishyLightParticle(pos, vel, 20, Main.rand.NextFloat(.2f, .4f), color, .2f);
        }

        // Pull in any players
        foreach (Player player in Main.ActivePlayers)
        {
            if (player == null || player.DeadOrGhost)
                continue;
            if (player.Distance(Projectile.Center) > StygainHeart.BarrierSize)
            {
                ParticleRegistry.SpawnBloomLineParticle(player.RotHitbox().RandomPoint(), Vector2.UnitY * -Main.rand.NextFloat(2f, 5f),
                    Main.rand.Next(10, 20), Main.rand.NextFloat(.3f, .6f), Color.DarkRed);

                Vector2 edge = ClosestPointOnCircle(player.Center, Projectile.Center, StygainHeart.BarrierSize, true);
                foreach (Vector2 point in edge.GetLaserControlPoints(player.Center, 30))
                {
                    ParticleRegistry.SpawnGlowParticle(point, player.Center.SafeDirectionTo(edge) * Main.rand.NextFloat(1f, 3f),
                        Main.rand.Next(30, 40), Main.rand.NextFloat(30f, 40f), Color.Crimson, 1.2f);
                }
                player.velocity = player.SafeDirectionTo(Projectile.Center)
                    * MathF.Min(15f, player.Distance(Projectile.Center + PolarVector(StygainHeart.BarrierSize, player.SafeDirectionTo(Projectile.Center).ToRotation())));
            }
        }

        if (FadeOut)
        {
            CurrentWidth--;
            if (CurrentWidth <= 0f)
                Projectile.Kill();
        }

        if (Projectile.ai[2] == 1)
        {
            this.Sync();
            Projectile.ai[2] = 0;
        }
    }

    public override bool CanHitPlayer(Player target)
    {
        if (CurrentWidth > 1f)
            return true;

        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        for (int i = 0; i < cache.Count; i++)
        {
            int x = (int)cache[i].X;
            int y = (int)cache[i].Y;
            int width = (int)WidthFunction(i / cache.Count);
            if (targetHitbox.Intersects(new Rectangle(x, y, width, width)))
                return true;
        }

        return false;
    }

    private float WidthFunction(float c)
    {
        return CurrentWidth;
    }

    private Color ColorFunction(SystemVector2 v, Vector2 position)
    {
        Color col = MulticolorLerp(v.X + Sin01(Main.GlobalTimeWrappedHourly * 2f), Color.Crimson, Color.DarkRed, Color.IndianRed);
        return col;
    }

    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail.Disposed || points == null)
                return;

            ManagedShader barrier = ShaderRegistry.EnlightenedBeam;

            barrier.TrySetParameter("time", Projectile.timeLeft * 0.01f);
            barrier.TrySetParameter("repeats", 6f);
            barrier.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1, SamplerState.LinearWrap);
            barrier.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 2, SamplerState.LinearWrap);
            trail.DrawTrail(barrier, points.Points, 30);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);
        return false;
    }
}