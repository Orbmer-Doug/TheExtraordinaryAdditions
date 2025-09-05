using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class LightningNimbusSparks : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LightningNimbusSparks);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 3;
    }

    public override void SetDefaults()
    {
        Projectile.width = 28;
        Projectile.height = 14;
        Projectile.timeLeft = 70;
        Projectile.penetrate = 2;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ref int counter = ref Owner.Additions().BrewingStormsCounter;
        counter += Projectile.numHits > 0 ? 1 : 3;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);
        Vector2 orig = frame.Size() / 2;
        Vector2 pos = Projectile.Center - Main.screenPosition;
        Color color = Color.White;

        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.Pink, Color.Violet, Color.DarkViolet], Projectile.Opacity);
        Main.spriteBatch.Draw(tex, pos, frame, color, Projectile.rotation, orig, Projectile.scale, 0, 0f);
        return false;
    }

    private ref float Time => ref Projectile.ai[0];
    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, new(1f, .75f), Projectile.Opacity, Projectile.rotation, 0, 90, 0, 0f, Projectile.ThisProjectileTexture().Frame(1, 3, 0, Projectile.frame), false, -.1f));

        Projectile.Opacity = InverseLerp(0f, 9f, Time);
        Projectile.SetAnimation(3, 6);
        Lighting.AddLight(Projectile.Center, Color.Purple.ToVector3() * .5f * Projectile.Opacity);

        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.velocity *= .975f;
        if (Projectile.numHits > 0)
        {
            float scaleDown = (1f - InverseLerp(0f, 15f, Projectile.ai[1]++)) / 2 + .2f;
            Projectile.scale = scaleDown;
            Projectile.Resize((int)(28 * scaleDown), (int)(14 * scaleDown));
        }
        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Projectile.numHits > 0)
            modifiers.FinalDamage /= 2;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 8; i++)
        {
            Dust.NewDustPerfect(Projectile.Center, DustID.WitherLightning, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(.7f, 1.2f)).noGravity = true;
        }

        SoundEngine.PlaySound(SoundID.NPCHit53, Projectile.Center);
    }
}