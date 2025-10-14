using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;


namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class DecayingCutleryStab : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DecayingCutleryStab);
    public const int StabsNeeded = 3;

    public const int MaxUpdates = 2;

    public const float Scale = 1.5f;
    public ref float Time => ref Projectile.ai[0];

    public int StabCounter
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public bool Init
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public bool Hit
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    public bool Vanishing
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    public ref float VanishTimer => ref Projectile.AdditionsInfo().ExtraAI[2];

    public int StabTime
    {
        get
        {
            return StabCounter switch
            {
                4 => 30 * MaxUpdates,
                _ => 12 * MaxUpdates,
            };
        }
    }

    public float Completion => InverseLerp(0f, StabTime, Time);

    public ref int HitCount => ref Owner.GetModPlayer<DecayingCutleryPlayer>().HitCount;

    public int Dir => Projectile.velocity.X.NonZeroSign();
    public float AngularVel => MathF.Abs(Projectile.rotation - Projectile.oldRot[1]);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 20;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 76;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = -1;
        Projectile.MaxUpdates = MaxUpdates;
    }
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public override void AI()
    {
        if (!Owner.Available())
        {
            Projectile.Kill();
            return;
        }

        if (!Init)
        {
            Hit = false;
            Projectile.ResetLocalNPCHitImmunity();
            Time = 0f;
            Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld).RotatedByRandom(StabCounter == 4 ? 0f : .15f);
            Init = true;
            Projectile.netUpdate = true;
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = Projectile.timeLeft = 2;
        Owner.ChangeDir(Dir);

        if (Vanishing)
        {
            Projectile.Opacity = MakePoly(3).OutFunction(1f - InverseLerp(0f, 15f * MaxUpdates, VanishTimer));
            if (Projectile.Opacity == 0f)
                Projectile.Kill();

            VanishTimer++;
        }

        if (Completion >= 1f)
        {
            if (Modded.SafeMouseLeft.Current && !Vanishing)
            {
                StabCounter = (StabCounter + 1) % 3;
                if (StabCounter == 2 && HitCount >= StabsNeeded)
                    StabCounter = 4;
                if (!Hit)
                    HitCount = 0;
                Init = false;
            }
            else
            {
                Vanishing = true;
            }
        }

        switch (StabCounter)
        {
            case 4:
                DoSwing();
                break;

            default:
                DoStab();
                break;
        }

        Owner.SetCompositeArmFront(true, 0, Rect.Rotation - MathHelper.PiOver2);
        Projectile.scale = Scale;
        Time++;
    }

    public RotatedRectangle Rect
    {
        get
        {
            Vector2 size = new(Projectile.width * Projectile.scale, 5f * Projectile.scale);
            return new(Projectile.Center - size / 2, size, Projectile.rotation - MathHelper.PiOver4);
        }
    }
    public RotatedRectangle TipRect
    {
        get
        {
            Vector2 size = new(22 * Projectile.scale);
            return new(Rect.Right - size / 2, size, Rect.Rotation);
        }
    }

    public void DoStab()
    {
        if (Time == 0f)
            SoundEngine.PlaySound(SoundID.Item7 with { Identifier = Name, PitchVariance = .2f, Volume = Main.rand.NextFloat(1f, 1.5f) }, Rect.Right);

        float pierce = new PiecewiseCurve()
            .Add(-20f, 70f, .7f, MakePoly(7).OutFunction)
            .Add(70f, -20f, 1f, MakePoly(3).OutFunction)
            .Evaluate(Completion);

        if (Completion < .7f)
        {
            Dust.NewDustPerfect(TipRect.RandomPoint(), DustID.Ichor, null, 0, default, Main.rand.NextFloat(.4f, .8f)).noGravity = true;
        }

        Projectile.Center = Center + Projectile.velocity * pierce;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
    }

    public void DoSwing()
    {
        float swing = MathHelper.PiOver4 + MathHelper.PiOver2 * new PiecewiseCurve()
            .Add(-.7f, -1f, .2f, MakePoly(2).OutFunction)
            .Add(-1f, 1f, .8f, MakePoly(4).InFunction)
            .Add(1f, 1.2f, 1f, Sine.InFunction)
            .Evaluate(Completion) * Dir;

        if (AngularVel > .1f && Completion > .2f)
        {
            ParticleRegistry.SpawnSquishyPixelParticle(TipRect.RandomPoint(), (Rect.Rotation + MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(4f, 6f), Main.rand.Next(90, 130), Main.rand.NextFloat(1.5f, 1.8f), Color.Gold, Color.Goldenrod, 0);

            if (Time % 3 == 2)
            {
                Vector2 pos = Rect.Right;
                Vector2 vel = Rect.Left.SafeDirectionTo(pos) * Main.rand.NextFloat(7f, 10f);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<IchorGlobule>(), Projectile.damage / 3, Projectile.knockBack / 2, Owner.whoAmI);
                for (int i = 0; i < 12; i++)
                {
                    if (Main.rand.NextBool())
                        ParticleRegistry.SpawnBloodParticle(pos, vel.RotatedByRandom(.4) * Main.rand.NextFloat(.4f, .6f), Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .7f), Color.Gold);
                    ParticleRegistry.SpawnGlowParticle(pos, vel.RotatedByRandom(.23f) * Main.rand.NextFloat(.3f, .6f), Main.rand.Next(40, 60), Main.rand.NextFloat(.3f, .5f), Color.Gold, Main.rand.NextFloat(.8f, 1f));
                }
                SoundEngine.PlaySound(SoundID.Item17 with { Volume = Main.rand.NextFloat(.8f, 1.2f), Identifier = Name, PitchVariance = .2f, MaxInstances = 10 }, pos);
            }
        }

        Projectile.Center = Center + PolarVector(Rect.Width / 2, Rect.Rotation);
        Projectile.rotation = Projectile.velocity.ToRotation() + swing;
        HitCount = 0;
    }

    public override bool ShouldUpdatePosition() => false;

    public override bool? CanCutTiles()
    {
        if (StabCounter == 4)
            return Completion.BetweenNum(.25f, .8f) ? null : false;
        return Completion < .7f ? null : false;
    }

    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Rect.TopRight, Rect.BottomLeft, Rect.Width, DelegateMethods.CutTiles);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return TipRect.Intersects(targetHitbox);
    }

    public override bool? CanHitNPC(NPC target)
    {
        if (StabCounter == 4)
            return Completion.BetweenNum(.25f, .8f) ? null : false;
        return Completion < .7f ? null : false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Hit)
        {
            HitCount++;
            Hit = true;
        }

        Vector2 pos;
        if (Rect.TryGetIntersectionPoints(target.RotHitbox(), out List<Vector2> points))
        {
            pos = points[0];
        }
        else
            pos = Rect.Right;

        for (int i = 0; i < 12; i++)
        {
            Vector2 vel = Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(9f, 12f);
            if (StabCounter == 4)
                vel = (Rect.Rotation + MathHelper.PiOver2).ToRotationVector2() * Main.rand.NextFloat(10f, 14f);

            int life = Main.rand.Next(60, 90);
            float scale = Main.rand.NextFloat(1.2f, 1.5f);
            Color color = Color.Gold.Lerp(Color.Yellow, Main.rand.NextFloat(.2f, .5f));
            ParticleRegistry.SpawnSquishyPixelParticle(pos, vel, life, scale, color, color * 2f, 4, true, true);
        }
        ScreenShakeSystem.New(new(.1f, .1f), pos);

        target.AddBuff(BuffID.Ichor, Main.rand.Next(120, 190));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(Lighting.GetColor(Rect.Center.ToTileCoordinates()));

        if (StabCounter == 4 && Completion < .2f)
            return false;

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
        Vector2 scale = new Vector2(.2f, .1f) * Projectile.scale * 1.4f;
        Vector2 pos = Rect.Right + Projectile.velocity.SafeNormalize(Vector2.Zero) * 5f - Main.screenPosition;
        float opacity = StabCounter == 4 ? InverseLerp(.1f, .15f, InverseLerp(0.056f, 0.1f, AngularVel)) : GetLerpBump(0f, .7f, 1f, .3f, Completion);
        Main.EntitySpriteDraw(tex, pos, null, Color.Gold * opacity * 3f, Rect.Rotation, tex.Size() / 2, scale * opacity * 1.4f, 0);
        Main.spriteBatch.ResetBlendState();

        return false;
    }

    public override void OnKill(int timeLeft)
    {
        HitCount = 0;
    }
}

public class DecayingCutleryPlayer : ModPlayer
{
    public int HitCount;
    public override void UpdateDead()
    {
        HitCount = 0;
    }
}
