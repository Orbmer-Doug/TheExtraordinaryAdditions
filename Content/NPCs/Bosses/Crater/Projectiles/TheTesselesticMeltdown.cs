using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;
using TheExtraordinaryAdditions.Core.DataStructures;
using static TheExtraordinaryAdditions.Content.Projectiles.Magic.Late.TesselesticMeltdownProj;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class TheTesselesticMeltdown : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TesselesticMeltdown);

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 176;
        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 9999;
        Projectile.netImportant = true;
    }

    public RotatedRectangle Rect()
    {
        return new(36, Projectile.Center, Projectile.Center + PolarVector(TesselesticMeltdownProj.StaffLength, Projectile.rotation - MathHelper.PiOver4));
    }
    public Vector2 TipOfStaff => Rect().Top;
    public int Dir => ModOwner.Direction;

    public State CurrentState
    {
        get => (State)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public ref float StateTime => ref Projectile.ai[1];
    public ref float OverallTime => ref Projectile.ai[2];
    public LoopedSoundInstance slot;

    public override void SafeAI()
    {
        if (ModOwner.Tesselestic_FadeTime <= 0f)
        {
            Projectile.Opacity = InverseLerp(0f, 20f, OverallTime);
        }
        else
        {
            Projectile.Opacity = InverseLerp(Asterlin.Tesselestic_FadeDuration, 0f, ModOwner.Tesselestic_FadeTime);
            if (Projectile.Opacity <= 0f)
            {
                Projectile.Kill();
                return;
            }
        }

        switch (CurrentState)
        {
            case State.Idle:
                Behavior_Idle();
                break;
            case State.Barrage:
                Behavior_Hold();
                break;
        }

        slot ??= LoopedSoundManager.CreateNew(new(AdditionsSound.ElectricityContinuous, () => .67f),
            () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => CurrentState == State.Barrage && ModOwner.Tesselestic_Shooting);
        slot?.Update(Projectile.Center);

        Projectile.timeLeft = 2;
        OverallTime++;
        Projectile.Center = Projectile.Center.ClampInWorld();
    }

    private void Behavior_Idle()
    {
        Vector2 pos = ModOwner.RotatedHitbox.Center + Vector2.UnitX * (140f * -ModOwner.Direction) + Vector2.UnitY * 22f * MathF.Sin(Main.GlobalTimeWrappedHourly);
        Projectile.velocity = Vector2.UnitX * -ModOwner.Direction;
        Projectile.Center = Vector2.Lerp(Projectile.Center, pos + Vector2.UnitY * StaffLength / 2, .6f);
        Projectile.rotation = Projectile.rotation.AngleLerp(-MathHelper.PiOver4, .1f);

        if (OverallTime % 4f == 3f)
            ParticleRegistry.SpawnLightningArcParticle(Rect().RandomPoint(), Main.rand.NextVector2Circular(100f, 100f), Main.rand.Next(12, 20), .6f, Color.Cyan);

        if (StateTime != 0f)
        {
            StateTime = 0f;
            this.Sync();
        }
    }

    private void Behavior_Hold()
    {
        Vector2 dir = -Vector2.UnitY;
        float amt = Utils.Remap(Projectile.velocity.Distance(dir), 0f, 1f, .1f, .2f);
        Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, dir, amt);

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Projectile.Center = Vector2.Lerp(Projectile.Center, ModOwner.RightHandPosition - Projectile.velocity * StaffLength / 2, Utils.Remap(StateTime, 0f, 50f, .02f, .9f));
        StateTime++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        bool flip = Dir == -1;

        Vector2 origin;
        float off;
        SpriteEffects fx;
        if (flip)
        {
            origin = new Vector2(0, tex.Height);

            off = 0;
            fx = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(tex.Width, tex.Height);

            off = MathHelper.PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }
        Texture2D glow = AssetRegistry.GetTexture(AdditionsTexture.TesselesticMeltdown_Glowmask);

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, Color.White * Projectile.Opacity,
            Projectile.rotation + off, origin, Projectile.scale, fx, 0f);
        return false;
    }
}