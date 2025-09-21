using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Common.Particles.Metaball;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Late;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late;

public class AbyssalCurrentsHoldout : ModProjectile, IHasScreenShader
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AbyssalCurrent);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.CanDistortWater[Type] = false;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.Size = new(134);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.penetrate = -1;
    }

    public Player Owner => player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public enum AbyssalState
    {
        Reel,
        Throw,
        Spin,
        Chase,
    }
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public AbyssalState State
    {
        get => (AbyssalState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }
    public ref float OldArmRot => ref Projectile.ai[2];
    public int BackTime
    {
        get => (int)Projectile.Additions().ExtraAI[0];
        set => Projectile.Additions().ExtraAI[0] = value;
    }

    public float MeleeSpeed => Owner.GetTotalAttackSpeed(DamageClass.MeleeNoSpeed);
    public int ReelTime => (int)(50 / MeleeSpeed);
    public static readonly int ThrowTime = SecondsToFrames(4);
    public int SpinTime => (int)(130 / MeleeSpeed);
    public static int WhirlpoolGrow => 72;
    public static int WhirlpoolRecede => 50;
    public Vector2 Tip => Projectile.RotHitbox().TopRight;
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, false);

    public override void AI()
    {
        switch (State)
        {
            case AbyssalState.Reel:
                DoReel();
                break;
            case AbyssalState.Throw:
                DoThrow();
                break;
            case AbyssalState.Spin:
                DoSpin();
                break;
            case AbyssalState.Chase:
                DoChase();
                break;
        }

        if (State == AbyssalState.Throw)
        {
            if (netMode != NetmodeID.Server)
            {
                WaterShaderData ripple = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                float waveSine = 0.1f * MathF.Sin(GlobalTimeWrappedHourly * 20f);
                Vector2 ripplePos = Projectile.Center + Projectile.velocity * 7f;
                Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
                ripple.QueueRipple(ripplePos, waveData, Vector2.One * 1360f, RippleShape.Square, Projectile.rotation);
            }
        }

        Time++;
    }

    public void DoReel()
    {
        float completion = InverseLerp(0f, ReelTime, Time);
        if (!Owner.Available() || (this.RunLocal() && !Modded.MouseLeft.Current && completion < 1f))
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 22;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Projectile.direction);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .6f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        float vel = Projectile.velocity.ToRotation();
        float reelAnim = Animators.MakePoly(3f).InOutFunction.Evaluate(vel, vel - (1.3f * Projectile.direction * Owner.gravDir), completion);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, reelAnim);
        OldArmRot = reelAnim;
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;

        if (this.RunLocal() && !Modded.MouseLeft.Current && completion >= 1f)
        {
            AdditionsSound.charShot.Play(Projectile.Center, 1.4f, -.3f, .2f);
            Time = 0;
            State = AbyssalState.Throw;
            Projectile.MaxUpdates = 2;
            Projectile.timeLeft = Projectile.MaxUpdates * ThrowTime;
            Projectile.velocity = Projectile.SafeDirectionTo(Modded.mouseWorld) * 30f;
            this.Sync();
        }
    }

    public void DoThrow()
    {
        if (Time < 30)
        {
            Owner.SetDummyItemTime(2);
            Owner.ChangeDir(Projectile.direction);
            float throwCompletion = InverseLerp(0f, 30, Time);
            float rot = OldArmRot + (Pi * Projectile.direction * Owner.gravDir);
            float anim = Animators.MakePoly(6f).OutFunction.Evaluate(OldArmRot, rot, throwCompletion);
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, anim);
            this.Sync();
        }

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(i == 0 ? -.6f : i == 1 ? -.4f : i == 2 ? .4f : .6f) * -(j == 0 ? 8f : 15f);
                MetaballRegistry.SpawnAbyssalMetaball(Tip, vel, 60, 130);
            }
        }
    }

    public void DoSpin()
    {
        float completion = InverseLerp(0f, SpinTime, Time);
        if (!Owner.Available() || (this.RunLocal() && !Modded.MouseRight.Current && completion < 1f))
        {
            Projectile.Kill();
            return;
        }

        Projectile.timeLeft = 2;
        Owner.SetDummyItemTime(2);
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Projectile.direction);

        if (Time == 0)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Projectile.SafeDirectionTo(Modded.mouseWorld);
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            this.Sync();
        }

        if (Main.rand.NextBool(3))
        {
            Vector2 pos = Projectile.Center + Main.rand.NextVector2CircularLimited(200f, 200f, .6f, 1f);
            Vector2 vel = pos.SafeDirectionTo(Projectile.Center);
            ParticleRegistry.SpawnDustParticle(pos, vel, Main.rand.Next(40, 60), Main.rand.NextFloat(.6f, 1f), MulticolorLerp(Main.rand.NextFloat(), AbyssalCurrents.WaterPalette), .1f, false, true, true);
        }

        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation());
        Projectile.rotation += MathHelper.Lerp(0f, .4f, completion) * Projectile.velocity.X.NonZeroSign();
        Projectile.Center = Owner.GetFrontHandPositionImproved();

        if (completion >= 1f)
        {
            Time = 0;
            State = AbyssalState.Chase;
            Projectile.velocity = Projectile.SafeDirectionTo(Modded.mouseWorld) * 10f;
            this.Sync();
        }
    }

    public LoopedSoundInstance water;
    public void DoChase()
    {
        float completion = InverseLerp(0f, WhirlpoolGrow, Time) * InverseLerp(WhirlpoolRecede, 0f, BackTime);
        water ??= LoopedSoundManager.CreateNew(new(AdditionsSound.waterfall, () => completion * 4.7f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile), () => State == AbyssalState.Chase);
        water?.Update(Projectile.Center);

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.CanHomeInto() && !npc.boss && npc.IsAnEnemy() && npc.realLife <= 0 && npc.velocity.Length() > 1f && npc.knockBackResist != 0f && !npc.dontTakeDamage)
            {
                npc.velocity += npc.SafeDirectionTo(Projectile.Center) * GaussianFalloff2D(Projectile.Center, npc.Center, .2f * npc.knockBackResist, 1000f);
            }
        }

        Projectile.timeLeft = 2;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir((Projectile.Center.X > Center.X).ToDirectionInt());
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Center.AngleTo(Projectile.Center));

        Projectile.rotation += .5f * InverseLerp(WhirlpoolRecede, 0f, BackTime);

        if (this.RunLocal() && Modded.MouseRight.Current && BackTime <= 0)
        {
            Vector2 dest = Modded.mouseWorld;
            bool away = Vector2.Dot(Projectile.Center.SafeDirectionTo(dest), Projectile.velocity) < 0f;
            Vector2 force = Projectile.Center.SafeDirectionTo(dest) * (MathF.Min(Projectile.Distance(dest), 15f) * (away ? 1.8f : 1f));
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, force, .1f);
            Projectile.velocity += force * .001f;
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        else if (this.RunLocal() && (!Modded.MouseRight.Current || BackTime > 0))
        {
            if (BackTime >= WhirlpoolRecede)
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(Owner.Center) * MathHelper.Clamp(Projectile.Distance(Owner.Center), 5f, 50f), .2f);
                Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.velocity.ToRotation() + MathHelper.PiOver4, .4f);
                Projectile.Opacity = Utils.Remap(Projectile.Distance(Owner.Center), 0f, 80f, 0f, 1f);
                if (Projectile.WithinRange(Owner.Center, 80f))
                    Projectile.Kill();
            }
            BackTime++;
            Projectile.netUpdate = true;
        }
    }

    public override bool? CanDamage() => (State == AbyssalState.Throw || State == AbyssalState.Chase) && BackTime <= 0;
    public override bool ShouldUpdatePosition() => State == AbyssalState.Throw || State == AbyssalState.Chase;
    public override bool? CanCutTiles() => CanDamage();
    public override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight, 130f, DelegateMethods.CutTiles);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight, 130f);
    }

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("WhirlpoolSwirl");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        float completion = InverseLerp(0f, WhirlpoolGrow, Time) * InverseLerp(WhirlpoolRecede, 0f, BackTime);
        Shader.TrySetParameter("screenSize", new Vector2(screenWidth, screenHeight));
        Shader.TrySetParameter("distortionRadius", 100f * completion);
        Shader.TrySetParameter("distortionIntensity", .7f * completion);
        Shader.TrySetParameter("blackSize", .6f * completion);
        Shader.TrySetParameter("distortionPosition", Vector2.Transform(Projectile.Center - screenPosition, GameViewMatrix.TransformationMatrix));
        Shader.TrySetParameter("zoom", GameViewMatrix.Zoom.X);
        Shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.BigWavyBlobNoise), 1, SamplerState.LinearClamp);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("WhirlpoolSwirl", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }

    public bool IsEntityActive() => Projectile.active;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;

        float opac = Projectile.Opacity;
        if (State == AbyssalState.Chase)
        {
            if (BackTime > 0)
                opac = InverseLerp(0f, WhirlpoolRecede, BackTime) * Projectile.Opacity;
            else
                opac = InverseLerp(WhirlpoolGrow, 0f, Time) * Projectile.Opacity;
        }
        if (State == AbyssalState.Spin || State == AbyssalState.Chase)
        {
            float comp = State == AbyssalState.Chase ? 1f : InverseLerp(0f, SpinTime, Time);
            float area = MathHelper.Lerp(14f, 2f, comp);
            Projectile.DrawProjectileBackglow(AbyssalCurrents.WaterPalette[2] * comp * opac, area, 0, 4);
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White * opac, Projectile.rotation, orig, Projectile.scale);
        if (State == AbyssalState.Reel)
        {
            SpriteBatch sb = Main.spriteBatch;
            Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise);

            float fade = State == AbyssalState.Throw ? 1f : Animators.MakePoly(2f).OutFunction(InverseLerp(0f, ReelTime, Time));
            Vector2 res = new(200f * fade);
            ManagedShader shine = AssetRegistry.GetShader("RadialShineShader");
            shine.TrySetParameter("glowPower", .2f);
            shine.TrySetParameter("glowColor", AbyssalCurrents.WaterPalette[0].ToVector4());
            shine.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 5f);
            shine.TrySetParameter("resolution", res);

            sb.EnterShaderRegionAlt();
            shine.Render();
            sb.Draw(noise, ToTarget(Tip, res), null, AbyssalCurrents.BrackishPalette[4] * 0.4f * fade, Projectile.rotation, noise.Size() * 0.5f, 0, 0f);

            sb.ExitShaderRegion();
        }

        if (State == AbyssalState.Chase)
        {
            if (!HasShader)
                InitializeShader();

            UpdateShader();

            void pool()
            {
                Texture2D noise = AssetRegistry.GetTexture(AdditionsTexture.CausticNoise);
                float completion = InverseLerp(0f, WhirlpoolGrow, Time) * InverseLerp(WhirlpoolRecede, 0f, BackTime);
                Main.spriteBatch.Draw(noise, ToTarget(Projectile.Center, new Vector2(400f) * completion), null, Color.Cyan * completion, 0f, noise.Size() / 2f, 0, 0f);
            }
            ManagedShader shader = AssetRegistry.GetShader("WhirlpoolShader");
            PixelationSystem.QueueTextureRenderAction(pool, PixelationLayer.UnderNPCs, BlendState.AlphaBlend, shader);
        }

        return false;
    }
}