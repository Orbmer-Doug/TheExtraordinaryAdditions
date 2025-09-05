using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class LuminescentChaser : ModProjectile
{
    public bool HasHitTarget
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.localAI[0];

    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 90;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.Opacity = 1f;
        Projectile.MaxUpdates = 2;
        Projectile.timeLeft = Projectile.MaxUpdates * 180;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
    }

    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, 20);
        points.Update(GetTransformedScreenCoords(Projectile.Center) + Main.screenPosition);;

        if (HasHitTarget)
        {
            Projectile.velocity = Projectile.velocity.RotatedBy((double)((Projectile.identity % 2f == 0f).ToDirectionInt() * 0.06f), default) * 0.93f;
            if (Projectile.timeLeft >= 30)
                Projectile.timeLeft = 30;
        }

        else if (Time >= 29f)
        {
            NPC target = NPCTargeting.GetClosestNPC(new(Projectile.Center, 4000));
            if (target.TargetValid())
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center + target.velocity * 10f) * 24f, .2f);
            else
                Projectile.velocity *= .98f;
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.scale = Projectile.Opacity = InverseLerp(0f, 30f, Projectile.timeLeft);

        if (Projectile.FinalExtraUpdate())
            Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int amount)
    {
        target.SimpleStrikeNPC(target.lifeMax / 200, hit.HitDirection, false, 0f, null, false, 0f, false);

        if (!HasHitTarget)
        {
            Projectile.velocity *= 0.8f;
            HasHitTarget = true;
            Projectile.netUpdate = true;
        }
    }

    public override bool PreDraw(ref Color lightColor) => false;

    public float WidthFunct(float c)
    {
        float baseWidth = InverseLerp(0f, 6f, Projectile.timeLeft) * Projectile.height * 2.5f * Projectile.scale;
        return InverseLerp(0f, 1f, c) * baseWidth * InverseLerp(1f, 0f, c);
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        float distortionStrength = Projectile.Opacity * 11f;

        // Unfortunately, no amount of encoding or decoding with transforming direction ranges will ever get past
        // normalizations done by shaders and their functions since they are all 0 - 1, and we cant add in a parameter
        // since that will apply to the projectiles globally D:
        // The only solutions left seem more complex than whats necessary to pull off the effect, so this is what we got
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
        return new Color(Projectile.Opacity, dir.X, dir.Y, distortionStrength);
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(20);
    public void DrawToTarget()
    {
        if (trail == null || points == null) 
            return;
        
        ManagedShader slashShader = ShaderRegistry.FadedStreak;
        slashShader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1);
        trail.DrawTrail(slashShader, points.Points, 200, true);
    }
}
