using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

public class Razorflake : ProjOwnedByNPC<AuroraGuard>
{
    public bool HitGround
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Time => ref Projectile.ai[2];
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Snowflake);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 26;
        Projectile.alpha = 55;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 300;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override void SafeAI()
    {
        if (Projectile.ai[0] == 0f)
        {
            after ??= new(15, () => Projectile.Center);
            Projectile.frame = Main.rand.Next(0, 4);
            Projectile.ai[0] = 1f;
        }

        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.width * 1.1f, 1.5f, Projectile.rotation, SpriteEffects.None, 40, 2, 0, null, true, 0f));

        Projectile.Opacity = GetLerpBump(0f, 30f, 300f, 290f, Projectile.timeLeft);
        Projectile.VelocityBasedRotation(.02f);

        if (Projectile.velocity.Y < 16f && !HitGround)
            Projectile.velocity.Y += .2f;

        Lighting.AddLight(Projectile.Center, new Vector3(0.75f, 0.85f, 1.4f) * Projectile.Opacity);

        if (Collision.SolidCollision(Projectile.Center, 4, 4) && Time > 10 && !HitGround)
        {
            if (Projectile.timeLeft > 60)
                Projectile.timeLeft = 60;
            SoundID.Item51.Play(Projectile.Center, .7f, .1f, .1f, null, 40);
            Projectile.velocity *= 0f;
            HitGround = true;
        }

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnDustParticle(Projectile.RotHitbox().RandomPoint(), Main.rand.NextVector2Circular(5f, 5f), Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, .7f), AuroraGuard.SlateBlue, .1f, true, true);
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        int frameHeight = tex.Height / Main.projFrames[Projectile.type];
        int frameY = frameHeight * Projectile.frame;
        Rectangle frame = new(0, frameY, tex.Width, frameHeight);

        void draw()
        {
            after.DrawFancyAfterimages(AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall), [AuroraGuard.DeepBlue, Color.DeepSkyBlue, Color.SkyBlue], Projectile.Opacity, 1f, 0f, true);
        }
        PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);

        Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Vector2.One * Projectile.width), frame, Color.White * Projectile.Opacity, Projectile.rotation, frame.Size() / 2);

        return false;
    }
}