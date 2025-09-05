using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static Terraria.Main;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class TheExingendies : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();

    public enum States
    {
        // Reality tears
        Luminescence,

        // Orion's Sword and numerous constellations
        Constellative,

        // A controlled star
        Vaporization,

        // Gamma Ray
        ActiveGalacticNucleus
    }

    public ref float GalaxyRotation => ref Projectile.ai[0];
    public States Phase
    {
        get => (States)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }
    public ref float ForwardRotation => ref Projectile.ai[2];
    public ref float ChargeTimer => ref Projectile.Additions().ExtraAI[0];
    public ref float PhaseTimer => ref Projectile.Additions().ExtraAI[1];
    public ref float FadeTimer => ref Projectile.Additions().ExtraAI[2];
    public States SavePhase
    {
        get => (States)Projectile.Additions().ExtraAI[3];
        set => Projectile.Additions().ExtraAI[3] = (int)value;
    }
    public bool RayWait
    {
        get => Projectile.Additions().ExtraAI[4] == 1f;
        set => Projectile.Additions().ExtraAI[4] = value.ToInt();
    }
    public ref float SpinDirection => ref Projectile.Additions().ExtraAI[5];

    public Vector2 Size;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Size);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Size = reader.ReadVector2();
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
    public float Completion => InverseLerp(0f, ChargeTime, ChargeTimer) * FadeCompletion;
    public Vector2 MainCenter => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public override void AI()
    {
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
                FadeTimer = (int)MakePoly(4f).InOutFunction.Evaluate(FadeTimer, 0f, .2f);
                Projectile.timeLeft = 650;
            }
        }

        Size = Vector2.Lerp(Vector2.Zero, new(600f), MakePoly(3f).InOutFunction(Completion));

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, MainCenter.SafeDirectionTo(ModdedOwner.mouseWorld), Utils.Remap(ModdedOwner.mouseWorld.Distance(MainCenter), 0f, 200f, .04f, .16f));
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = MainCenter + PolarVector(BezierEase.Evaluate(0f, 80f, Completion), Projectile.rotation);
        ForwardRotation = MakePoly(3f).OutFunction.Evaluate(ForwardRotation,
            Utils.Remap(ModdedOwner.mouseWorld.Distance(MainCenter), 0f, 200f, MathHelper.PiOver2, MathHelper.Pi), .01f);

        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.itemRotation = WrapAngle(Projectile.rotation);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        if (Phase == States.ActiveGalacticNucleus)
            // Make the galaxy spiral inward as if its contents are being used by a black hole to create the gamma rays
            SpinDirection = MathHelper.Lerp(SpinDirection, -1f, .06f);
        else
            SpinDirection = MathHelper.Lerp(SpinDirection, 1f, .06f);

        if (ChargeTimer < ChargeTime)
            ChargeTimer++;

        if (Completion >= 1f)
        {
            if (AdditionsKeybinds.SetBonusHotKey.JustPressed && Phase != States.ActiveGalacticNucleus && !RayWait && !AdditionsKeybinds.MiscHotKey.Current)
            {
                PhaseTimer = 0f;
                SavePhase = Phase;
                Phase = States.ActiveGalacticNucleus;
            }
            if (Phase != States.ActiveGalacticNucleus)
            {
                if (AdditionsKeybinds.MiscHotKey.JustPressed)
                {
                    PhaseTimer = 0f;
                    Projectile.ai[1] = (Projectile.ai[1] + 1) % (int)GetLastEnumValue<States>();
                }
            }

            switch (Phase)
            {
                case States.Luminescence:
                    DoPhase_Luminescence();
                    break;
                case States.Constellative:
                    DoPhase_Constellative();
                    break;
                case States.Vaporization:
                    DoPhase_Vaporization();
                    break;
                case States.ActiveGalacticNucleus:
                    DoPhase_GammaRay();
                    break;
            }
            PhaseTimer++;
        }

        GalaxyRotation = (GalaxyRotation + (.1f * SpinDirection)) % (MathHelper.TwoPi);
    }

    public void DoPhase_Luminescence()
    {
        if (PhaseTimer % 10 == 9 && this.RunLocal())
        {
            Projectile.NewProj(Projectile.Center, MainCenter.SafeDirectionTo(ModdedOwner.mouseWorld).RotatedByRandom(.6f) * Main.rand.NextFloat(10f, 20f),
                ModContent.ProjectileType<LuminescentChaser>(), (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(2000f), Projectile.knockBack, Owner.whoAmI);
            AdditionsSound.MagicHit.Play(Projectile.Center, .8f, 0f, .2f, 400, Name);
        }

        if (PhaseTimer % 40 == 39 && this.RunLocal())
        {
            Projectile.NewProj(ModdedOwner.mouseWorld, Vector2.UnitY.RotatedByRandom(.67f),
                ModContent.ProjectileType<ScreenSplit>(), (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(23500f), Projectile.knockBack, Owner.whoAmI);
            AdditionsSound.VirtueAttack.Play(ModdedOwner.mouseWorld, 1.4f, -.7f, 0f, 300, Name);
            AdditionsSound.LargeWeaponFireDifferent.Play(ModdedOwner.mouseWorld, 1.3f, .5f, 0f, 300, Name);
        }

        if (PhaseTimer >= SecondsToFrames(7))
        {
            PhaseTimer = 0f;
            Phase = States.Constellative;
        }
    }

    public void DoPhase_Constellative()
    {
        if (PhaseTimer % 12 == 11 && this.RunLocal())
        {
            Projectile.NewProj(Projectile.Center, MainCenter.SafeDirectionTo(ModdedOwner.mouseWorld) * 14f,
                ModContent.ProjectileType<SpaceRip>(), (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(2000f), Projectile.knockBack, Owner.whoAmI);
        }

        if (PhaseTimer % (Constellation.Lifetime / 2) == (Constellation.Lifetime / 2 - 1) && this.RunLocal())
        {
            Projectile.NewProj(ModdedOwner.mouseWorld, Vector2.UnitY.RotatedByRandom(.67f),
                ModContent.ProjectileType<Constellation>(), (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(1000f), Projectile.knockBack, Owner.whoAmI);
            AdditionsSound.etherealChargeBoom2.Play(ModdedOwner.mouseWorld, 1f, 0f, .1f, 200, Name);
        }

        if (PhaseTimer >= SecondsToFrames(7))
        {
            PhaseTimer = 0f;
            Phase = States.Vaporization;
        }
    }

    public void DoPhase_Vaporization()
    {
        int star = ModContent.ProjectileType<CollapsingStar>();
        if (this.RunLocal() && Owner.ownedProjectileCounts[star] <= 0)
        {
            Main.projectile[Projectile.NewProj(Projectile.Center, Vector2.Zero, star, (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(42000f), Projectile.knockBack, Owner.whoAmI)].Additions().ExtraAI[3] = Projectile.whoAmI;
            AdditionsSound.BallFire.Play(Projectile.Center, 1.2f, -.2f, 0f, 50, Name);
        }

        if (this.RunLocal() && PhaseTimer % 40 == 39 )
            Projectile.NewProj(Projectile.Center, MainCenter.SafeDirectionTo(ModdedOwner.mouseWorld) * 16f,
                ModContent.ProjectileType<HighSpeedDebris>(), (int)Owner.GetTotalDamage<GenericDamageClass>().ApplyTo(18000f), Projectile.knockBack, Owner.whoAmI);

        if (PhaseTimer >= SecondsToFrames(7))
        {
            PhaseTimer = 0f;
            Phase = States.Luminescence;
        }
    }

    public void DoPhase_GammaRay()
    {
        int ray = ModContent.ProjectileType<GammaRay>();
        if (!RayWait)
        {
            if (this.RunLocal() && Owner.ownedProjectileCounts[ray] <= 0)
            {
                Projectile.NewProj(Projectile.Center, Vector2.Zero, ray, Projectile.damage, Projectile.knockBack, Owner.whoAmI, Projectile.whoAmI);
            }

            if (AdditionsKeybinds.SetBonusHotKey.JustPressed && PhaseTimer > GammaRay.ExpandTime)
            {
                RayWait = true;
            }
        }
        if (RayWait)
            PhaseTimer = 0f;

        if (RayWait && Owner.ownedProjectileCounts[ray] <= 0)
        {
            RayWait = false;
            Phase = SavePhase;
        }
    }

    public override bool? CanCutTiles() => false;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.Pixel);
            spriteBatch.DrawBetterRect(tex, ToTarget(Projectile.Center, Size * 2f), null, Color.White, Projectile.rotation + MathHelper.PiOver2, tex.Size() / 2f);
        }

        float speed = 1f;
        Vector3 tint = new(100, 50, 150);
        int arms = 3;
        float armTightness = 12f;
        float dust = 25f;
        float bulge = 16f;
        bool additive = false;
        bool negative = false;

        // i would have used a fancy hashset or switch statement but no flexibility
        // conditional chain RELEASE
        if (Owner.name.Equals($"chinny winny 2nd", StringComparison.OrdinalIgnoreCase))
        {
            tint = new Vector3(51, 90, 194 * 2.5f);
            arms = 2;
            armTightness = 1112.410f;
            dust = 30f;
        }
        else if (Owner.name.Equals($"a little bit too much coffee", StringComparison.OrdinalIgnoreCase))
        {
            speed = .7f;
            tint = new Vector3(50, 205, 50);
        }
        else if (Owner.name.Equals($"titan", StringComparison.OrdinalIgnoreCase))
        {
            tint = new Vector3(52, 152 / 2, 219 * 2);
            dust = 40f;
        }
        else if (Owner.name.Equals($"Balaho", StringComparison.OrdinalIgnoreCase))
        {
            negative = true;
            tint = new Vector3(200, 800, 100);
            bulge = 20f;
            speed = 1.8f;
            arms = 2;
            dust = 50f;
        }
        else if (Owner.name.Equals($"ashes_plus", StringComparison.OrdinalIgnoreCase))
        {
            tint = new Vector3(128 * 2, 3, 3);
        }
        else if (Owner.name.Equals($"plussie", StringComparison.OrdinalIgnoreCase))
        {
            additive = true;
            speed = 2f;
            tint = new Vector3(255, 229, 180); // new Vector3(255, 152, 153)
            arms = 12;
            armTightness = 400f;
            dust = 2f;
            bulge = 1.8f;
        }
        else if (Owner.name.Equals($"bugman", StringComparison.OrdinalIgnoreCase))
        {
            bulge = 20f;
            speed = 3f;
            tint = new(40, 230, 20);
            arms = 8;
            armTightness = 2f;
        }

        ManagedShader shader = AssetRegistry.GetShader("ExingenediesVortex");
        shader.TrySetParameter("Size", Size);
        shader.TrySetParameter("Time", GalaxyRotation * speed);
        shader.TrySetParameter("ColorTint", new Vector3(tint.X / 255f, tint.Y / 255f, tint.Z / 255f) * FadeCompletion);
        shader.TrySetParameter("SpiralArmCount", arms);
        shader.TrySetParameter("SpiralWinding", armTightness);
        shader.TrySetParameter("BulgeAmount", bulge);
        shader.TrySetParameter("DustDensity", dust);
        shader.TrySetParameter("Negative", negative);
        shader.TrySetParameter("ForwardRotation", ForwardRotation); // PI = perpindicular PI/2 = flat

        ScreenShaderUpdates.QueueDrawAction(draw, additive ? BlendState.Additive : BlendState.AlphaBlend, shader);
        return false;
    }
}