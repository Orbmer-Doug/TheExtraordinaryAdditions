using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class DartBomb : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    // To help with easing the dart animations
    public struct VisualDart(Vector2 center, float rotation, float opacity, int time)
    {
        public Vector2 Center = center;
        public float Rotation = rotation;
        public float Opacity = opacity;
        public int Time = time;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.friendly = Projectile.tileCollide = false;
        Projectile.hostile = Projectile.ignoreWater = true;
        Projectile.timeLeft = 5000;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float InitialRotation => ref Projectile.ai[1];

    public List<VisualDart> DartList = [];
    public const int TotalDarts = 8;
    public static int BuildTime => DifficultyBasedValue(45, 40, 35, 30, 25, 15);
    public static int PullTime => DifficultyBasedValue(20, 18, 16, 14, 12, 8);
    public static int TotalTime => BuildTime + PullTime;
    public static int DartBuildRate => BuildTime / TotalDarts;
    public float Progress => InverseLerp(0f, TotalTime, Time);
    public float PullProgress => InverseLerp(BuildTime, TotalTime, Time);
    public float DartDistance => Animators.MakePoly(3.1f).OutFunction.Evaluate(150f, 40f, Progress);

    public override void SafeAI()
    {
        if (Time == 0)
        {
            InitialRotation = RandomRotation();
            Projectile.netUpdate = true;
        }

        // Explode darts outward once done
        if (Progress >= 1f)
        {
            AdditionsSound.banditShot2A.Play(Projectile.Center, .9f, 0f, .1f, 40, Name);
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 30, 0f, Vector2.One, 0f, 212f, Color.Cyan);
            ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 35, 0f, Vector2.One, 0f, 262f, Color.Cyan * .3f);
            if (this.RunServer())
            {
                for (int i = 0; i < DartList.Count; i++)
                {
                    VisualDart dart = DartList[i];
                    Main.projectile[SpawnProjectile(dart.Center, dart.Rotation.ToRotationVector2() * 2.5f,
                        ModContent.ProjectileType<GodPiercingDart>(), Asterlin.LightAttackDamage, 0f)].As<GodPiercingDart>().ExtendedTelegraph = true;
                }
            }

            Projectile.Kill();
            return;
        }
        Projectile.velocity *= .95f;

        // Add new ones if necessary
        if (Time % DartBuildRate == (DartBuildRate - 1) && DartList.Count < TotalDarts)
        {
            DartList.Add(new(Projectile.Center, InitialRotation, 0f, 0));
        }

        // Update all darts
        for (int i = 0; i < DartList.Count; i++)
        {
            VisualDart dart = DartList[i];

            float targetRotation = i * MathHelper.TwoPi / TotalDarts;
            dart.Rotation = dart.Rotation.AngleLerp(MathHelper.WrapAngle(targetRotation + InitialRotation), .2f);
            Vector2 newPosition = Projectile.Center + PolarVector(DartDistance, dart.Rotation);
            float opacity = (InverseLerp(0f, DartBuildRate * 2, dart.Time));

            DartList[i] = new VisualDart(newPosition, dart.Rotation, opacity, dart.Time + 1);
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D dart = AssetRegistry.GetTexture(AdditionsTexture.GodPiercingDart);
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.Glow);

        // Draw darts
        for (int i = 0; i < DartList.Count; i++)
        {
            VisualDart visual = DartList[i];
            Main.spriteBatch.DrawBetter(dart, visual.Center, null, Color.White * visual.Opacity, visual.Rotation + MathHelper.PiOver2, dart.Size() / 2, 1f * visual.Opacity);
        }

        // Draw main bomb core
        void draw()
        {
            float opac = Animators.BezierEase(InverseLerp(0f, 18f, Time));
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(Projectile.Center, new(30f * opac)), null, Color.LightCyan * opac, 0f, glow.Size() / 2);
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(Projectile.Center, new(60f)), null, Color.Cyan * .75f * opac, 0f, glow.Size() / 2);
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(Projectile.Center, new(90f)), null, Color.DarkCyan * .4f * opac, 0f, glow.Size() / 2);
            Main.spriteBatch.DrawBetterRect(glow, ToTarget(Projectile.Center, new(120f)), null, Color.DarkCyan * .2f * opac, 0f, glow.Size() / 2);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }
}