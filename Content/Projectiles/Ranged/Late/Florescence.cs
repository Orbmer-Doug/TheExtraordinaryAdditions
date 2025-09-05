using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Interfaces;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class Florescence : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Florescence);

    public Player Owner => Main.player[Projectile.owner];
    public ref float Timer => ref Projectile.ai[0];
    public int HomingCooldown
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.timeLeft = 170;
        Projectile.penetrate = -1;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.aiStyle = 0;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 2;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() * .5f;
        
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.DeepPink, Color.Pink], Projectile.Opacity);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White, Projectile.rotation, orig, Projectile.scale, 0);
        void star()
        {
            Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            Vector2 starOrig = star.Size() * .5f;
            Vector2 pos = Projectile.Center - Main.screenPosition;
            float interpolant = InverseLerp(0f, 8f, Timer);
            Color col = Color.Lerp(Color.Pink, Color.LightPink * 1.2f, interpolant) * interpolant;
            Vector2 scale = new(interpolant * .28f);
            Main.EntitySpriteDraw(star, pos, null, col, Projectile.rotation, starOrig, scale, 0);
        }
        PixelationSystem.QueueTextureRenderAction(star, PixelationLayer.OverProjectiles, BlendState.Additive);

        return false;
    }

    public override void AI()
    {
        after ??= new(4, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 100, 2, 3f, null, false, .2f));

        Timer++;
        if (HomingCooldown > 0)
        {
            HomingCooldown--;
        }
        else if (Timer > 8f)
        {
            if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 800, true, true), out NPC target))
            {
                if (Projectile.velocity.Length() < 40f)
                    Projectile.velocity += target.velocity * .5f;

                Projectile.timeLeft += (int)(target.velocity.Length() * .33f);

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 22f, .8f).
                    RotatedBy(InverseGoldenRatio * (Projectile.identity % 2f == 1f).ToDirectionInt());
            }
        }

        Projectile.rotation += Projectile.direction * .3f;
        if (Projectile.damage <= 1)
            Projectile.Kill();
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (HomingCooldown <= 0)
        {
            return null;
        }
        return false;
    }

    public override bool CanHitPvp(Player target)
    {
        return HomingCooldown <= 0;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        HomingCooldown = 25;
        Projectile.velocity = Projectile.velocity.RotatedBy(InverseGoldenRatio) * -InverseGoldenRatio;

        Projectile.damage = (int)(Projectile.damage * .35f);
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 18; i++)
        {
            ParticleRegistry.SpawnGlowParticle(Projectile.RandAreaInEntity(), Projectile.velocity * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(14, 20), Projectile.width * Main.rand.NextFloat(.3f, .5f), Color.Pink);

            Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.Plantera_Pink, Main.rand.NextVector2Circular(4f, 4f), 0, default, Main.rand.NextFloat(.4f, .9f)).noGravity = true;
        }
    }
}