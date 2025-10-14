using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain.Projectiles;

public class SanguinePortal : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.height = Projectile.width = 80;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.hostile = Projectile.friendly = false;
    }

    public ref float Time => ref Projectile.ai[0];
    public override void SafeAI()
    {
        Projectile.Opacity = Projectile.scale = InverseLerp(0f, 20f, Time) * 1.5f;
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        if (Owner == null)
            return;

        int ringCount = DifficultyBasedValue(2, 3, 3, 4, 4, 5);
        int eyeCount = DifficultyBasedValue(14, 16, 18, 20, 22, 24);
        float eyeShootSpeed = DifficultyBasedValue(12f, 14f, 16f, 18f, 20f, 22f);

        if (ModOwner.HasDonePhase2Drama)
            ringCount += 1;

        float angleOffset = 0f; // Start with no offset for the first ring
        float rand = RandomRotation();

        for (int i = 0; i < ringCount; i++)
        {
            for (int j = 0; j < eyeCount; j++)
            {
                float angle = angleOffset + (MathHelper.TwoPi * InverseLerp(0f, eyeCount, j) + rand);
                Vector2 vel = angle.ToRotationVector2() * MathHelper.Lerp(eyeShootSpeed, 9f, i / (float)(ringCount - 1f));
                if (i % 2 == 0)
                    vel = vel.RotatedBy(Main.rand.NextFloat(-.1f, .1f));

                if (this.RunServer())
                {
                    SpawnProjectile(Projectile.Center, vel, ModContent.ProjectileType<WrithingEyeball>(), StygainHeart.BloodshotDamage, 0f, 0f, 0f, ai2: 1);
                }
            }
            angleOffset += MathHelper.Pi / eyeCount;
        }

        ParticleRegistry.SpawnPulseRingParticle(Projectile.Center, Vector2.Zero, 30, 0f, Vector2.One, 0f, 700f, Color.Crimson, true);
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool PreDraw(ref Color lightColor)
    {
        ManagedShader portal = ShaderRegistry.PortalShader;
        Color col1 = ColorSwap(Color.Crimson, Color.DarkRed * 2f, 1f);
        Color col2 = Color.Crimson * 1.5f;

        portal.TrySetParameter("opacity", Projectile.Opacity);
        portal.TrySetParameter("color", col1);
        portal.TrySetParameter("secondColor", col2);
        portal.TrySetParameter("globalTime", Projectile.scale * 1.2f);

        PixelationSystem.QueueTextureRenderAction(Draw, PixelationLayer.UnderProjectiles, null, portal);
        return false;
    }

    public void Draw()
    {
        Texture2D noiseTexture = AssetRegistry.GetTexture(AdditionsTexture.FractalNoise);
        Vector2 origin = noiseTexture.Size() * 0.5f;
        Vector2 diskScale = Projectile.scale * Vector2.One;
        Main.spriteBatch.DrawBetter(noiseTexture, Projectile.Center, null, Color.White, Projectile.rotation, origin, diskScale, SpriteEffects.None);
    }
}
