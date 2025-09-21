using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class BloodBeacon : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.Invis;

    public static readonly int Lifetime = SecondsToFrames(15);
    public const int MaxLaserLength = 3000;

    public ref float Time => ref Projectile.ai[0];
    public ref float LaserLength => ref Projectile.ai[1];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = MaxLaserLength * 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 270;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public Vector2 Start => Owner.Center - Vector2.UnitY * MaxLaserLength;
    public Vector2 End => Start + Vector2.UnitY * LaserLength;
    public float Completion => Owner.AdditionsInfo().ExtraAI[2];
    public LoopedSoundInstance sound;
    public override void SafeAI()
    {
        sound ??= LoopedSoundManager.CreateNew(new(AdditionsSound.BraveMediumFireLoop, () => Completion, () => -.1f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        sound?.Update(Projectile.Center);

        if (trail == null || trail._disposed)
            trail = new(WidthFunction, ColorFunction, null, 24);
        if (trail2 == null || trail2._disposed)
            trail2 = new(AltWidthFunction, AltColorFunction, null, 24);

        // Make the laser expand outward.
        LaserLength = Animators.MakePoly(3f).InOutFunction.Evaluate(0f, MaxLaserLength * 2, Completion);
        ScreenShakeSystem.New(new(.4f, .2f, 5000f), Projectile.Center);
        Projectile.Center = Owner.Center;
        if (Boss.CurrentState == StygainHeart.StygainAttackType.BloodBeacon)
            Projectile.timeLeft = 2;
        cache.SetPoints(Start.GetLaserControlPoints(End, 24));

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Start, End, Projectile.width * Projectile.scale);
    }

    public float WidthFunction(float _) => Projectile.width * 2f;

    public static Color ColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        float colorInterpolant = 0.5f * MathF.Sin(-9f * Main.GlobalTimeWrappedHourly) + 0.5f;
        return Color.Lerp(Color.DarkRed, Color.Black, 0.25f * colorInterpolant);
    }
    public float AltWidthFunction(float _) => WidthFunction(_) * 2f;
    public static Color AltColorFunction(SystemVector2 completionRatio, Vector2 position) => ColorFunction(completionRatio, position) * .4f;

    public ManualTrailPoints cache = new(24);
    public OptimizedPrimitiveTrail trail;
    public OptimizedPrimitiveTrail trail2;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || trail2 == null || trail._disposed || trail2._disposed || cache == null)
                return;

            ManagedShader shader = ShaderRegistry.BloodBeacon;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin), 2);

            trail.DrawTrail(shader, cache.Points, 80);

            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1);
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.WarpMap), 2);

            trail2.DrawTrail(shader, cache.Points, 80);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);
        return false;
    }
}