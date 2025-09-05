using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class TheTechnicBlitzripper : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TechnicBlitzripper);
    public override void SetDefaults()
    {
        Projectile.width = 210;
        Projectile.height = 44;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public ref float Recoil => ref Projectile.ai[2];
    public ref float Heat => ref Projectile.Additions().ExtraAI[0];
    public ref float ShootDelay => ref Projectile.Additions().ExtraAI[1];

    public int Dir => Projectile.velocity.X.NonZeroSign();
    public Vector2 Tip => Projectile.Center + PolarVector(105f, Projectile.rotation) + PolarVector(3f * Dir, Projectile.rotation - MathHelper.PiOver2);

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void SafeAI()
    {
        if (Boss.TechnicBombBarrage_FadeTimer > 0)
        {
            Projectile.Opacity = InverseLerp(30f, 0f, Boss.TechnicBombBarrage_FadeTimer);
            if (Projectile.Opacity <= 0f)
                Projectile.Kill();
        }
        else
            Projectile.Opacity = Animators.MakePoly(2f).OutFunction(InverseLerp(0f, 30f, Time));

        Vector2 offset = PolarVector(30f - (Recoil * 4), Projectile.rotation) + PolarVector(10f * Dir, Projectile.rotation - MathHelper.PiOver2);
        Projectile.Center = Boss.RightHandPosition + offset;
        Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.Center.SafeDirectionTo(Boss.ReticlePosition), .2f);
        Projectile.rotation = Projectile.velocity.ToRotation();

        Heat = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Heat, -.11f, .04f), 0f, TechnicBlitzripperProj.MaxHeat);
        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.25f, .095f), 0f, 40f);

        if (ShootDelay > 0f)
            ShootDelay--;

        Time++;
    }

    public void Shoot()
    {
        if (ShootDelay <= 0f)
        {
            SpawnProjectile(Tip, Projectile.velocity.SafeNormalize(Vector2.Zero) * 12f, ModContent.ProjectileType<TheLightripBullet>(), Asterlin.LightAttackDamage, 0f);

            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnGlowParticle(Tip, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(6, 12), Main.rand.NextFloat(30f, 60f), Color.LightCyan, 1.4f);

            for (int i = 0; i < 8; i++)
            {
                ParticleRegistry.SpawnTechyHolosquareParticle(Tip, Projectile.velocity.RotatedByRandom(.54f) * Main.rand.NextFloat(7f, 14f), Main.rand.Next(50, 90),
                    Main.rand.NextFloat(.7f, 1.5f), Color.Cyan, Main.rand.NextFloat(.8f, 1.1f), Main.rand.NextFloat(1.3f, 1.8f));
                ParticleRegistry.SpawnBloomLineParticle(Tip, Projectile.velocity.RotatedByRandom(.6f) * Main.rand.NextFloat(12f, 22f),
                    Main.rand.Next(10, 12), Main.rand.NextFloat(.3f, .5f), Color.Cyan);
                ParticleRegistry.SpawnMistParticle(Tip, Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(7f, 10f),
                    Main.rand.NextFloat(.4f, .6f), Color.Cyan, Color.DarkCyan, Main.rand.NextFloat(50f, 180f));
            }

            AdditionsSound.banditShot1B.Play(Tip, .85f, 0f, .1f, 20, Name);

            Heat = MathHelper.Clamp(Heat + 1, 0f, TechnicBlitzripperProj.MaxHeat);
            Recoil = 4f;
            ShootDelay = 4f;
        }
    }

    public override bool PreDraw(ref Color lightColor) => false;

    public void DrawGun()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Vector2 origin = texture.Size() * .5f;
        SpriteEffects effects = Dir == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, null, Color.White.Lerp(Color.Cyan, 1f - Projectile.Opacity) * Projectile.Opacity, Projectile.rotation, texture.Size() / 2f, Projectile.scale, effects);

        float comp = MathHelper.Lerp(0f, .7f, InverseLerp(0f, TechnicBlitzripperProj.MaxHeat, Heat));
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.TechnicBlitzripperHeat);
        for (int i = 0; i < 8; i++)
        {
            Vector2 off = (MathHelper.TwoPi * i / 8).ToRotationVector2() * 5f * comp;
            Main.spriteBatch.DrawBetter(glow, Projectile.Center + off, null, Color.Cyan with { A = 0 } * comp * .8f, Projectile.rotation, glow.Size() / 2, Projectile.scale, effects);
        }

        Texture2D invis = AssetRegistry.InvisTex;
        const float sightsSize = 300f;
        float sightsResolution = 2f;
        Color color = Color.Cyan * Projectile.Opacity;

        Vector2 top = Projectile.Center + PolarVector(-12f, Projectile.rotation) + PolarVector(13f * Dir, Projectile.rotation - MathHelper.PiOver2);

        ManagedShader scope = ShaderRegistry.PixelatedSightLine;
        scope.TrySetParameter("noiseOffset", Main.GameUpdateCount * -0.003f);
        scope.TrySetParameter("mainOpacity", 1f);
        scope.TrySetParameter("resolution", new Vector2(sightsResolution * sightsSize));
        scope.TrySetParameter("rotation", -Projectile.rotation);
        float sine = Sin01(Main.GlobalTimeWrappedHourly * 2f) * .005f;
        scope.TrySetParameter("width", 0.0025f + sine);
        scope.TrySetParameter("lightStrength", 3f);
        scope.TrySetParameter("color", color.ToVector3());
        scope.TrySetParameter("darkerColor", Color.Black.ToVector3());
        scope.TrySetParameter("bloomSize", 0.29f - sine);
        scope.TrySetParameter("bloomMaxOpacity", 0.4f);
        scope.TrySetParameter("bloomFadeStrength", 7f);

        Main.spriteBatch.EnterShaderRegion(BlendState.Additive, scope.Effect);

        Main.EntitySpriteDraw(invis, top - Main.screenPosition, null, Color.White, 0f, invis.Size() * .5f, sightsSize * Projectile.Opacity, 0, 0f);

        Main.spriteBatch.ExitShaderRegion();
    }
}