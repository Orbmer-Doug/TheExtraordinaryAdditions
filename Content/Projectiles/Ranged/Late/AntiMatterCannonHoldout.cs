using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Ranged.Late;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class AntiMatterCannonHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ModContent.ItemType<AntiMatterCannon>();

    public override int IntendedProjectileType => ModContent.ProjectileType<AntiMatterCannonHoldout>();

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.AntiMatterCannon);

    public const int WaitTime = 78;
    public const int ReloadTime = 120;
    public ref float Wait => ref Projectile.ai[0];
    public ref float Time => ref Projectile.ai[1];
    public ref float Recoil => ref Projectile.ai[2];
    public ref float ReloadTimer => ref Projectile.AdditionsInfo().ExtraAI[0];
    public bool Reloading
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    public ref int Shots => ref Owner.GetModPlayer<AntiMatterPlayer>().Shots;

    public override void Defaults()
    {
        Projectile.width = 332;
        Projectile.height = 50;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public Vector2 Tip => Projectile.Center + PolarVector(166f, Projectile.rotation) + PolarVector(18f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public override void SafeAI()
    {
        Projectile.Opacity = InverseLerp(0f, 8f, Time);

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Mouse), .2f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());

        float anim = new Animators.PiecewiseCurve()
            .Add(0f, -1.2f, .3f, Animators.MakePoly(9f).OutFunction)
            .Add(-1.2f, 0f, 1f, Animators.MakePoly(3f).InOutFunction)
            .Evaluate(InverseLerp(WaitTime, 10f, Wait));
        float reel = new Animators.PiecewiseCurve()
            .Add(0f, .9f, .6f, Animators.MakePoly(3f).InFunction)
            .Add(.9f, 0f, 1f, Animators.MakePoly(4f).InOutFunction)
            .Evaluate(InverseLerp(0f, ReloadTime, ReloadTimer));

        Projectile.rotation = Projectile.velocity.ToRotation() + ((anim + reel) * Dir * Owner.gravDir);
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Owner.SetBackHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

        Projectile.Center = Center + PolarVector(96f - Recoil, Projectile.rotation);

        if (Shots >= AntiMatterPlayer.MaxShots && !Reloading)
            Reloading = true;
        if (Reloading)
        {
            if ((int)ReloadTimer == (int)(ReloadTime * .6f))
            {
                Vector2 pos = Projectile.Center - PolarVector(83f, Projectile.rotation) + PolarVector(8f * Dir, Projectile.rotation - MathHelper.PiOver2);

                SoundID.Item149.Play(pos, 1f, -.2f);
                if (Main.netMode != NetmodeID.Server)
                {
                    string goreType = "AntiMatterCartridge";
                    Vector2 vel = Projectile.rotation.ToRotationVector2().SafeNormalize(Vector2.Zero).RotatedBy(2f * -Dir) * Main.rand.NextFloat(2f, 6f);
                    Gore.NewGore(Projectile.GetSource_FromThis(), pos, vel, Mod.Find<ModGore>(goreType).Type);
                }
            }

            if (ReloadTimer > ReloadTime)
            {
                Shots = 0;
                ReloadTimer = 0;
                Reloading = false;
                this.Sync();
            }
            ReloadTimer++;
        }

        if ((this.RunLocal() && Modded.SafeMouseLeft.Current) && Wait <= 0f && !Reloading)
        {
            Modded.LungingDown = true;
            AdditionsSound.LargeSniperFire.Play(Tip, 1f, 0f, .15f, 2, Name);
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero);

            float playerSpeed = Owner.velocity.Length();
            Vector2 pushback = vel * -11f;
            Vector2 newPlayerVelocity = Owner.velocity + pushback;
            float newPlayerSpeed = newPlayerVelocity.Length();
            if (playerSpeed < 11f || newPlayerSpeed < playerSpeed)
                Owner.velocity = newPlayerVelocity;
            else
                Owner.velocity = newPlayerVelocity.SafeNormalize(Vector2.UnitX) * playerSpeed;

            for (int i = 0; i < 10; i++)
                ParticleRegistry.SpawnGlowParticle(Tip, Vector2.Zero, 10, Main.rand.NextFloat(90f, 120f), Color.OrangeRed, 1.3f);
            for (int i = 0; i < 40; i++)
            {
                ParticleRegistry.SpawnBloomLineParticle(Tip, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(14f, 30f), Main.rand.Next(8, 12), Main.rand.NextFloat(.5f, .9f), Color.OrangeRed);
            }
            ScreenShakeSystem.New(new(.88f, .4f), Tip);

            if (this.RunLocal())
            {
                Projectile.NewProj(Tip, vel * 15f, ModContent.ProjectileType<AntiBulletp>(), Projectile.damage, Projectile.knockBack, Owner.whoAmI);
                Vector2 pos = Projectile.Center - PolarVector(77f, Projectile.rotation) + PolarVector(18f * Dir * Owner.gravDir, Projectile.rotation - MathHelper.PiOver2);
                Projectile.NewProj(pos, -vel.RotatedBy(.5f * Dir * Owner.gravDir) * 10f, ModContent.ProjectileType<AntiBulletShell>(), 0, 0f, Owner.whoAmI);
            }
            Recoil = 40f;
            Wait = WaitTime;
            Shots++;
            Modded.LungingDown = false;
            this.Sync();
        }
        Recoil = MathHelper.Clamp(Animators.MakePoly(3f).OutFunction.Evaluate(Recoil, -.25f, .03f), 0f, 40f);
        if (Wait > 0f)
            Wait--;

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        float rotation = Projectile.rotation;
        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        Main.spriteBatch.Draw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), rotation, origin, Projectile.scale, FixedDirection(), 0f);
        return false;
    }
}

public class AntiMatterPlayer : ModPlayer
{
    public const int MaxShots = 4;
    public int Shots;
}