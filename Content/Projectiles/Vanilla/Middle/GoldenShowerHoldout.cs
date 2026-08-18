using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class GoldenShowerHoldout : BaseIdleHoldoutProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.GoldenShower;
    public override int AssociatedItemID => ItemID.GoldenShower;
    public override int IntendedProjectileType => ModContent.ProjectileType<GoldenShowerHoldout>();

    public override void Defaults()
    {
        Projectile.width = 24;
        Projectile.height = 28;
        Projectile.scale = .9f;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
    }

    public ref float Time => ref Projectile.ai[0];
    public ref float Delay => ref Projectile.ai[1];

    public override void SafeAI()
    {
        Item item = Owner.HeldItem;

        if (Modded.SafeMouseLeft.Current && Delay <= 0 && this.RunLocal())
        {
            SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);
            Delay = item.useAnimation;
        }

        if (Delay > 0)
            Delay--;

        Vector2 pos = Projectile.Center + PolarVector(10f, Projectile.rotation);
        if (Delay % item.useTime == item.useTime - 1 && this.RunLocal() && TryUseMana(false))
        {
            Vector2 vel = Projectile.velocity;

            vel *= item.shootSpeed;

            Projectile.NewProj(pos, vel, ModContent.ProjectileType<IchorStream>(), item.damage, item.knockBack,
                Owner.whoAmI);
        }

        int wait = item.useAnimation * 2;
        if (Time % wait == wait - 1 && Modded.SafeMouseRight.Current && TryUseMana() && !Modded.MouseLeft.Current &&
            this.RunLocal())
        {
            SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);

            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = -Vector2.UnitY.RotatedByRandom(.36f) * item.shootSpeed * Main.rand.NextFloat(.66f, 1f);
                SoundEngine.PlaySound(SoundID.Item13, Projectile.Center);
                Projectile.NewProj(pos, vel, ModContent.ProjectileType<IchorSwirl>(), item.damage, item.knockBack * 2,
                    Owner.whoAmI, 0f, 1f);
            }
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Modded.MouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Projectile.Center = Owner.GetFrontHandPositionImproved() - PolarVector(5f, Projectile.rotation);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height / 2);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation,
            orig, 1, FixedDirection());
        return false;
    }
}

public class IchorStream : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.width =
            Projectile.height = 32;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.penetrate = 5;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float Time => ref Projectile.ai[0];

    public override void AI()
    {
        Projectile.scale -= 0.002f;
        if (Projectile.scale <= 0f)
            Projectile.Kill();

        if (Time > 1f)
        {
            Projectile.velocity.Y += 0.075f;
            if (Main.rand.NextBool(5))
            {
                float scale = Main.rand.NextFloat(.4f, .8f);
                ParticleRegistry.SpawnBloodParticle(Projectile.Center,
                    Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.3f, .6f),
                    Main.rand.Next(25, 40), scale, Color.Gold);
            }

            for (int i = 0; i < 3; i++)
            {
                Dust ichor = Dust.NewDustPerfect(Projectile.Center, DustID.Ichor, null, 100);
                ichor.noGravity = true;
                ichor.velocity *= .25f;
                ichor.velocity += Projectile.velocity / 2;
            }

            if (Main.rand.NextBool(8))
            {
                Dust fall = Dust.NewDustPerfect(Projectile.Center, DustID.Ichor, null, 100, default, .5f);
                fall.velocity *= .25f;
                fall.velocity += Projectile.velocity / 2;
            }
        }

        Time++;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Vanilla
        if (target.IsDestroyer() || target.type == NPCID.Probe)
            modifiers.FinalDamage *= .75f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // convenient the ichor id is 69...
        target.AddBuff(BuffID.Ichor, 600);
    }
}

public class IchorSwirl : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.Size = new(32);
        Projectile.hostile = false;
        Projectile.friendly = Projectile.tileCollide = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.extraUpdates = 2;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.localNPCHitCooldown = 10;
    }

    public ref float Time => ref Projectile.ai[0];

    public override void AI()
    {
        Projectile.scale -= 0.002f;
        if (Projectile.scale <= 0f)
            Projectile.Kill();

        if (Time > 3f)
        {
            Projectile.velocity.Y += 0.075f;
            if (Main.rand.NextBool(5))
            {
                float scale = Main.rand.NextFloat(.4f, .8f);
                ParticleRegistry.SpawnBloodParticle(Projectile.Center,
                    Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.3f, .6f), Main.rand.Next(25, 40),
                    scale, Color.Gold);
            }

            int offset = 16;
            Vector2 pos = new(Projectile.position.X + offset, Projectile.position.Y + offset);
            ref float Offset = ref Projectile.ai[2];
            Offset += .09f * (Projectile.identity % 2f == 1f).ToDirectionInt() % MathHelper.TwoPi;
            int arms = 6;
            for (int i = 0; i < arms; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / arms + Offset).ToRotationVector2().RotatedBy(20) * 12f;
                Dust swirl = Dust.NewDustPerfect(pos, DustID.Ichor, vel, 100, default, 1.1f);
                swirl.velocity *= 0.25f;
                swirl.velocity += Projectile.velocity / 2;
                swirl.noGravity = true;
            }
        }

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // convenient the ichor id is 69...
        target.AddBuff(BuffID.Ichor, 600);
        Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
        ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 72f, Vector2.Zero,
            15, Color.Gold, null, Color.Goldenrod);
        for (int i = 0; i < 10; i++)
            ParticleRegistry.SpawnMistParticle(Projectile.RandAreaInEntity(), RandomVelocity(4f, 2f, 10f),
                Main.rand.NextFloat(.2f, .4f), Color.Gold, Color.DarkGoldenrod, Main.rand.NextByte(130, 190));

        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Vector2.Zero, ModContent.ProjectileType<IchorStreamBlast>(),
                (int) (Projectile.damage * .75f), 0f, Projectile.owner);
    }
}

public class IchorStreamBlast : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.width = 72;
        Projectile.height = 72;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 10;
        Projectile.MaxUpdates = 3;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 7;

        Projectile.hostile = false;
        Projectile.friendly = true;
    }
}
