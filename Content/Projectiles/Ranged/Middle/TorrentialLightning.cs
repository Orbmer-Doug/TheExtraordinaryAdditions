using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using Utils = Terraria.Utils;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class TorrentialLightning : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public const int Life = 60;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Life;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Power => ref Projectile.ai[1];
    public float Completion => MakePoly(6).OutFunction(InverseLerp(0f, Life, Time));

    private List<List<Line>> Branches;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        if (Time == 0f)
        {
            Vector2 end = LaserCollision(Projectile.Center,
                Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) *
                MathHelper.Max(700f, 1500f * Power), CollisionTarget.Tiles);
            Branches = CreateLightningBranch(Projectile.Center, end, (int) Utils.MultiLerp(Power, 0, 1, 2, 3, 4, 5),
                MathHelper.Max(.5f, Power * 2f));
        }

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity is > 0f and .05f)
            Projectile.Kill();

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (List<Line> list in Branches)
        {
            foreach (Line line in list)
            {
                int width = 8;
                if (new Rectangle((int) line.A.X - width / 2, (int) line.A.Y - width / 2, width, width).Intersects(
                        targetHitbox))
                    return true;
            }
        }

        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Branches == null || Branches.Count == 0)
            return false;

        foreach (List<Line> list in Branches)
        {
            foreach (Line line in list)
            {
                line.DrawPixelated(PixelationLayer.OverNPCs, BlendState.Additive,
                    MulticolorLerp(Completion, Color.White, Color.LightCyan, Color.Cyan, Color.DarkCyan)
                    * Projectile.Opacity);
            }
        }

        return false;
    }
}
