using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;
using Utils = Terraria.Utils;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class TheExingendies : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public Player Owner => player[Projectile.owner];
    public PlayerMouse ModdedOwner => Owner.AdditionsMouse();
    public ref float GalaxyRotation => ref Projectile.ai[0];

    public ref float ForwardRotation => ref Projectile.ai[2];

    public int FadeTimer
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[2];
        set => Projectile.AdditionsInfo().ExtraAI[2] = value;
    }

    public int Time
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[4];
        set => Projectile.AdditionsInfo().ExtraAI[4] = value;
    }

    public ref float SpinDirection => ref Projectile.AdditionsInfo().ExtraAI[5];

    public Vector2 Size;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Size);
        writer.Write(Preset.Speed);
        writer.Write(Preset.Tint.X);
        writer.Write(Preset.Tint.Y);
        writer.Write(Preset.Tint.Z);
        writer.Write(Preset.Arms);
        writer.Write(Preset.ArmTightness);
        writer.Write(Preset.Dust);
        writer.Write(Preset.Bulge);
        writer.Write(Preset.Additive);
        writer.Write(Preset.Negative);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Size = reader.ReadVector2();
        float speed = reader.ReadSingle();
        float r = reader.ReadSingle();
        float g = reader.ReadSingle();
        float b = reader.ReadSingle();
        int arms = reader.ReadInt32();
        float armstight = reader.ReadSingle();
        float dust = reader.ReadSingle();
        float bulge = reader.ReadSingle();
        bool add = reader.ReadBoolean();
        bool neg = reader.ReadBoolean();
        Preset = new(speed, new(r, g, b), arms, armstight, dust, bulge, add, neg);
    }

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.CanDistortWater[Type] = false;
        ProjectileID.Sets.CanHitPastShimmer[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(80);
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Default;
        Projectile.penetrate = -1;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public static readonly int FadeTime = SecondsToFrames(.65f);
    public static readonly int ChargeTime = SecondsToFrames(.75f);
    public float FadeCompletion => InverseLerp(FadeTime, 0f, FadeTimer);
    public float Completion => InverseLerp(0f, ChargeTime, Time) * FadeCompletion;
    public Vector2 MainCenter => Owner.RotatedRelativePoint(Owner.MountedCenter);

    public override void AI()
    {
        if (Time == 0)
            DecideGalaxy();

        if (this.RunLocal())
        {
            if (!Owner.Available() || !ModdedOwner.SafeMouseLeft.Current)
            {
                FadeTimer++;
                if (FadeTimer >= FadeTime)
                    Projectile.Kill();
            }
            else
            {
                FadeTimer = (int) MakePoly(4f).InOutFunction.Evaluate(FadeTimer, 0f, .2f);
                Projectile.timeLeft = 650;
            }
        }

        if (Completion < 1f)
        {
            Size = Vector2.Lerp(Vector2.Zero, new(600f), MakePoly(3f).InOutFunction(Completion));
            this.Sync();
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity,
                MainCenter.SafeDirectionTo(ModdedOwner.MouseWorld),
                Utils.Remap(ModdedOwner.MouseWorld.Distance(MainCenter), 0f, 200f, .04f, .16f));
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.rotation = Lerp(-PiOver2 - .8f, -PiOver2 + .8f, Sin01(Time * .01f));
        Projectile.Center = MainCenter + PolarVector(BezierEase.Evaluate(0f, 80f, Completion), -PiOver2);
        ForwardRotation += .005f;
        SpinDirection = Lerp(SpinDirection, 1f, .06f);
        GalaxyRotation += .06f * SpinDirection;

        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());

        if (Completion >= 1f)
        {
            if (this.RunLocal() && ModdedOwner.SafeMouseRight.JustPressed)
            {
                foreach (NPC npc in ActiveNPCs)
                {
                    if (npc.Hitbox.Intersects(MouseHitbox))
                    {
                        Projectile.NewProj(ModdedOwner.MouseWorld, PolarVector(1f, RandomRotation()),
                            ModContent.ProjectileType<ScreenSplit>(), int.MaxValue / 4, Projectile.knockBack, Owner.whoAmI);
                        AssetRegistry.GennedSounds.VirtueAttack.Play(ModdedOwner.MouseWorld, 1.4f, -.7f, 0f, 300, Name);
                        AssetRegistry.GennedSounds.LargeWeaponFireDifferent.Play(ModdedOwner.MouseWorld, 1.3f, .5f, 0f,
                            300, Name);
                        break;
                    }
                }
            }
        }

        Time++;
    }

    public override bool? CanCutTiles() => false;

    public readonly struct GalaxyPreset(
        float speed,
        Vector3 tint,
        int arms,
        float armtight,
        float dust,
        float bulge,
        bool add,
        bool neg)
    {
        public readonly float Speed = speed;
        public readonly Vector3 Tint = tint;
        public readonly int Arms = arms;
        public readonly float ArmTightness = armtight;
        public readonly float Dust = dust;
        public readonly float Bulge = bulge;
        public readonly bool Additive = add;
        public readonly bool Negative = neg;
    }

    public GalaxyPreset Preset;

    public void DecideGalaxy()
    {
        float speed = 1f;
        Vector3 tint = new(100, 50, 150);
        int arms = 3;
        float armTightness = 12f;
        float dust = 25f;
        float bulge = 16f;
        bool additive = false;
        bool negative = false;

        if (Owner.name.Equals("chinny", StringComparison.OrdinalIgnoreCase) ||
            Owner.name.Equals("chinny winny 2nd", StringComparison.OrdinalIgnoreCase))
        {
            tint = new Vector3(51, 90, 194 * 2.5f);
            arms = 2;
            armTightness = 1112.410f;
            dust = 30f;
        }
        else if (Owner.name.Equals("too much coffee", StringComparison.OrdinalIgnoreCase))
        {
            speed = .7f;
            tint = new Vector3(50, 205, 50);
        }
        else if (Owner.name.Equals("titan", StringComparison.OrdinalIgnoreCase))
        {
            tint = new Vector3(52, 152 / 2f, 219 * 2);
            dust = 40f;
        }
        else if (Owner.name.Equals("Balaho", StringComparison.OrdinalIgnoreCase))
        {
            negative = true;
            tint = new Vector3(200, 800, 100);
            bulge = 20f;
            speed = 1.8f;
            arms = 2;
            dust = 50f;
        }
        else if (Owner.name.Equals("ashes_plus", StringComparison.OrdinalIgnoreCase))
        {
            tint = new Vector3(128 * 2, 3, 3);
        }
        else if (Owner.name.Equals("plussie", StringComparison.OrdinalIgnoreCase))
        {
            additive = true;
            speed = 2f;
            tint = new Vector3(255, 229, 180); // new Vector3(255, 152, 153)
            arms = 12;
            armTightness = 400f;
            dust = 2f;
            bulge = 4f;
        }
        else if (Owner.name.Equals("bugman", StringComparison.OrdinalIgnoreCase))
        {
            bulge = 20f;
            speed = 3f;
            tint = new(40, 230, 20);
            arms = 8;
            armTightness = 2f;
        }
        else if (Owner.name.Equals("wacey", StringComparison.OrdinalIgnoreCase))
        {
            bulge = 6f;
            tint = new Vector3(48, 0, 156);
            dust = 9f;
            speed = 1.5f;
            arms = 1;
            armTightness = 29;
        }
        else if (Owner.name.Equals("brin", StringComparison.OrdinalIgnoreCase))
        {
            bulge = 20f;
            tint = new(28, 50, 348);
            dust = 20f;
            arms = 3;
        }
        else if (Owner.name.Equals("roomy", StringComparison.OrdinalIgnoreCase))
        {
            tint = new(355, 160, 0);
            dust = 22f;
            arms = 0;
            armTightness = 0f;
        }
        else if (Owner.name.Equals("peter", StringComparison.OrdinalIgnoreCase))
        {
            bulge = 9f;
            tint = new Vector3(40, 255, 0);
            arms = 4;
            dust = 50f;
            armTightness = 205.5f;
            additive = true;
        }
        else if (Owner.name.Equals("dante", StringComparison.OrdinalIgnoreCase))
        {
            bulge = 15f;
            tint = new(205, 0, 30);
            arms = 6;
            armTightness = 2f;
            dust = 14f;
        }
        else
        {
            speed = rand.NextFloat(.9f, 1.3f);
            if (!rand.NextBool(40))
            {
                tint = new(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));
                // err away from too yellow a bit
                int tries = 0;
                while (Math.Min(tint.X, tint.Y) - tint.Z > 60 && tries < 15)
                {
                    tint = new(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255));
                    tries++;
                }

                // green gets a little too strong when overpowering the other channels
                if (tint.Y > tint.X && tint.Y > tint.Z)
                    tint.Y = Math.Max(tint.X, tint.Z);
            }

            arms = rand.Next(2, 4);
            armTightness = rand.NextFloat(8f, 18f);
            dust = rand.NextFloat(17f, 34f);
            bulge = rand.NextFloat(12f, 18f);
            additive = rand.NextBool(60);
            negative = rand.NextBool(60);
        }

        Preset = new GalaxyPreset(speed, tint, arms, armTightness, dust, bulge, additive, negative);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D tex = AssetRegistry.GennedTextures.Pixel;
            spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size * 2f), null, Color.White,
                Projectile.rotation + PiOver2, tex.Size() / 2f);
        }

        ManagedShader shader = AssetRegistry.GennedShaders.ExingenediesVortex;
        shader.TrySetParameter("Size", Size);
        shader.TrySetParameter("Time", GalaxyRotation * Preset.Speed);
        shader.TrySetParameter("ColorTint", Vector3.Divide(Preset.Tint, 255f) * FadeCompletion);
        shader.TrySetParameter("SpiralArmCount", Preset.Arms);
        shader.TrySetParameter("SpiralWinding", Preset.ArmTightness);
        shader.TrySetParameter("BulgeAmount", Preset.Bulge);
        shader.TrySetParameter("DustDensity", Preset.Dust);
        shader.TrySetParameter("Negative", Preset.Negative);
        shader.TrySetParameter("ForwardRotation", ForwardRotation); // PI = perpindicular PI/2 = flat

        ScreenShaderUpdates.QueueDrawAction(draw, Preset.Additive ? BlendState.Additive : BlendState.AlphaBlend,
            shader);
        return false;
    }
}

public class ScreenSplit : ModProjectile, IHasScreenShader
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public ref float Time => ref Projectile.ai[0];
    public ref float Width => ref Projectile.ai[1];
    public const int Lifetime = 110;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 60000;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 250;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.tileCollide = Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Default;
        Projectile.timeLeft = Lifetime;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = -1;
    }

    public override void AI()
    {
        Projectile.Opacity = MakePoly(2f).OutFunction.Evaluate(Time, 0f, 20f, 0f, 1f);

        float comp = InverseLerp(0f, Lifetime, Time);
        float max = 100f / ScreenSize.X;
        Width = new PiecewiseCurve()
            .Add(0f, max, 10f / Lifetime, MakePoly(4f).InOutFunction)
            .AddStall(max, 90f / Lifetime)
            .Add(max, 0f, 1f, MakePoly(3f).OutFunction)
            .Evaluate(comp);
        float hitbox = new PiecewiseCurve()
            .Add(0f, 250f, 10f / Lifetime, MakePoly(4f).InOutFunction)
            .AddStall(250f, 90f / Lifetime)
            .Add(250f, 0f, 1f, MakePoly(3f).OutFunction)
            .Evaluate(comp);
        Projectile.ExpandHitboxBy(new Vector2(hitbox));
        
        Projectile.ResetLocalNPCHitImmunity();
        
        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.defense = 0;
        target.SimpleStrikeNPC(target.lifeMax / 20, hit.HitDirection);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.ScalingArmorPenetration += 1f;
        modifiers.SetCrit();
        modifiers.SetInstantKill();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        for (int i = 0; i < 4; i++)
        {
            float angle = TwoPi * InverseLerp(0, 4, i);
            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(angle);
            return targetHitbox.LineCollision(Projectile.Center - dir * 1000f, Projectile.Center + dir * 1000f, Projectile.width / 2f);
        }
        
        return false;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; }

    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("GenediesScreenSplit");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        Vector2 size = Main.ScreenSize.ToVector2();
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        Shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        Shader.TrySetParameter("glitchIntensity", .04f * Projectile.Opacity);
        Shader.TrySetParameter("screenSize", size);
        Shader.TrySetParameter("splitWidth", Width);
        Shader.TrySetParameter("splitCenter", GetTransformedScreenCoords(Projectile.Center) / size);
        Shader.TrySetParameter("splitDirection", new Vector2(-dir.Y, dir.X));
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("GenediesScreenSplit", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }

    public bool IsEntityActive() => Projectile.active;

    public override bool PreDraw(ref Color lightColor)
    {
        if (!HasShader)
            InitializeShader();

        UpdateShader();

        return false;
    }

    public override bool PreKill(int timeLeft)
    {
        ReleaseShader();
        return true;
    }
}
