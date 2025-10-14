using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode.CrossDiscHoldout;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class VRP : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrossCodeBoll);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;

        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.timeLeft = 1200;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.friendly = Projectile.ignoreWater = Projectile.noEnchantmentVisuals = Projectile.usesLocalNPCImmunity = Projectile.tileCollide = true;
        Projectile.hostile = false;
        Projectile.aiStyle = 0;
        Projectile.CritChance = 0;
        Projectile.MaxUpdates = 4;
        Projectile.localNPCHitCooldown = 20;
        Projectile.width = Projectile.height = 1;
    }

    private Element State
    {
        get => (Element)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public ref float Completion => ref Projectile.ai[1];
    public int Bounces
    {
        get => (int)Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }
    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[0];
    public bool TileDeath
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    public bool Charged
    {
        get => Projectile.AdditionsInfo().ExtraAI[2] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }
    public int MaxBounces => Charged ? 4 : 1;

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public override void AI()
    {
        if (State == Element.Neutral)
        {
            Texture2D bigNeutral = AssetRegistry.GetTexture(AdditionsTexture.VRPNeutral);
            after ??= new(5, () => Projectile.Center);
            after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 0, 0, 0, bigNeutral.Frame(1, 4, 0, Projectile.frame), false, 0f));
        }

        Projectile.FacingUp();

        if (Charged && Projectile.FinalExtraUpdate())
        {
            Projectile.SetAnimation(4, 7);

            if (Time % 3 == 2)
            {
                ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, Projectile.rotation, ParticleRegistry.CrosscodeBollType.Trail, State);
            }
        }

        Time++;
    }

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        fallThrough = true;
        return true;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Bounces >= MaxBounces || !Charged)
        {
            ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, ClampToCardinalDirection(oldVelocity).ToRotation() + MathHelper.PiOver2, ParticleRegistry.CrosscodeBollType.DieWallBig, State);
            TileDeath = true;
            Projectile.Kill();
            return false;
        }

        Bounces++;
        ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, ClampToCardinalDirection(oldVelocity).ToRotation() + MathHelper.PiOver2, ParticleRegistry.CrosscodeBollType.DieWallSmall, State);
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;

        Projectile.damage += (int)(Projectile.damage * .25);
        Projectile.CritChance = (int)(InverseLerp(0f, Bounces, MaxBounces) * 100);

        switch (State)
        {
            case Element.Neutral:
                AdditionsSound.NeutralBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Cold:
                AdditionsSound.ColdBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Heat:
                AdditionsSound.HeatBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Shock:
                AdditionsSound.ShockBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Wave:
                AdditionsSound.WaveBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
        }

        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Charged)
        {
            switch (State)
            {
                case Element.Neutral:
                    AdditionsSound.NeutralBallHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Cold:
                    AdditionsSound.ColdHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Heat:
                    AdditionsSound.HeatHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Shock:
                    AdditionsSound.ShockHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Wave:
                    AdditionsSound.WaveHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
            }

            ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Big, State);
        }

        else if (Completion <= .5f)
        {
            switch (State)
            {
                case Element.Neutral:
                    AdditionsSound.NeutralBallHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Cold:
                    AdditionsSound.ColdHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Heat:
                    AdditionsSound.HeatHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Shock:
                    AdditionsSound.ShockHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Wave:
                    AdditionsSound.WaveHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
            }

            ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Small, State);
        }

        else if (Completion <= 1f)
        {
            switch (State)
            {
                case Element.Neutral:
                    AdditionsSound.NeutralBallHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Cold:
                    AdditionsSound.ColdHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Heat:
                    AdditionsSound.HeatHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Shock:
                    AdditionsSound.ShockHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Wave:
                    AdditionsSound.WaveHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
            }

            ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Medium, State);
        }

        switch (State)
        {
            case Element.Neutral:
                break;
            case Element.Cold:
                target.AddBuff(BuffID.Frostburn, SecondsToFrames(3));
                target.AddBuff(BuffID.Frostburn2, SecondsToFrames(3));
                break;
            case Element.Heat:
                target.AddBuff(BuffID.OnFire, SecondsToFrames(3));
                target.AddBuff(BuffID.OnFire3, SecondsToFrames(3));
                break;
            case Element.Shock:
                break;
            case Element.Wave:
                break;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Charged)
            modifiers.FinalDamage *= 1.5f;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.numHits <= 0)
        {
            if (!TileDeath)
                ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, 0f, ParticleRegistry.CrosscodeBollType.Die, State);
            AdditionsSound.crosscodeBallDie.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
        }
    }

    private static readonly Texture2D Bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D bigNeutral = AssetRegistry.GetTexture(AdditionsTexture.VRPNeutral);
        Texture2D bigIce = AssetRegistry.GetTexture(AdditionsTexture.VRPIce);
        Texture2D bigFire = AssetRegistry.GetTexture(AdditionsTexture.VRPFire);
        Texture2D bigShock = AssetRegistry.GetTexture(AdditionsTexture.VRPLightning);
        Texture2D bigWave = AssetRegistry.GetTexture(AdditionsTexture.VRPWave);
        Texture2D smoll = AssetRegistry.GetTexture(AdditionsTexture.SmolBoll);

        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        float rot = Projectile.rotation;
        float sca = Projectile.scale;
        Color col = Projectile.GetAlpha(Color.White);

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Main.EntitySpriteDraw(Bloom, Projectile.Center - Main.screenPosition, null, Color.White * .5f, 0f, Bloom.Size() * .5f, .3f, 0);
        Main.spriteBatch.ResetBlendState();

        if (!Charged)
        {
            int smolFrame = 0;
            switch (State)
            {
                case Element.Neutral:
                    smolFrame = 0;
                    break;
                case Element.Cold:
                    smolFrame = 2;
                    break;
                case Element.Heat:
                    smolFrame = 1;
                    break;
                case Element.Shock:
                    smolFrame = 3;
                    break;
                case Element.Wave:
                    smolFrame = 4;
                    break;
            }
            Rectangle framed = smoll.Frame(1, 5, 0, smolFrame);
            Vector2 orig1 = framed.Size() * .5f;
            Main.EntitySpriteDraw(smoll, drawPos, framed, col, rot, orig1, sca, direction, 0f);
        }

        if (Charged)
        {
            Texture2D tex = bigNeutral;
            switch (State)
            {
                case Element.Neutral:
                    tex = bigNeutral;
                    break;
                case Element.Cold:
                    tex = bigIce;
                    break;
                case Element.Heat:
                    tex = bigFire;
                    break;
                case Element.Shock:
                    tex = bigShock;
                    break;
                case Element.Wave:
                    tex = bigWave;
                    break;
            }
            Rectangle framed = tex.Frame(1, 4, 0, Projectile.frame);
            Vector2 orig1 = framed.Size() * .5f;
            after?.DrawFancyAfterimages(tex, [col * .2f], Projectile.Opacity);
            Main.EntitySpriteDraw(tex, drawPos, framed, col, rot, orig1, sca, direction, 0f);
        }

        return false;
    }
}