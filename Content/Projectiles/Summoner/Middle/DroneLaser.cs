using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class DroneLaser : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.timeLeft = 30;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Summon;
    }

    public Vector2 Start => Projectile.Center;
    public Vector2 End;
    public const float MaxDist = 1400f;
    public bool HitSomething
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float Time => ref Projectile.ai[1];
    public ref float ChargeCompletion => ref Projectile.ai[2];
    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(End);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        End = reader.ReadVector2();
    }

    public override void AI()
    {
        Projectile.width = Projectile.height = 10 + (int)(10 * ChargeCompletion);

        if (trail == null || trail._disposed)
            trail = new(tip, c => Projectile.height * Projectile.scale, (c, pos) => Color.Cyan * Projectile.scale, null, 50);

        Vector2 expected = Start + Projectile.velocity.SafeNormalize(Vector2.Zero) * Animators.MakePoly(2.8f).OutFunction.Evaluate(Time, 0f, 17f, 0f, MaxDist);
        End = LaserCollision(Start, expected, CollisionTarget.Tiles | CollisionTarget.NPCs);

        if (End != expected && !HitSomething)
        {
            HitSomething = true;
        }

        cache ??= new(50);
        cache.SetPoints(Start.GetLaserControlPoints(End, 50));

        Projectile.scale = InverseLerp(0f, 10f, Projectile.timeLeft);
        if (!HitSomething)
            Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(cache.Points, c => 10f * Projectile.scale);
    }

    public ManualTrailPoints cache;
    public OptimizedPrimitiveTrail trail;
    public static readonly ITrailTip tip = new RoundedTip(20);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;
            ManagedShader shader = ShaderRegistry.CrunchyLaserShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkTurbulentNoise), 1);
            trail.DrawTippedTrail(shader, cache.Points, tip);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
