using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Lightning;

public class LightningVolt : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LightningVolt);

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = 64;
        Projectile.height = 20;
        Projectile.timeLeft = 300;
        Projectile.penetrate = 5;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.aiStyle = 0;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.friendly);
        writer.Write(Projectile.hostile);
        writer.Write(Projectile.penetrate);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.friendly = reader.ReadBoolean();
        Projectile.hostile = reader.ReadBoolean();
        Projectile.penetrate = reader.ReadInt32();
    }

    public ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);

        Projectile.Opacity = InverseLerp(0f, 10f, Time);
        if (Projectile.timeLeft > 40)
            Projectile.SetAnimation(2, 7);
        else if (Projectile.timeLeft > 10)
            Projectile.frame = 2;
        else if (Projectile.timeLeft > 0)
            Projectile.frame = 3;

        after.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 0, 0, Projectile.ThisProjectileTexture().Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame)));

        if (Projectile.velocity.Length() < 64f)
            Projectile.velocity *= 1.01f;

        Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * 1f);
        Projectile.FacingRight();
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        OnHit();
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        OnHit();
    }

    public void OnHit()
    {
        for (int i = 0; i < 9; i++)
        {
            ParticleRegistry.SpawnSparkParticle(Projectile.BaseRotHitbox().Right, Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.4f)
                * Main.rand.NextFloat(4f, 12f), Main.rand.Next(30, 40), Main.rand.NextFloat(.5f, .8f), Color.Pink);
        }
        SoundID.NPCHit53.Play(Projectile.Center, 1f, 0f, .1f);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Texture2D glowmask = AssetRegistry.GetTexture(AdditionsTexture.LightningVolt_Glowmask);
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;

        after?.DrawFancyAfterimages(texture, [Color.Pink, Color.Violet], Projectile.Opacity);

        // Draw the base sprite and glowmask.
        Main.EntitySpriteDraw(texture, drawPosition, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
        Main.EntitySpriteDraw(glowmask, drawPosition, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, 0, 0);
        return false;
    }
}