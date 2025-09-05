using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Stygain;

public class BloodDroplet : ProjOwnedByNPC<StygainHeart>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BloodParticle2);

    public override void SetDefaults()
    {
        Projectile.width =
        Projectile.height = 12;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.extraUpdates = 0;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 210;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public override void SafeAI()
    {
        if (!Collision.CanHitLine(Owner.Center, 1, 1, Target.Center, 1, 1))
            Projectile.tileCollide = false;
        else
            Projectile.tileCollide = true;

        Lighting.AddLight(Projectile.Center, Color.DarkRed.ToVector3() * .5f);
        Projectile.FacingUp();
        Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .2f, 0f, 18f);
        if (Projectile.ai[0]++ > 30 && Projectile.velocity.Length() < .05f)
            Projectile.Kill();
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        StygainHeart.ApplyLifesteal(Projectile, target, info.Damage);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        return true;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 10; i++)
        {
            ParticleRegistry.SpawnBloodParticle(Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(1.5f) * Main.rand.NextFloat(2f, 6f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.1f, .3f), Color.DarkRed);
            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.Blood, Projectile.velocity * Main.rand.NextFloat(), 0, default, Main.rand.NextFloat(.5f, 1f));
        }
        SoundEngine.PlaySound(SoundID.SplashWeak with { MaxInstances = 0, Volume = .4f, Pitch = .1f }, Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Color color = Projectile.GetAlpha(Color.Red);
        float squish = MathHelper.Clamp(Projectile.velocity.Length() / 10f * 3f, .1f, 5f);

        Main.EntitySpriteDraw(texture, drawPosition, frame, color, Projectile.rotation, frame.Size() / 2, new Vector2(1f, 1f * squish) * .1f, 0, 0);
        return false;
    }
}
