using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public class SanguineAssimilation : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 30;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.ignoreWater = true;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public bool NotFree
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Time => ref Projectile.ai[2];
    public ref float Rot => ref Projectile.Additions().ExtraAI[0];
    public ref float SavedDistance => ref Projectile.Additions().ExtraAI[1];
    public ref float BeamTimer => ref Projectile.Additions().ExtraAI[2];
    public ref float Dir => ref Projectile.Additions().ExtraAI[3];
    public ref float SpinSpeed => ref Projectile.Additions().ExtraAI[4];

    public override bool? CanDamage()
    {
        if (BeamTimer > 0f)
            return null;
        return false;
    }
    public override bool ShouldUpdatePosition()
    {
        if (BeamTimer > 0f)
            return false;
        return true;
    }

    public const int TimeForBeam = 95;
    public const int Lifetime = TimeForBeam + 30;
    private void GetPoints(out Vector2 start, out Vector2 end)
    {
        float interpolant = Utils.Remap(BeamTimer, 0f, 15f, 0f, 1f);
        start = Projectile.Center;
        end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * SavedDistance * interpolant * 2;
    }

    public override void SafeAI()
    {
        if (Owner == null || Owner.active == false)
        {
            Projectile.Kill();
            return;
        }

        if (Time == 0f)
        {
            for (int i = 0; i < 5; i++)
            {
                Vector2 squish = new(Main.rand.NextFloat(.6f, 1f), 1f);
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, squish * (150f + (i + 20 * i)), Vector2.Zero, 18 + (i * 3), Color.DarkRed.Lerp(Color.Crimson, Main.rand.NextFloat(.4f, .6f)));
                ParticleRegistry.SpawnCloudParticle(Projectile.Center, Main.rand.NextVector2Circular(3f, 3f), Color.Crimson, Color.DarkRed, 40, Main.rand.NextFloat(.3f, .5f), .6f, 1);
            }
            SpinSpeed = Main.rand.NextFloat(.12f, .34f);
            Projectile.netUpdate = true;
        }

        GetPoints(out Vector2 start, out Vector2 end);

        basePoints ??= new(40);
        if (Time < TimeForBeam && !NotFree)
        {
            float move = Animators.MakePoly(2).OutFunction(InverseLerp(0f, TimeForBeam - 30f, Time));
            Projectile.Center = Vector2.Lerp(Projectile.Center, Target.Center + PolarVector(500f, Rot), move);

            float interpolant = Animators.MakePoly(4).OutFunction(InverseLerp(0f, TimeForBeam, Time));
            Rot = (Rot + (SpinSpeed * (1f - interpolant) * Dir)) % MathHelper.TwoPi;

            Projectile.velocity = Projectile.SafeDirectionTo(Target.Center);

            Vector2 start2 = Projectile.Center;
            Vector2 end2 = start + Projectile.SafeDirectionTo(Target.Center).SafeNormalize(Vector2.Zero) * (1200f * InverseLerp(0f, TimeForBeam / 2, Time));

            basePoints.SetPoints(start2.GetLaserControlPoints(end2, 40));
        }
        else if (Time == TimeForBeam)
        {
            AssetRegistry.GetSound(AdditionsSound.etherealHit3).Play(Projectile.Center, .6f, -.4f, .1f, new(-.4f, 0f), 4);
            SavedDistance = start.Distance(Target.Center);
            Projectile.netUpdate = true;
        }
        else if (Time > TimeForBeam)
        {
            BeamTimer++;
            Vector2 veloc = end.SafeDirectionTo(start).RotatedByRandom(.45f) * Main.rand.NextFloat(4f, 16f);
            Color color = Color.Lerp(Color.Crimson, Color.IndianRed, Main.rand.NextFloat(.1f, 1f)) * OpacityInterpolant;
            ParticleRegistry.SpawnSparkParticle(end, veloc, Main.rand.Next(15, 25), Main.rand.NextFloat(.3f, .9f), color);

            basePoints.SetPoints(start.GetLaserControlPoints(end, 40));
        }

        if (Time > Lifetime)
            Projectile.Kill();

        Time++;
    }

    public float OpacityInterpolant => 1f - InverseLerp(20f, 30f, BeamTimer);
    public float WidthFunction(float c)
    {
        return MathHelper.SmoothStep(Projectile.width, 0f, c);
    }
    public Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        float colorInterpolant = Sin01(-9f * Main.GlobalTimeWrappedHourly);
        return Color.Lerp(Color.Crimson, Color.DarkRed, 0.55f * colorInterpolant) * OpacityInterpolant;
    }
    public float TeleCompletion => InverseLerp(0f, TimeForBeam - 10f, Time);
    public float TeleWidthFunction(float c)
    {
        return MathHelper.SmoothStep(Projectile.height + 4f, 0f, c) * (1f - TeleCompletion);
    }
    public Color TeleColorFunction(SystemVector2 c, Vector2 position)
    {
        Color col = MulticolorLerp(Cos01(c.X + Main.GlobalTimeWrappedHourly * 4f), Color.MediumVioletRed, Color.Crimson * 2f, Color.IndianRed * 1.4f);
        col *= 1f - TeleCompletion;
        col *= .3f;
        col *= GetLerpBump(.05f, .1f, 1f, .9f, c.X);
        return col;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(basePoints.Points, WidthFunction);
    }

    public ManualTrailPoints basePoints;
    public override bool PreDraw(ref Color lightColor)
    {
        if (Owner == null || Owner.active == false)
            return false;

        void draw()
        {
            if (BeamTimer > 0f)
            {
                ManagedShader shader = ShaderRegistry.BloodBeacon;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 1);
                OptimizedPrimitiveTrail trail = new(WidthFunction, ColorFunction, null, 5);
                trail.DrawTrail(shader, basePoints.Points, 30);
            }
            else if (!NotFree)
            {
                ManagedShader shader = ShaderRegistry.SideStreakTrail;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1);
                OptimizedPrimitiveTrail trail = new(TeleWidthFunction, TeleColorFunction, null, 5);
                trail.DrawTrail(shader, basePoints.Points, 30);
            }
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        void flare()
        {
            if (BeamTimer > 0f)
            {
                GetPoints(out Vector2 start, out Vector2 end);

                for (float i = 0f; i < .2f; i += .05f)
                {
                    Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.BloomFlare);
                    Vector2 pos = end - Main.screenPosition;
                    Vector2 orig = bloom.Size() * .5f;
                    Color col = Color.DarkRed * 1.4f * OpacityInterpolant;
                    float rot = Main.GlobalTimeWrappedHourly * 5f * (i % .05f == .04f).ToDirectionInt();
                    float scale = .2f - i;
                    Main.EntitySpriteDraw(bloom, pos, null, col, rot, orig, scale * .3f, 0);
                }
            }
        }
        LayeredDrawSystem.QueueDrawAction(flare, PixelationLayer.OverPlayers, BlendState.Additive);

        Main.spriteBatch.PrepareForShaders();
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Vector2 pos = Projectile.Center - Main.screenPosition;
        Vector2 scale = Projectile.Size * 2.4f / pixel.Size() * OpacityInterpolant;
        Color color = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly), Color.Crimson, Color.Crimson * 1.5f, Color.Crimson.Lerp(Color.DarkRed, .5f)) * OpacityInterpolant;

        ManagedShader sphere = ShaderRegistry.MagicSphere;
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FractalNoise), 1, SamplerState.LinearWrap);
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise), 2, SamplerState.LinearWrap);
        sphere.TrySetParameter("resolution", new Vector2(400, 400));
        sphere.TrySetParameter("posterizationPrecision", 20f);
        sphere.TrySetParameter("mainColor", color.ToVector3());
        sphere.Render();

        Main.spriteBatch.Draw(pixel, pos, null, Color.White, 0f, pixel.Size() / 2, scale * 1.2f, 0, 0f);
        Main.spriteBatch.ResetToDefault();

        return false;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        StygainHeart.ApplyLifesteal(Projectile, target, info.Damage);
    }

    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnTwinkleParticle(Projectile.Center, Vector2.Zero, 30, new(Main.rand.NextFloat(1.4f, 2f)), Color.Crimson, 4);
    }
}
