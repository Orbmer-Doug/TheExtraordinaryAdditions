using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class FrostyIcicle : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Pixel);

    public override void SetDefaults()
    {
        Projectile.penetrate = 1;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.timeLeft = 500;
        Projectile.friendly = true;
        Projectile.hostile = false;
    }

    public ref float Time => ref Projectile.ai[0];
    public Player Owner => Main.player[Projectile.owner];
    public Vector2 Offset;
    public const int Wait = 30;
    public Vector2[] InitialLocalPoints;
    public override void AI()
    {
        if (Time == 0f)
        {
            Projectile.width = Main.rand.Next(20, 30);
            Projectile.height = Main.rand.Next(50, 65);
            List<Vector2> bolt = GetBoltPoints(Projectile.Center, Projectile.Center + Vector2.UnitY * Projectile.height, 20f, 2f);
            iciclePoints.SetPoints(bolt);

            // Store initial local offsets relative to Projectile.Center
            InitialLocalPoints = new Vector2[bolt.Count];
            for (int i = 0; i < bolt.Count; i++)
                InitialLocalPoints[i] = bolt[i] - Projectile.Center;
            this.Sync();
        }

        if (Time < Wait)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Additions().mouseWorld), .2f);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + Offset;
        }
        else if (Time == Wait)
        {
            Projectile.velocity *= 12f;
            this.Sync();
        }
        else if (Time > Wait)
        {
            if (Main.rand.NextBool())
                ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * .1f, Main.rand.Next(30, 40), Main.rand.NextFloat(.2f, .4f), Color.SlateBlue, Color.DeepSkyBlue, null, .4f);
            if (Time > (Wait + 10) && Projectile.velocity.Y < 20f)
                Projectile.velocity.Y += .2f;
        }

        // Rotate points with velocity
        float rotation = Projectile.velocity.ToRotation();
        Projectile.rotation = rotation - MathHelper.PiOver2;
        if (InitialLocalPoints != null)
        {
            for (int i = 0; i < iciclePoints.Count; i++)
            {
                Vector2 rotatedOffset = InitialLocalPoints[i].RotatedBy(rotation - MathHelper.PiOver2);
                iciclePoints.SetPoint(i, Projectile.Center + rotatedOffset);
            }
        }

        // Update points with velocity
        for (int i = 0; i < iciclePoints.Count; i++)
            iciclePoints.SetPoint(i, iciclePoints.Points[i] + Projectile.velocity);

        if (icicle == null || icicle._disposed)
            icicle = new(c => Projectile.width * (1f - c), (c, pos) => Color.White, null, 50);

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        SoundID.Item27.Play(Projectile.Center, 1.1f, 0f, .1f, null, 20, Name);
        for (int i = 0; i < 20; i++)
        {
            Dust.NewDustPerfect(Projectile.BaseRotHitbox().RandomPoint(), DustID.FrostHydra, Projectile.velocity * Main.rand.NextFloat(.1f, .2f), 0, default, Main.rand.NextFloat(.7f, 1.1f)).noGravity = true;
        }
    }

    public OptimizedPrimitiveTrail icicle;
    public ManualTrailPoints iciclePoints = new(50);
    public override bool PreDraw(ref Color lightColor)
    {
        if (icicle == null || icicle._disposed || iciclePoints == null)
            return false;

        ManagedShader shader = AssetRegistry.GetShader("TextureStretch");
        shader.TrySetParameter("completion", 1f - InverseLerp(0f, Wait, Time));
        shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DiscIceProjectile), 0, SamplerState.PointClamp);

        icicle.DrawTrail(shader, iciclePoints.Points, 100, true, false);

        return false;
    }
}