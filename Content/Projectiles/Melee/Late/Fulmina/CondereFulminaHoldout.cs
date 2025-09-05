using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Fulmina;

public class CondereFulminaHoldout : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CondereFulmina);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public ref float Time => ref Projectile.ai[0];

    public const int ReelTime = 30;
    public const int Charge1 = 30;
    public const int Charge2 = 20;
    public const int Charge3 = 10;
    public const int Charge4 = 5;
    public const int TotalReelTime = ReelTime + Charge1 + Charge2 + Charge3 + Charge4;
    public const int ThrowTime = 40;

    public enum FulminaState
    {
        Aiming,
        Firing,
    }
    public FulminaState State
    {
        get => (FulminaState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }

    public enum FulminaCharge
    {
        None,
        First,
        Second,
        Third,
        Fourth,
    }
    public FulminaCharge Charge
    {
        get => (FulminaCharge)Projectile.ai[2];
        set => Projectile.ai[2] = (int)value;
    }

    public ref float OldArmRot => ref Projectile.Additions().ExtraAI[0];
    public ref float TotalTime => ref Projectile.Additions().ExtraAI[1];

    public Vector2 Tip => Projectile.RotHitbox().TopRight;
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public int Dir => Projectile.velocity.X.NonZeroSign();

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.Size = new(184);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = 1;
    }

    public void OwnerDefaults()
    {
        Owner.heldProj = Projectile.whoAmI;
        Owner.ChangeDir(Dir);
        Owner.SetDummyItemTime(2);
    }

    public override void AI()
    {
        after ??= new(14, () => Projectile.Center);
        switch (State)
        {
            case FulminaState.Aiming:
                if (this.RunLocal())
                {
                    Projectile.velocity = Center.SafeDirectionTo(Modded.mouseWorld);
                    if (Projectile.velocity != Projectile.oldVelocity)
                        this.Sync();
                }
                Projectile.Center = Owner.GetFrontHandPositionImproved();
                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;
                OwnerDefaults();

                float vel = Projectile.velocity.ToRotation();
                float reelAnim = Animators.MakePoly(3f).InOutFunction.Evaluate(vel, vel - (2f * Dir * Owner.gravDir), InverseLerp(0f, ReelTime, Time));
                Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, reelAnim);
                OldArmRot = reelAnim;

                switch (Charge)
                {
                    case FulminaCharge.None:
                        if (Time >= Charge1)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip, Main.rand.NextVector2CircularLimited(5f, 5f, .4f, 1f), Main.rand.Next(15, 20), Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                                ParticleRegistry.SpawnBloomPixelParticle(Tip, RandomVelocity(1f, 1f, 4f), Main.rand.Next(30, 50), Main.rand.NextFloat(.1f, .3f), Color.Cyan, Color.White);
                            }

                            SoundID.DD2_LightningAuraZap.Play(Tip, 1.3f, .1f);
                            Charge = FulminaCharge.First;
                        }
                        break;
                    case FulminaCharge.First:
                        if (Time >= (Charge1 + Charge2))
                        {
                            for (int i = 0; i < 3; i++)
                                ParticleRegistry.SpawnLightningArcParticle(Tip, Main.rand.NextVector2CircularLimited(60f, 60f, .5f, 1f), Main.rand.Next(8, 11), Main.rand.NextFloat(.1f, .4f), Color.Cyan);

                            for (int i = 0; i < 20; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip, Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f), Main.rand.Next(15, 20), Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                                ParticleRegistry.SpawnBloomPixelParticle(Tip, RandomVelocity(1f, 1f, 5f), Main.rand.Next(40, 60), Main.rand.NextFloat(.1f, .3f), Color.Cyan, Color.White);
                            }

                            SoundID.DD2_LightningAuraZap.Play(Tip, 1.7f, 0f);
                            Charge = FulminaCharge.Second;
                        }
                        break;
                    case FulminaCharge.Second:
                        if (Time >= (Charge1 + Charge2 + Charge3))
                        {
                            for (int i = 0; i < 5; i++)
                                ParticleRegistry.SpawnLightningArcParticle(Tip, Main.rand.NextVector2CircularLimited(80f, 80f, .5f, 1f), Main.rand.Next(8, 11), Main.rand.NextFloat(.2f, .5f), Color.Cyan);

                            for (int i = 0; i < 30; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip, Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f), Main.rand.Next(15, 22), Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                                ParticleRegistry.SpawnBloomPixelParticle(Tip, RandomVelocity(1f, 2f, 7f), Main.rand.Next(40, 60), Main.rand.NextFloat(.1f, .3f), Color.Cyan, Color.White);
                            }

                            SoundID.DD2_LightningAuraZap.Play(Tip, 2.3f, -.1f);
                            Charge = FulminaCharge.Third;
                        }
                        break;
                    case FulminaCharge.Third:
                        if (Time >= (Charge1 + Charge2 + Charge3 + Charge4))
                        {
                            for (int i = 0; i < 6; i++)
                                ParticleRegistry.SpawnLightningArcParticle(Tip, Main.rand.NextVector2CircularLimited(100f, 100f, .5f, 1f), Main.rand.Next(8, 11), Main.rand.NextFloat(.3f, .6f), Color.Cyan);

                            for (int i = 0; i < 50; i++)
                            {
                                ParticleRegistry.SpawnSparkParticle(Tip, Main.rand.NextVector2CircularLimited(20f, 20f, .4f, 1f), Main.rand.Next(20, 25), Main.rand.NextFloat(1.4f, 1.7f), Color.Cyan);
                            }
                            ParticleRegistry.SpawnBlurParticle(Tip, 30, .4f, 200f);

                            AdditionsSound.LightningStrike.Play(Tip, 1f, 0f);
                            Charge = FulminaCharge.Fourth;
                        }
                        break;
                    case FulminaCharge.Fourth:

                        if (Time % 2 == 1)
                            ParticleRegistry.SpawnLightningArcParticle(Tip, Main.rand.NextVector2CircularLimited(200f, 200f, .5f, 1f), Main.rand.Next(8, 11), Main.rand.NextFloat(.4f, .8f), Color.Cyan);
                        break;
                }

                if (this.RunLocal() && !Modded.MouseLeft.Current)
                {
                    if (Charge == FulminaCharge.None)
                    {
                        Projectile.Kill();
                        return;
                    }

                    AdditionsSound.etherealReleaseA.Play(Owner.Center, 1.1f, 0f, .1f, 20);
                    State = FulminaState.Firing;
                    Time = 0f;

                    Projectile.MaxUpdates = 2;
                    Projectile.velocity = Projectile.Center.SafeDirectionTo(Modded.mouseWorld) * (Charge == FulminaCharge.First || Charge == FulminaCharge.Second ? 22f : 34f);
                    this.Sync();
                }
                break;

            case FulminaState.Firing:
                if (Time < ThrowTime)
                {
                    OwnerDefaults();

                    float throwCompletion = InverseLerp(0f, ThrowTime, Time);
                    float rot = OldArmRot + (Pi * Dir * Owner.gravDir);
                    float anim = Animators.MakePoly(6f).OutFunction.Evaluate(OldArmRot, rot, throwCompletion);
                    Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, anim);
                }

                Projectile.rotation = Projectile.velocity.ToRotation() + PiOver4;

                for (int p = 0; p < 2; p++)
                {
                    for (int i = -1; i <= 1; i += 2)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            float comp = InverseLerp(0f, 3, j);
                            Vector2 dir = -Projectile.velocity.RotatedBy(MathHelper.Lerp(.5f, 0f, comp) * i) * MathHelper.Lerp(.4f, .8f, comp);
                            float scale = MathHelper.Lerp(1.1f, 2f, comp);
                            ParticleRegistry.SpawnSparkParticle(Tip, dir, 40, scale, Color.DeepSkyBlue);
                        }
                    }
                }
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, 1f, Projectile.rotation, 0, 90));
                break;
        }
        Time++;

        TotalTime++;
    }

    public void SummonLightning(NPC target)
    {
        Vector2 pos = target.Center - new Vector2(Main.rand.NextFloat(-150f, 150f), Main.screenHeight + Main.rand.NextFloat(-180f, 180f));
        Vector2 vel = Vector2.UnitY;
        int type = ModContent.ProjectileType<HonedLightning>();
        HonedLightning lightning = Main.projectile[Projectile.NewProj(pos, vel, type, Projectile.damage, Projectile.knockBack, Owner.whoAmI, 0f, TotalTime)].As<HonedLightning>();
        lightning.End = target.RandAreaInEntity();
        lightning.Sync();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!this.RunLocal())
            return;

        LightningChain chain = Main.projectile[Projectile.NewProj(Tip, Vector2.Zero, ModContent.ProjectileType<LightningChain>(), Projectile.damage, 0f, Projectile.owner, 0f, TotalTime)].As<LightningChain>();

        for (int i = 0; i < (Charge == FulminaCharge.First || Charge == FulminaCharge.Second ? 1 : 2); i++)
            SummonLightning(target);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.RotHitbox().BottomLeft, Projectile.RotHitbox().TopRight, 20f);
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 orig = tex.Size() / 2;

        if (State == FulminaState.Firing && after.afterimages != null)
            after?.DrawFancyAfterimages(tex, [Color.Cyan]);

        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Color.White, Projectile.rotation, orig, Projectile.scale);

        return false;
    }

    public override bool? CanDamage()
    {
        if (State == FulminaState.Firing)
            return Projectile.numHits <= 0 ? null : false;
        return false;
    }
}