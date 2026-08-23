using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class ThrashedVoid : BaseWhip
{
    public override string Texture => AssetRegistry.GennedTextures.ThrashedVoid.Path;

    public override void Defaults()
    {
        Projectile.Size = new(620, 400);
    }

    public override int SegmentSkip => 4;

    public override void SafeAI()
    {
        if (Time == 0)
        {
            if (this.RunLocal())
            {
                int x = (int) MathHelper.Clamp((int) Projectile.Center.Distance(Modded.MouseWorld), 100, 700);
                Projectile.Size = new(x, (int) Utils.Remap(x, 100, 700, 80, 250));
                this.Sync();
            }
        }

        if (Trail == null || Trail.Disposed)
            Trail = new(TrailWidth, TrailColor, null, Samples);

        points.Update(Tip);
    }

    public override void CrackEffects()
    {
        if (this.RunLocal())
            Projectile.CreateProj(Tip, Vector2.Zero, ModContent.ProjectileType<VoidBlast>(), Projectile.damage / 4,
                Projectile.knockBack, Owner.whoAmI);
        AssetRegistry.GennedSounds.commandoBlast2.Play(Tip, 1.1f, -.1f, .1f);
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        Projectile.damage = (int) (Projectile.damage * .85f);
        for (int i = 0; i < 20; i++)
        {
            ShaderParticleRegistry.SpawnCosmicParticle(pos + Main.rand.NextVector2Circular(5f, 5f),
                vel.RotatedByRandom(.5f) * Main.rand.NextFloat(1f, 5f), new Vector2(10f, 50f));
        }
    }

    public override Color LineColor(SystemVector2 completion, Vector2 position)
    {
        return Color.BlueViolet;
    }

    public override float LineWidth(float completion)
    {
        return 8f * MathHelper.SmoothStep(1f, .5f, completion);
    }

    public static Color TrailColor(SystemVector2 completion, Vector2 position)
    {
        return new Color(21 + (int) (10 * completion.X), 3, 51 + (int) (10 * completion.X)) * completion.X;
    }

    public static float TrailWidth(float completion)
    {
        return MathHelper.SmoothStep(1f, 0f, completion) * 35f;
    }

    public Trail Trail;
    public TrailPoints points = new(30);

    public override void DrawLine()
    {
        if (Trail != null)
        {
            ManagedShader shader = AssetRegistry.GennedShaders.EnlightenedBeam;
            shader.TrySetParameter("time", Main.GameUpdateCount * .02f);
            shader.TrySetParameter("repeats", 12f);
            shader.SetTexture(AssetRegistry.GennedTextures.StreakLightning, 1, SamplerState.LinearWrap);
            shader.SetTexture(AssetRegistry.GennedTextures.FractalNoise, 2, SamplerState.LinearWrap);
            Trail.DrawTrail(shader, points.Points);
        }

        if (Line != null)
        {
            ManagedShader fire = AssetRegistry.GennedShaders.SpecialLightningTrail;
            fire.SetTexture(AssetRegistry.GennedTextures.TurbulentNoise2, 1);
            Line.DrawTrail(fire, WhipPoints.Points);
        }
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 14, 26);
        Rectangle seg1Frame = new(0, 26, 14, 18);
        Rectangle seg2Frame = new(0, 44, 14, 16);
        Rectangle seg3Frame = new(0, 60, 14, 16);
        Rectangle tipFrame = new(0, 76, 14, 18);

        int len = WhipPoints.Points.Length - 1;
        for (int i = 0; i < len; i++)
        {
            Vector2 pos = WhipPoints.Points[i];
            Vector2 next = WhipPoints.Points[i + 1];

            Rectangle frame;
            bool hilt = i == 0;
            bool tip = i == len - 1;
            bool shouldDraw = i % SegmentSkip == (SegmentSkip - 1);
            if (hilt || tip)
                shouldDraw = true;

            if (hilt)
                frame = hiltFrame;
            else if (i < (len / 3))
                frame = seg1Frame;
            else if (i < (len / 2))
                frame = seg2Frame;
            else if (i < (len - 1))
                frame = seg3Frame;
            else
                frame = tipFrame;

            if (shouldDraw)
            {
                Vector3 light = Lighting.GetSubLight(pos);
                Color color = Projectile.GetAlpha(new(light.X, light.Y, light.Z));
                float rotation = (next - pos).ToRotation() - MathHelper.PiOver2;
                Vector2 orig = frame.Size() / 2;
                SpriteEffects flip = Owner.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

                Main.spriteBatch.DrawBetter(texture, pos, frame, color, rotation, orig, tip ? Projectile.scale : 1f,
                    flip);
            }
        }
    }
}

public class VoidBlast : ModProjectile
{
    private const int Lifetime = 35;
    public float Completion => InverseLerp(0f, Lifetime, Time);
    public float Radius => MakePoly(4).OutFunction.Evaluate(0f, 120f, Completion);
    public ref float Time => ref Projectile.ai[0];
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            for (int i = 0; i < 100; i++)
            {
                Vector2 veloc = Main.rand.NextVector2Circular(10f, 10f) + Main.rand.NextVector2Circular(10f, 10f);
                ShaderParticleRegistry.SpawnCosmicParticle(Projectile.Center, veloc / 2,
                    new Vector2(50f + Main.rand.NextFloat(-20f, 20f), 50f + Main.rand.NextFloat(-20f, 20f)));
            }

            ParticleRegistry.SpawnChromaticAberration(Projectile.Center, Lifetime, 1.24f, 250f);
            for (int i = 0; i < 25; i++)
                ParticleRegistry.SpawnMistParticle(Projectile.Center, Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextFloat(.7f, 1.4f), Color.Violet, Color.DarkViolet, Main.rand.NextFloat(150f, 220f),
                    Main.rand.NextFloat(-.1f, .1f));
        }

        Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularEdge(Radius, Radius);
        int life = Main.rand.Next(30, 40);
        float scale = Main.rand.NextFloat(.4f, .8f);
        Color col = MulticolorLerp(Completion, Color.White.Lerp(Color.Violet, .5f), Color.Violet, Color.DarkViolet,
            Color.Black) * MathHelper.Lerp(2f, .5f, Completion);
        Vector2 vel = Projectile.Center.SafeDirectionTo(pos) * 11f * Completion;

        ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life * 3, scale * 3f, col, Color.White, 8);

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Radius / 2f, targetHitbox);
    }
}
