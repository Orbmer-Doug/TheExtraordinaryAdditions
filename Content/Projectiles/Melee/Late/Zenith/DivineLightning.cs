using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Zenith;

public class DivineLightning : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
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
    public Vector2 End
    {
        get => new(Projectile.ai[1], Projectile.ai[2]);
        set
        {
            Projectile.ai[1] = value.X;
            Projectile.ai[2] = value.Y;
        }
    }
    public float Completion => Animators.MakePoly(6).OutFunction(InverseLerp(0f, Life, Time));

    private List<List<Line>> Branches = [];
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (Time == 0f)
            Branches = CreateLightningBranch(Projectile.Center, End, Main.rand.Next(1, 6), 2f, Main.rand.NextFloat(.4f, .7f), Main.rand.NextFloat(40f, 80f));

        Projectile.Opacity = 1f - Completion;
        if (Projectile.Opacity.BetweenNum(0f, .05f))
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
                if (new Rectangle((int)line.a.X - width / 2, (int)line.a.Y - width / 2, width, width).Intersects(targetHitbox))
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
                PixelationSystem.QueueTextureRenderAction(() => line.Draw(MulticolorLerp(Completion, Color.White, Color.AntiqueWhite, Color.WhiteSmoke)
                    * Projectile.Opacity), PixelationLayer.OverNPCs, BlendState.Additive);
            }
        }
        return false;
    }
}
