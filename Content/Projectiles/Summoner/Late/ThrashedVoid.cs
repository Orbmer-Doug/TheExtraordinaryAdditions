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
        if (Trail == null || Trail.Disposed)
            Trail = new(TrailWidth, TrailColor, null, Samples);

        points.Update(Tip);
    }

    public override void CrackEffects()
    {
        if (this.RunLocal())
            Projectile.NewProj(Tip, Vector2.Zero, ModContent.ProjectileType<VoidBlast>(), Projectile.damage / 4,
                Projectile.knockBack, Owner.whoAmI);
        AssetRegistry.GennedSounds.commandoBlast2.Play(Tip, 1.1f, -.1f, .1f);
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        Projectile.damage = (int) (Projectile.damage * .85f);
        for (int i = 0; i < 20; i++)
        {
            ShaderParticleRegistry.SpawnCosmicParticle(pos + Main.rand.NextVector2Circular(5f, 5f), vel.RotatedByRandom(.5f) * Main.rand.NextFloat(1f, 5f), new Vector2(10f, 50f));
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
