using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class DisintegrationBeam : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 60;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public enum BeamState
    {
        Telegraphing,
        Vaporizing,
        Fading,
    }

    public static int TelegraphTime => DifficultyBasedValue(50, 45, 45, 40, 40, 40);
    public static int BeamTime => DifficultyBasedValue(40, 36, 33, 30, 28, 25);
    public static int FadeTime => DifficultyBasedValue(40, 35, 35, 30, 25, 16);
    public const int MaxLength = 3200;

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public int ProjOwner
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public ref float MaxAngleShift => ref Projectile.ai[2];
    public BeamState CurrentState
    {
        get => (BeamState)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = (int)value;
    }
    public ref float CurrentLength => ref Projectile.AdditionsInfo().ExtraAI[1];
    public bool DontTurn
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }

    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, null, 80);

        Projectile owner = Main.projectile?[ProjOwner] ?? null;
        if (owner != null && owner.active && owner.type == ModContent.ProjectileType<VaporizingStar>())
            Projectile.Center = owner.Center;

        switch (CurrentState)
        {
            case BeamState.Telegraphing:
                float teleComp = InverseLerp(0f, TelegraphTime, Time);
                if (!DontTurn)
                    Projectile.velocity = Projectile.velocity.RotatedBy(MaxAngleShift * Animators.MakePoly(12f).InFunction(Convert01To010(teleComp)));
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.scale = 0f;

                if (teleComp >= 1f)
                {
                    ScreenShakeSystem.New(new(.7f, .6f), Projectile.Center);
                    AdditionsSound.HeavyLaserBlast.Play(Projectile.Center, 2.2f, -.2f, 0f);
                    Time = 0;
                    CurrentState = BeamState.Vaporizing;
                    this.Sync();
                }
                break;
            case BeamState.Vaporizing:
                float vaporComp = InverseLerp(0f, BeamTime, Time);

                CurrentLength = Animators.BezierEase.Evaluate(0f, MaxLength, InverseLerp(0f, 80f, Time));
                Projectile.scale = Animators.MakePoly(3f).OutFunction(InverseLerp(0f, 20f, Time));

                if (vaporComp >= 1f)
                {
                    Time = 0;
                    CurrentState = BeamState.Fading;
                    this.Sync();
                }
                break;
            case BeamState.Fading:
                float fadeComp = InverseLerp(0f, FadeTime, Time);
                Projectile.scale = Animators.Sine.InOutFunction.Evaluate(1f, 0f, fadeComp);

                if (fadeComp >= 1f)
                {
                    Projectile.Kill();
                    return;
                }
                break;
        }
        points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * MaxLength, 80));

        Time++;
    }

    public override bool? CanDamage() => CurrentState == BeamState.Vaporizing ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct);
    }

    public float WidthFunct(float c)
    {
        return MathHelper.Lerp(Projectile.width * .65f, Projectile.width, c) * Projectile.scale;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return MulticolorLerp(c.X, Color.Goldenrod, Color.Orange, Color.DarkOrange);
    }

    public TrailPoints points = new(80);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        if (CurrentState == BeamState.Telegraphing)
        {
            void line()
            {
                Texture2D cap = AssetRegistry.GetTexture(AdditionsTexture.BloomLineCap);
                Texture2D horiz = AssetRegistry.GetTexture(AdditionsTexture.BloomLineHoriz);

                Vector2 a = Projectile.Center;
                Vector2 b = a + Projectile.rotation.ToRotationVector2() * MaxLength;
                float thickness = Animators.BezierEase.Evaluate(0f, 4f, InverseLerp(0f, 15f, Time));
                float opacity = Animators.MakePoly(4f).InOutFunction(InverseLerp(0f, 10f, Time));

                Line small = new(a, b, thickness * .3f);
                small.Draw(Color.PaleGoldenrod * opacity);
                Line medium = new(a, b, thickness);
                medium.Draw(Color.Goldenrod * opacity);
                Line large = new(a, b, thickness * 1.5f);
                large.Draw(Color.DarkGoldenrod * opacity * .7f);
            }
            PixelationSystem.QueueTextureRenderAction(line, PixelationLayer.UnderProjectiles, BlendState.Additive);
        }
        else
        {
            void draw()
            {
                if (points == null || trail == null)
                    return;

                ManagedShader shader = AssetRegistry.GetShader("DisintegrationBeamShader");
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.StreakMagma), 1, SamplerState.AnisotropicWrap);
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.VoronoiShapes), 2, SamplerState.AnisotropicWrap);
                trail.DrawTrail(shader, points.Points);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }
        return false;
    }
}