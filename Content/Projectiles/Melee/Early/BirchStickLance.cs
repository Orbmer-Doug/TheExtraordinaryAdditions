using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;

public class BirchStickLance : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BirchStick);

    public const int MaxUpdates = 3;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 20;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.netImportant = true;

        Projectile.width = 106;
        Projectile.height = 98;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.scale = 1f;
        Projectile.ownerHitCheck = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.noEnchantmentVisuals = true;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public float AngularVelocity => MathF.Abs(Projectile.rotation - OldRot);
    public enum BirchStickState
    {
        BashUp,
        BashDown,
        Poke
    }
    public BirchStickState State
    {
        get => (BirchStickState)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public ref float Time => ref Projectile.ai[1];
    public ref float TimeStop => ref Projectile.ai[2];
    public ref float Counter => ref Projectile.AdditionsInfo().ExtraAI[0];
    public bool StruckTile
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    public bool Stabbing
    {
        get => Projectile.AdditionsInfo().ExtraAI[2] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }
    public bool Initialized
    {
        get => Projectile.AdditionsInfo().ExtraAI[3] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[3] = value.ToInt();
    }
    public bool PlayedSound
    {
        get => Projectile.AdditionsInfo().ExtraAI[4] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[4] = value.ToInt();
    }

    public const int MaxStopTime = 5;
    public const int StopTime = MaxStopTime * MaxUpdates;

    public ref float VanishTime => ref Projectile.AdditionsInfo().ExtraAI[5];
    public ref float TotalTime => ref Projectile.AdditionsInfo().ExtraAI[6];
    public ref float OldRot => ref Projectile.AdditionsInfo().ExtraAI[7];

    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public Vector2 SwordDir => (Projectile.rotation - MathHelper.PiOver4 + MathHelper.PiOver2).ToRotationVector2() * (State != BirchStickState.BashUp).ToDirectionInt() * Direction;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!Initialized)
        {
            if (State != BirchStickState.Poke)
                Projectile.MaxUpdates = MaxUpdates;

            cache?.Clear();
            PlayedSound = false;
            if (this.RunLocal())
                Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            Time = 0f;
            Initialized = true;
            this.Sync();
        }
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 20);

        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemAnimation = Owner.itemTime = 2;
        Owner.itemRotation = Projectile.rotation;
        Projectile.timeLeft = 1000;
        if (State != BirchStickState.Poke)
            Offset = PolarVector(Projectile.height * .7f, Projectile.rotation - MathHelper.PiOver4);

        Projectile.Center = State switch
        {
            BirchStickState.Poke => Owner.MountedCenter + Offset.RotatedBy(Projectile.rotation + MathHelper.PiOver4) * Projectile.scale,
            _ => Owner.GetFrontHandPositionImproved() + Offset,
        };

        if (VanishTime > 0f)
        {
            Projectile.Opacity = MakePoly(3).OutFunction.Evaluate(VanishTime, 0f, 25f * MaxUpdates, 1f, 0f);
            if (Projectile.Opacity <= 0f)
                Projectile.Kill();

            VanishTime++;
            return;
        }

        if (SwingCompletion >= 1f && State != BirchStickState.Poke)
        {
            if (this.RunLocal() && Modded.MouseLeft.Current)
            {
                if (State == BirchStickState.BashUp)
                    State = BirchStickState.BashDown;
                else
                    State = BirchStickState.BashUp;

                Initialized = false;
                Projectile.ResetLocalNPCHitImmunity();
                this.Sync();
                return;
            }
            else
            {
                VanishTime++;
            }
        }

        switch (State)
        {
            case BirchStickState.BashUp:
                DoBash();
                break;
            case BirchStickState.BashDown:
                DoBash();
                break;
            case BirchStickState.Poke:
                DoPoke();
                break;
        }

        if (TimeStop > 0f)
            TimeStop--;

        TotalTime++;
    }

    public float SwingTime => SecondsToFrames(1.2f) / (int)Owner.GetTotalAttackSpeed(Projectile.DamageType);
    public float SwingCompletion => InverseLerp(0f, SwingTime, Time);
    public int Direction => Projectile.velocity.X.NonZeroSign();
    public const float ReelPercent = .3f;
    public const float SwingPercent = .85f;

    public float SwingAmt()
    {
        return MathHelper.PiOver4 + MathHelper.PiOver2 * (new PiecewiseCurve()
            .Add(-1.2f, -1.3f, ReelPercent, MakePoly(1.5f).OutFunction) // Reel
            .Add(-1.3f, 1.2f, SwingPercent, MakePoly(3.5f).InFunction) // Swing
            .Add(1.2f, 1.2f, 1f, MakePoly(2).OutFunction) // End-Swing
            .Evaluate(SwingCompletion) * (State != BirchStickState.BashUp).ToDirectionInt()) * Direction;
    }

    private void DoBash()
    {
        if (SwingCompletion > ReelPercent && !PlayedSound)
        {
            SoundEngine.PlaySound(SoundID.Item1 with { PitchVariance = .3f, Volume = Main.rand.NextFloat(.9f, 1.3f) }, Projectile.Center);
            PlayedSound = true;
            this.Sync();
        }

        cache ??= new(20);
        if (TimeStop <= 0)
        {
            OldRot = Projectile.rotation;
            cache.Update(GetRect().Bottom.Lerp(GetRect().Top, .6f) - Center);
            Time++;
        }

        Projectile.Opacity = MakePoly(3).InOutFunction.Evaluate(TotalTime, 0f, 12f * MaxUpdates, 0f, 1f);
        Projectile.rotation = Projectile.velocity.ToRotation() + SwingAmt();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver4);
    }

    private void DoPoke()
    {
        if ((this.RunLocal() && !Modded.MouseRight.Current) && Time >= 50f && !Stabbing)
        {
            Stabbing = true;
            this.Sync();
        }
        if ((this.RunLocal() && Modded.MouseRight.Current) && !Stabbing)
        {
            float completion = Circ.InFunction(InverseLerp(0f, 50f, Time));
            Offset = Vector2.Lerp(new(0f, -60f), new(0f, 10f), completion);
            Projectile.timeLeft = 200;

            Projectile.scale = InverseLerp(0f, 20f, Time);
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        else if (Stabbing)
        {
            if (Counter == 0f && !StruckTile)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1.2f, Pitch = -.3f }, Projectile.Center);

            float completion = Circ.OutFunction(InverseLerp(0f, 14f, Counter));
            Offset = Vector2.Lerp(new(0f, 10f), new(0f, -90f), completion);

            if (!StruckTile)
                Counter++;
            if (StruckTile)
                Counter -= .5f;

            Vector2 pos = Projectile.RotHitbox().TopRight;
            if (Collision.SolidCollision(pos, 8, 8) && !StruckTile && Counter > 4f)
            {
                Owner.velocity += -Projectile.velocity.SafeNormalize(Vector2.Zero) * 6.2f;
                SoundEngine.PlaySound(SoundID.Dig, pos);
                Collision.HitTiles(pos, -Projectile.velocity * 2, 28, 28);

                StruckTile = true;
            }
            if (Projectile.numHits > 0)
                StruckTile = true;

            Projectile.scale = StruckTile ? InverseLerp(0f, 8f, Counter) : 1f - InverseLerp(15f, 20f, Counter);

            if ((StruckTile && Counter <= 0f) || (!StruckTile && Counter > 20f))
                Projectile.Kill();
            this.Sync();
        }
        else if ((this.RunLocal() && !Modded.MouseRight.Current) && !Stabbing)
        {
            Projectile.Kill();
        }

        Projectile.timeLeft = 2;
        Owner.itemTime = Owner.itemAnimation = 2;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() + (.5f * Projectile.direction));
        Time++;
    }

    public override bool CanHitPvp(Player target)
    {
        if (State != BirchStickState.Poke)
            return SwingCompletion.BetweenNum(ReelPercent, SwingPercent);
        else
            return Stabbing;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        ItemLoader.OnHitPvp(Owner.HeldItem, Owner, target, info);
    }

    public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
    {
        modifiers.Knockback *= 0f;
    }

    public RotatedRectangle GetRect()
    {
        float scale = Projectile.scale;
        Vector2 a = Projectile.RotHitbox().BottomLeft;
        Vector2 b = Projectile.RotHitbox().TopRight;
        float width = 6f * scale;
        float y = a.Distance(b) * scale;

        return new RotatedRectangle(Vector2.Lerp(a, b, .5f) - Vector2.UnitY * y / 2, new(width, y), Projectile.rotation + MathHelper.PiOver4);
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (State != BirchStickState.Poke)
            return SwingCompletion.BetweenNum(ReelPercent, SwingPercent) ? null : false;
        else
            return Stabbing ? null : false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        ItemLoader.OnHitNPC(Owner.HeldItem, Owner, target, hit, damageDone);
        NPCLoader.OnHitByItem(target, Owner, Owner.HeldItem, hit, damageDone);
        PlayerLoader.OnHitNPC(Owner, target, hit, damageDone);

        if (State != BirchStickState.Poke)
        {
            RotatedRectangle rect = GetRect();
            bool metal = !target.IsFleshy();

            HitEffects(metal, CheckLinearCollision(GetRect().Bottom, GetRect().Top, target.Hitbox, out Vector2 start, out _) ? start : target.Center);

            target.velocity += GetRect().Bottom.SafeDirectionTo(GetRect().Top) * Projectile.knockBack * target.knockBackResist;
        }
        else
        {
            if (!target.boss)
                target.velocity += Projectile.velocity.SafeNormalize(Vector2.Zero) * 6.5f * target.knockBackResist;
            if (Projectile.numHits <= 0)
                Owner.velocity += -Projectile.velocity.SafeNormalize(Vector2.Zero) * 4.8f;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (State == BirchStickState.Poke)
        {
            modifiers.FinalDamage *= 1.8f;
        }
        else
            modifiers.Knockback *= 0f;
    }

    public void HitEffects(bool metal, Vector2 point)
    {
        for (int i = 0; i < 12; i++)
        {
            float scale = Main.rand.NextFloat(.4f, 1f);
            Dust.NewDustPerfect(point, DustID.WoodFurniture, SwordDir.RotatedByRandom(.24f) * Main.rand.NextFloat(2f, 5f), 0, default, scale);
        }

        TimeStop = StopTime;
        this.Sync();
        ScreenShakeSystem.New(new(.1f, .2f), point);
        SoundEngine.PlaySound(SoundID.Dig with { PitchVariance = .3f, Volume = Main.rand.NextFloat(.9f, 1.2f), MaxInstances = 30, Pitch = metal ? .12f : -.1f }, point);
    }

    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Offset);
        writer.Write(Projectile.rotation);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Offset = reader.ReadVector2();
        Projectile.rotation = reader.ReadSingle();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), GetRect().Top, GetRect().Bottom, GetRect().Width, ref _);
    }

    private Color ColorFunct(SystemVector2 c, Vector2 position) => Color.SaddleBrown * MathHelper.SmoothStep(1f, 0f, c.X) * InverseLerp(0.026f, 0.1f, AngularVelocity);
    private float WidthFunct(float c) => Projectile.width;
    public OptimizedPrimitiveTrail trail;
    private TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        if (State != BirchStickState.Poke && trail != null && cache != null)
        {
            trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, cache.Points, 100, false, false);
        }

        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, lightColor * Projectile.Opacity, Projectile.rotation, orig, Projectile.scale, 0);
        return false;
    }
}