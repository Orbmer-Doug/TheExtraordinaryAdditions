using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.AuroraTurret;

public class GlacialShell : ProjOwnedByNPC<AuroraGuard>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlacialShell);
    public override void SetDefaults()
    {
        Projectile.width = 12;
        Projectile.height = 38;
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.timeLeft = 360;
        Projectile.scale = 1f;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        for (int i = 0; i < 10; i++)
        {
            ParticleRegistry.SpawnDustParticle(Projectile.RotHitbox().RandomPoint(), Projectile.velocity * Main.rand.NextFloat(.2f, .4f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .6f), Color.WhiteSmoke, Main.rand.NextFloat(-.1f, .1f));
        }
        SoundID.Item51.Play(Projectile.Center, .8f, .14f, .05f, null, 10);
        Projectile.Kill();
    }

    public bool HitGround
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float Time => ref Projectile.ai[1];
    public override void SafeAI()
    {
        after ??= new(4, () => Projectile.Center);
        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 10, 3, 3f));

        Projectile.Opacity = InverseLerp(0f, 10f, Time) * InverseLerp(0f, 20f, Projectile.timeLeft);
        Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * Projectile.scale * .4f);

        if (Main.rand.NextBool(15))
            ParticleRegistry.SpawnBloomPixelParticle(Projectile.RotHitbox().RandomPoint(), -Projectile.velocity * Main.rand.NextFloat(.1f, .2f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), Color.Cyan, Color.DeepSkyBlue, null, 1.2f);

        if (!HitGround)
        {
            Projectile.FacingUp();
            if (SolidCollisionFix(Projectile.Center, 10, 10))
            {
                SoundID.Item50.Play(Projectile.Center, 1.1f, -.1f, .1f);
                Collision.HitTiles(Projectile.Center, -Projectile.velocity, 10, 10);
                HitGround = true;
            }
        }
        else
        {
            Projectile.velocity = Vector2.Zero;
        }

        Time++;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        void glow()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Projectile.Size * 1.65f), null, Color.LightCyan, Projectile.rotation, tex.Size() / 2);
            Main.spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Projectile.Size * 1.85f), null, Color.DarkBlue, Projectile.rotation, tex.Size() / 2);
        }
        PixelationSystem.QueueTextureRenderAction(glow, PixelationLayer.UnderProjectiles, BlendState.Additive);

        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [new(14, 32, 168)], Projectile.Opacity, Projectile.scale);
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
