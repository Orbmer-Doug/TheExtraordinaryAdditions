using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Utilities.Utility;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class WaveSiphon : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SmolBoll);
    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 22;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = 120;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 1;
    }

    public Player Owner => Main.player[Projectile.owner];
    public bool TileDeath
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float Time => ref Projectile.ai[1];
    public override void AI()
    {
        if (Time == 0f)
        {
            Projectile.timeLeft = Main.rand.Next(90, 120);
        }
        if (Time > 10f && NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 400, true), out NPC target))
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 12f, .2f);
        }
        else
            Projectile.velocity *= .996f;

        Utility.ProjAntiClump(Projectile, .3f);
        Projectile.FacingUp();
        Time++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, ClampToCardinalDirection(Projectile.velocity).ToRotation() + MathHelper.PiOver2, ParticleRegistry.CrosscodeBollType.DieWallBig, CrossDiscHoldout.Element.Wave);
        TileDeath = true;
        return true;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.numHits <= 0)
        {
            if (!TileDeath)
                ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, 0f, ParticleRegistry.CrosscodeBollType.Die, CrossDiscHoldout.Element.Wave);
            AdditionsSound.crosscodeBallDie.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AdditionsSound.WaveHitSmall.Play(Projectile.Center, .6f, 0f, 0f, 20, Name);
        ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Small, CrossDiscHoldout.Element.Wave);
        Owner.Heal(Main.rand.Next(3, 5));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
        Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.White * .5f, 0f, bloom.Size() * .5f, .3f, 0);
        Main.spriteBatch.ResetBlendState();

        Texture2D tex = Projectile.ThisProjectileTexture();

        Rectangle framed = tex.Frame(1, 5, 0, 4);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, framed, Color.White, Projectile.rotation, framed.Size() / 2, 1f);
        return false;
    }
}
