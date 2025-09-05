using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class FallingPillar : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FallingPillar);
    private Vector2 saveTarget;

    public ref float Time => ref Projectile.ai[0];

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
    }

    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 336;
        Projectile.tileCollide = false;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.penetrate = -1;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 700;
    }

    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(TelegraphWidthFunction, TelegraphColorFunction, null, 40);

        Projectile.rotation = MathHelper.PiOver2;

        if (Time < 1f)
        {
            Vector2 target = new(Projectile.Center.X, Target.Center.Y);
            saveTarget = RaytraceTiles(Projectile.Center, target, false, Projectile.width) ?? target;
            Projectile.velocity.Y = 0f;
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, (Target.Center.X - Projectile.Center.X) * 0.05f, 0.01f * Utils.GetLerpValue(0f, -25f, Time, true));
            Projectile.velocity.X *= 0.98f * Utils.GetLerpValue(0f, -25f, Time, true);

            // If much higher the pillar despawns if the player is at the top of the world
            Projectile.position.Y = saveTarget.Y - 900f + Projectile.height / 2;
        }
        else
        {
            Projectile.velocity.X = 0f;
        }
        if (Time > 0f)
        {
            Projectile.Center = Vector2.Lerp(Projectile.Center, saveTarget, 0.001f + Utils.GetLerpValue(0f, 10f, Time, true));
        }
        if (Time == 8f)
        {
            ScreenShakeSystem.New(new(.4f, .5f), Projectile.Center);
            AdditionsSound.RockBreak.Play(Projectile.Center, 1.2f, 0f, .14f, 30, Name);
        }
        if (Time > 20f)
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 pos = Projectile.RandAreaInEntity();
                ParticleRegistry.SpawnMistParticle(pos, Main.rand.NextVector2CircularEdge(3f, 3f), Main.rand.NextFloat(.3f, .9f), new(61, 46, 58), Color.Transparent, Main.rand.NextByte(220, 255));
            }
            Projectile.Kill();
        }
        Time++;

        teleCache.SetPoints(Projectile.RotHitbox().Bottom.GetLaserControlPoints(Projectile.RotHitbox().Bottom * Vector2.UnitY * 2000f, 40));
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Time > 2f && Time < 12f)
        {
            Rectangle val = new(projHitbox.X, projHitbox.Y - 336, projHitbox.Width, projHitbox.Height + 336);
            return val.Intersects(targetHitbox);
        }
        return false;
    }

    public float TelegraphWidthFunction(float completionRatio)
    {
        return (float)Math.Pow(Utils.GetLerpValue(0f, 15f, Time, true) * Utils.GetLerpValue(5f, -60f, Time, true), 0.6) * Projectile.width;
    }

    public Color TelegraphColorFunction(SystemVector2 completionRatio, Vector2 position)
    {
        return Color.LightSlateGray * InverseLerp(20f, 0f, Time) * GetLerpBump(0f, .2f, 1f, .8f, completionRatio.X);
    }

    public ManualTrailPoints teleCache = new(40);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader shader = ShaderRegistry.SideStreakTrail;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.Perlin), 1);
            trail.DrawTrail(shader, teleCache.Points, 40);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D texture = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Projectile.GetAlpha(Color.White), Projectile.rotation - MathHelper.PiOver2, texture.Size() * 0.5f, Projectile.scale);
        return false;
    }
}

public class PillarFalling : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.FallingPillar);

    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 182;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.aiStyle = -1;
        Projectile.timeLeft = 700;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }
    
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public static int TelegraphTime => DifficultyBasedValue(60, 50, 45, 40, 35, 30);
    public static int SmashTime => 18;

    public Vector2 SmashTarget;
    public override void SendAI(BinaryWriter writer)
    {
        writer.WriteVector2(SmashTarget);
    }
    public override void ReceiveAI(BinaryReader reader)
    {
        SmashTarget = reader.ReadVector2();
    }

    public override void SafeAI()
    {
        if (tele == null || tele._disposed)
            tele = new(WidthFunct, ColorFunct, null, 40);

        if (Time < TelegraphTime)
        {
            float comp = 1f - InverseLerp(0f, TelegraphTime, Time);
            Vector2 target = new(Projectile.Center.X, Target.Center.Y + MathHelper.Clamp(Target.velocity.Y * 10f, 0f, 450f) + (Projectile.height / 2));
            Vector2? tile = RaytraceTiles(Projectile.Center, target, false);
            if (tile.HasValue)
                SmashTarget = tile.Value;
            else
                SmashTarget = target;

            Projectile.velocity.Y = 0f;
            Projectile.velocity.X = MathHelper.Lerp(Projectile.velocity.X, (Target.Center.X - Projectile.Center.X) * 0.05f, 0.01f * comp);
            Projectile.velocity.X *= 0.98f * comp;
            Projectile.Opacity = 1f;//Animators.BezierEase(InverseLerp(0f, 5f, Time));

            // If much higher the pillar despawns if the player is at the top of the world
            Projectile.position.Y = SmashTarget.Y - 600f + Projectile.height / 2;
        }
        else if (Time < (TelegraphTime + SmashTime))
        {
            Projectile.velocity.X = 0f;
            Projectile.Center = Vector2.Lerp(Projectile.Center, SmashTarget, Animators.MakePoly(3f).InFunction(InverseLerp(TelegraphTime, (TelegraphTime + SmashTime), Time)));
        }
        else if (Time == (TelegraphTime + SmashTime))
        {
            for (int i = 0; i < 50; i++)
            {
                Vector2 pos = Projectile.RandAreaInEntity();
                ParticleRegistry.SpawnMistParticle(pos, Main.rand.NextVector2CircularEdge(3f, 3f), Main.rand.NextFloat(.3f, .9f), new(61, 46, 58), Color.Transparent, Main.rand.NextByte(220, 255));
            }

            ScreenShakeSystem.New(new(.4f, .5f), Projectile.Center);
            AdditionsSound.RockBreak.Play(Projectile.Center, 1.2f, 0f, .14f, 30, Name);
            Projectile.Kill();
        }

        points.SetPoints(Projectile.RotHitbox().Bottom.GetLaserControlPoints(Projectile.RotHitbox().Bottom + Vector2.UnitY * Animators.MakePoly(3f).InOutFunction.Evaluate(Time, 10f, 20f, 0f, 900f), 40));
        Time++;
    }

    public float WidthFunct(float c) => Projectile.width;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color.Tan * MathHelper.SmoothStep(1f, 0f, c.X) * InverseLerp(TelegraphTime, TelegraphTime - 15f, Time) * .5f;

    public ManualTrailPoints points = new(40);
    public OptimizedPrimitiveTrail tele;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (points == null || tele == null)
                return;

            tele.DrawTrail(ShaderRegistry.StandardPrimitiveShader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Texture2D tex = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Projectile.GetAlpha(Color.White), 0f, tex.Size() / 2f, Projectile.scale);
        return false;
    }
}