using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class SeethingRockball : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SeethingRockball);

    public override void SetDefaults()
    {
        Projectile.width = 52;
        Projectile.height = 54;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public ref float Timer => ref Projectile.ai[0];
    public bool HitGround
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float GroundTimer => ref Projectile.ai[2];

    public override void SafeAI()
    {
        if (Timer > 10f)
            Projectile.tileCollide = true;

        if (HitGround)
        {
            ParticleRegistry.SpawnCloudParticle(Projectile.RotHitbox().RandomPoint(), -Vector2.UnitY.RotatedByRandom(.2f) * Main.rand.NextFloat(1f, 3f),
                Color.OrangeRed, Color.DarkGray, Main.rand.Next(20, 40), Main.rand.NextFloat(20f, 40f), Main.rand.NextFloat(.4f, .6f));
            Projectile.Opacity = InverseLerp(0f, 30f, Projectile.timeLeft);
            GroundTimer++;
        }
        else
        {
            Color col = Color.Lerp(Color.OrangeRed, Color.Goldenrod, Main.rand.NextFloat(.2f, 1f));
            ParticleRegistry.SpawnBloomLineParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * .1f, 20, Main.rand.NextFloat(.3f, .4f), col);
        }
        Lighting.AddLight(Projectile.Center, (Color.Lerp(Color.OrangeRed, Color.White, 1f - InverseLerp(0f, 60f, GroundTimer))).ToVector3() * Projectile.Opacity * .5f);

        if (Timer > 18f && !HitGround)
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .4f, -50f, 32f);
        }
        Projectile.VelocityBasedRotation();

        after ??= new(10, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 0, 2f, null, false, .2f));
        Timer++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
        {
            for (int i = 0; i < 30; i++)
            {
                float completion = InverseLerp(0f, 30, i);
                Vector2 norm = oldVelocity.SafeNormalize(Vector2.UnitX);
                Vector2 dir = new(-norm.Y, norm.X);
                Vector2 vel = dir * MathHelper.Lerp(-8f, 8f, completion);
                if (vel == Vector2.Zero)
                    vel = dir * 2f;

                int life = (int)MathHelper.Lerp(30, 50, Convert01To010(completion));
                float scale = (int)MathHelper.Lerp(.5f, 2f, Convert01To010(completion));
                ParticleRegistry.SpawnGlowParticle(Projectile.Center + oldVelocity * 2f, vel, life, scale * 122f, Color.OrangeRed);
            }

            SoundID.DD2_ExplosiveTrapExplode.Play(Projectile.Center, .9f, -.4f);
            Projectile.Center += oldVelocity * Main.rand.NextFloat(1f, 2f);
            if (Projectile.timeLeft > 120)
                Projectile.timeLeft = 120;
            HitGround = true;
            Projectile.netUpdate = true;
        }

        Projectile.velocity *= 0f;
        return false;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        float inter = 1f - InverseLerp(0f, 60f, GroundTimer);
        Color col = Color.Lerp(Color.OrangeRed, Color.White, inter) * Projectile.Opacity;
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);

        if (inter > 0f)
        {
            void draw()
            {
                Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
                float inter = 1f - InverseLerp(0f, 60f, GroundTimer);
                Color col = Color.OrangeRed * inter * .5f;
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(260f)), null, col * .6f, 0f, tex.Size() / 2f);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(230f)), null, col.Lerp(Color.White * inter, .3f) * .8f, 0f, tex.Size() / 2f);
                Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, new(150f)), null, col.Lerp(Color.White * inter, .6f), 0f, tex.Size() / 2f);
            }
            PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.UnderProjectiles, BlendState.Additive);
        }

        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [col], inter);
        Projectile.DrawBaseProjectile(col);
        Projectile.DrawProjectileBackglow(col * inter, inter * 4f, 0, 20);

        return false;
    }
}
