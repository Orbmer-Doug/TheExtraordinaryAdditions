using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class FunnyRock : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CopperWireWrappedRock);
    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 26;
        Projectile.timeLeft = 300;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public ref float Time => ref Projectile.ai[0];
    public bool HitGround
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }

    public FancyAfterimages fancy;
    public Player Owner => Main.player[Projectile.owner];
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public const int ThrowTime = 30;
    public float Completion => InverseLerp(0f, ThrowTime, Time);
    public float ThrowDisplacement()
    {
        return Projectile.velocity.ToRotation() + (MathHelper.PiOver2 * new PiecewiseCurve()
            .Add(0f, -1f, .4f, Sine.OutFunction)
            .Add(-1f, -.1f, 1f, MakePoly(4).InFunction)
            .Evaluate(Completion) * Dir);
    }
    public override void AI()
    {
        if (Time < ThrowTime)
        {
            Projectile.tileCollide = false;
            Owner.heldProj = Projectile.whoAmI;
            Owner.SetDummyItemTime(50);
            if (this.RunLocal())
            {
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Owner.RotatedRelativePoint(Owner.MountedCenter, false, true).SafeDirectionTo(Owner.Additions().mouseWorld), .8f);
            }
            Owner.ChangeDir(Dir);
            float rot = ThrowDisplacement();
            Projectile.rotation = rot;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rot - MathHelper.PiOver2);
            Projectile.Center = Owner.GetFrontHandPositionImproved() + PolarVector(Projectile.width / 2, rot);
            this.Sync();
        }
        if (Time == ThrowTime)
        {
            SoundID.Item1.Play(Projectile.Center, 1f, -.1f, .2f);
            Projectile.tileCollide = true;
            Projectile.velocity *= 15f;
        }
        if (Time > ThrowTime)
        {
            Projectile.VelocityBasedRotation();
            fancy ??= new(5, () => Projectile.Center);
            fancy.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity * .8f, Projectile.rotation, 0, 210));

            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -22f, 22f);
            if (Projectile.velocity.Y > 3f && HitGround)
            {
                if (Projectile.localAI[0]++ > 20)
                {
                    HitGround = false;
                    Projectile.localAI[0] = 0;
                }
            }
        }

        Projectile.Opacity = InverseLerp(0f, 10f, Time) * InverseLerp(0f, 20f, Projectile.timeLeft);

        Time++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Projectile.timeLeft > 120)
            Projectile.timeLeft = 120;
        Projectile.velocity.X *= .8f;
        Projectile.velocity.Y *= .3f;
        if (!HitGround)
        {
            Collision.HitTiles(Projectile.Center, -Projectile.velocity, Projectile.width, Projectile.height);
            SoundID.Tink.Play(Projectile.Center, 1f, -.2f, .1f);
            HitGround = true;
        }

        return false;
    }

    public override bool? CanDamage()
    {
        if (Time > ThrowTime)
        {
            if (HitGround)
            {
                if (Projectile.velocity.Y > 8f)
                    return null;
                return false;
            }
            return null;
        }
        return false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (!target.IsFleshy())
            modifiers.FinalDamage *= 2;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.IsFleshy())
        {
            SoundID.Tink.Play(Projectile.Center, .9f, 0f, .2f);
            for (int i = 0; i < 20; i++)
            {
                Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.Stone, -Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(.2f, 4f), 0, default, Main.rand.NextFloat(.4f, 1f));
                ParticleRegistry.SpawnBloodParticle(Projectile.RotHitbox().RandomPoint(),
                    -Projectile.velocity * Main.rand.NextFloat(.2f, .4f), Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .6f), Color.DarkRed);
            }
        }
        else
        {
            AdditionsSound.LightningStrike.Play(Projectile.Center, .7f, 0f, 0f, 1, Name);
            AdditionsSound.ElectricalPowBoom.Play(Projectile.Center, 1.2f, .2f, 0f, 0, Name);
            if (this.RunLocal())
                Projectile.NewProj(target.Center, Vector2.Zero, ModContent.ProjectileType<RockLightning>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);

            for (int i = 0; i < 50; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(20f, 20f);
                int life = Main.rand.Next(30, 50);
                float scale = Main.rand.NextFloat(.5f, 1.1f);
                Color col = Color.Chocolate.Lerp(Color.White, Main.rand.NextFloat(.2f, .4f));
                ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel, life, scale, col, true, true);
                ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel * 2f, life / 2, scale, col, .8f, true);
            }
            ScreenShakeSystem.New(new(.2f, .2f), Projectile.Center);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Time > ThrowTime)
            fancy?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [lightColor]);

        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
