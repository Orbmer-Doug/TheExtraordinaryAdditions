using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class AmethystStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.AmethystStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<AmethystStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();

    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }

    public ref float ShootDelay => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 tip = Projectile.RotHitbox().TopRight + PolarVector(Item.height, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;

        Lighting.AddLight(tip, Color.Purple.ToVector3() * .12f);

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed;
            Projectile.NewProj(tip, vel, Item.shoot, Item.damage, Item.knockBack, Owner.whoAmI);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnSparkParticle(tip, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(.3f, .6f), Main.rand.Next(14, 25), Main.rand.NextFloat(.4f, .5f), Color.Purple);
            SoundID.Item43.Play(tip);

            ShootDelay = Owner.HeldItem.useTime;
            this.Sync();
        }

        if (ShootDelay > 0f)
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);
        return false;
    }
}
public class AmethystBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.AmethystBolt;
    public override void SetDefaults(Projectile proj)
    {
        proj.width = proj.height = 10;
        proj.DamageType = DamageClass.Magic;
        proj.friendly = true;
        proj.usesLocalNPCImmunity = true;
        proj.localNPCHitCooldown = 12;
        proj.Opacity = 0f;
        proj.penetrate = 1;
    }

    public override bool PreAI(Projectile proj)
    {
        ParticleRegistry.SpawnGlowParticle(proj.Center, -proj.velocity * Main.rand.NextFloat(.1f, .3f), Main.rand.Next(20, 30), proj.width * Main.rand.NextFloat(.8f, 1.4f), Color.Purple, 1.7f);
        ParticleRegistry.SpawnGlowParticle(proj.Center, proj.velocity * Main.rand.NextFloat(.1f, .3f), Main.rand.Next(20, 30), proj.width * Main.rand.NextFloat(.8f, 1.4f), Color.Magenta, 1.7f);

        Lighting.AddLight(proj.Center, Color.Purple.ToVector3());
        return false;
    }

    public override void OnKill(Projectile proj)
    {
        proj.velocity = Vector2.Zero;
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * i / 20 + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(2f, 5f);
            int life = Main.rand.Next(10, 15);
            float scale = Main.rand.NextFloat(.35f, .4f);
            ParticleRegistry.SpawnSparkParticle(proj.Center, vel, life, scale, Color.Purple);
        }
        proj.Resize(32, 32);
        proj.penetrate = -1;
        proj.Damage();

        SoundID.Item14.Play(proj.Center, .6f, .35f, .1f);
    }
}

public class TopazStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.TopazStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<TopazStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();

    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float ShootDelay => ref Projectile.ai[0];
    public ref float Charge => ref Projectile.ai[1];
    public const int MaxCharge = 120;
    public float Completion => InverseLerp(0f, MaxCharge, Charge);
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.width - 10, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;

        Lighting.AddLight(pos, Color.Orange.ToVector3() * (.13f + (Completion * .2f)));
        if (Modded.MouseLeft.Current && Charge < MaxCharge && this.RunLocal())
        {
            if (Charge % 2 == 1)
                ParticleRegistry.SpawnBloomPixelParticle(pos + Main.rand.NextVector2CircularLimited(40f, 40f, .4f, 1f),
                    Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(14, 20), Main.rand.NextFloat(.3f, .5f) * Completion, Color.Orange, Color.DarkOrange, pos, Completion, 2);

            Charge++;
            Projectile.netUpdate = true;
        }
        else if (!Modded.MouseLeft.Current && Charge > 0f && this.RunLocal())
            Charge--;

        if (Charge > 20f && Modded.CanUseMouseButton && Modded.SafeMouseLeft.JustReleased && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed * (Completion * 2f);
            int dmg = (int)(Item.damage * (Completion * 3f));
            float kb = Item.knockBack * (Completion * 2f);
            Projectile.NewProj(pos, vel, Item.shoot, dmg, kb, Owner.whoAmI, Completion);

            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnGlowParticle(pos, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(.3f, .6f) * Completion,
                    Main.rand.Next(14, 25), Main.rand.NextFloat(15.4f, 20.5f) * Completion, Color.Orange);
            SoundID.Item43.Play(pos, .2f + Completion);

            ShootDelay = Owner.HeldItem.useTime;
            Charge = 0f;

            Projectile.netUpdate = true;
        }

        if (ShootDelay > 0f && Projectile.FinalExtraUpdate())
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);

        if (Completion > 0f)
        {
            void draw()
            {
                Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
                Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.width - 10, Projectile.rotation - MathHelper.PiOver4);
                float rot = Projectile.rotation - MathHelper.PiOver4;
                Main.spriteBatch.DrawBetter(star, pos, null, Color.Orange * Completion, rot, star.Size() / 2, .15f * Completion, 0);
                Main.spriteBatch.DrawBetter(star, pos, null, Color.Orange.Lerp(Color.White, .5f) * Completion, rot, star.Size() / 2, .1f * Completion, 0);
            }
            PixelationSystem.QueueTextureRenderAction(draw, PixelationLayer.OverPlayers, BlendState.Additive);
        }

        return false;
    }
}
public class TopazBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.TopazBolt;
    public override bool PreAI(Projectile proj)
    {
        ref float power = ref proj.ai[0];
        ParticleRegistry.SpawnGlowParticle(proj.Center, proj.velocity * Main.rand.NextFloat(.2f, .5f), Main.rand.Next(20, 30), Main.rand.NextFloat(14.3f, 16.4f) * power, Color.Orange, 1.3f);
        ParticleRegistry.SpawnDustParticle(proj.Center, -proj.velocity.RotatedByRandom(.3f) * Main.rand.NextFloat(.3f, .4f), Main.rand.Next(30, 40), Main.rand.NextFloat(.2f, .4f) * power, Color.Orange, .1f, false, true);

        Lighting.AddLight(proj.Center, Color.Orange.ToVector3() * power);
        return false;
    }
    public override void OnKill(Projectile proj)
    {
        ref float power = ref proj.ai[0];
        for (int i = 0; i < 20; i++)
        {
            ParticleRegistry.SpawnGlowParticle(proj.Center, proj.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.2f, .5f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(10.3f, 11.4f) * power, Color.Orange);
        }
    }
}

public class SapphireStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.SapphireStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<SapphireStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();
    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float ShootDelay => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.height, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;
        Lighting.AddLight(pos, Color.Blue.ToVector3() * .18f);

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed;
            Projectile.NewProj(pos, vel, Item.shoot, Item.damage, Item.knockBack, Owner.whoAmI);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnBloodParticle(pos, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.2f, .65f), Main.rand.Next(18, 28), Main.rand.NextFloat(.4f, .5f), Color.Blue);

            SoundID.Item43.Play(pos);

            ShootDelay = Owner.HeldItem.useTime;
            Projectile.netUpdate = true;
        }
        if (ShootDelay > 0f && Projectile.FinalExtraUpdate())
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);
        return false;
    }
}
public class SapphireBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.SapphireBolt;
    public override bool PreAI(Projectile proj)
    {
        ParticleRegistry.SpawnCloudParticle(proj.Center, proj.velocity * Main.rand.NextFloat(-.1f, .1f),
            Color.LightBlue, Color.DarkBlue, Main.rand.Next(18, 24), Main.rand.NextFloat(proj.width - 2f, proj.width) * 3f, Main.rand.NextFloat(.5f, .7f), 1);
        ParticleRegistry.SpawnSparkParticle(proj.Center, -proj.velocity * Main.rand.NextFloat(.2f, .3f),
            Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f), Color.Blue);

        Lighting.AddLight(proj.Center, Color.Blue.ToVector3());
        return false;
    }
    public override void OnKill(Projectile proj)
    {
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnGlowParticle(proj.Center, Main.rand.NextVector2Circular(3f, 3f), Main.rand.Next(20, 30), Main.rand.NextFloat(13f, 18f), Color.DarkBlue);
        }
    }
    public override void ModifyHitNPC(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.Knockback += .4f;
    }
}

public class EmeraldStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.EmeraldStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<EmeraldStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();
    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float ShootDelay => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.height, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;
        Lighting.AddLight(pos, Color.LawnGreen.ToVector3() * .2f);

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed;
            Projectile.NewProj(pos, vel, Item.shoot, Item.damage, Item.knockBack, Owner.whoAmI);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(.4f, .65f), Main.rand.Next(18, 30),
                    Main.rand.NextFloat(.4f, .5f), Color.Green, Color.LawnGreen);

            SoundID.Item43.Play(pos);

            ShootDelay = Owner.HeldItem.useTime;
            Projectile.netUpdate = true;
        }
        if (ShootDelay > 0f && Projectile.FinalExtraUpdate())
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);
        return false;
    }
}
public class EmeraldBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.EmeraldBolt;
    public override bool PreAI(Projectile proj)
    {
        for (int i = 0; i < 3; i++)
            ParticleRegistry.SpawnGlowParticle(proj.Center, -proj.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.1f, .2f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(19.3f, 20.4f), Color.LawnGreen, 2f);

        return false;
    }

    public override void OnKill(Projectile proj)
    {
        for (int i = 0; i < 18; i++)
            ParticleRegistry.SpawnGlowParticle(proj.Center, proj.velocity.RotatedByRandom(.5f) * Main.rand.NextFloat(.01f, .1f), Main.rand.Next(40, 50), Main.rand.NextFloat(14f, 21f), Color.LawnGreen, 1.5f);
    }

    public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.rand.NextBool())
            Main.player[proj.owner].Heal(Main.rand.Next(1, 3));
    }
}

public class AmberStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.AmberStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<AmberStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();
    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float ShootDelay => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.height, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;
        Color col = new(191, 108, 0);
        Lighting.AddLight(pos, col.ToVector3() * .22f);

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed;
            Projectile.NewProj(pos, vel, Item.shoot, Item.damage, Item.knockBack, Owner.whoAmI);
            for (int i = 0; i < 14; i++)
                ParticleRegistry.SpawnDustParticle(pos, vel.RotatedByRandom(.4f) * Main.rand.NextFloat(.3f, .6f),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .5f), col, .14f, false, true);

            SoundID.Item43.Play(pos);

            ShootDelay = Owner.HeldItem.useTime;
            Projectile.netUpdate = true;
        }
        if (ShootDelay > 0f && Projectile.FinalExtraUpdate())
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);
        return false;
    }
}
public class AmberBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.AmberBolt;
    public readonly Color col = new(byte.MaxValue, 196, 48);
    public override bool PreAI(Projectile proj)
    {
        ParticleRegistry.SpawnGlowParticle(proj.Center, -proj.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.2f, .4f), Main.rand.Next(20, 30), Main.rand.NextFloat(18f, 28f), col);
        ParticleRegistry.SpawnHeavySmokeParticle(proj.Center, -proj.velocity * Main.rand.NextFloat(.2f, .4f), Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), col);

        return false;
    }

    public override void OnKill(Projectile proj)
    {
        for (int i = 0; i < 15; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * i / 15 + RandomRotation()).ToRotationVector2() * Main.rand.NextFloat(2f, 3f);
            ParticleRegistry.SpawnDustParticle(proj.Center, vel, Main.rand.Next(20, 30), Main.rand.NextFloat(.3f, .5f), col, .1f, false, true);
        }
    }

    public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (proj.numHits > 2)
            return;

        if (NPCTargeting.TryGetClosestNPC(new(proj.Center, 200, true, false, [target]), out NPC n))
        {
            proj.velocity = proj.SafeDirectionTo(n.Center) * 6f;
            proj.netUpdate = true;
        }
    }
}

public class RubyStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.RubyStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<RubyStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();
    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float ShootDelay => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.height, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;
        Lighting.AddLight(pos, Color.Red.ToVector3() * .22f);

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed;
            Projectile.NewProj(pos, vel, Item.shoot, Item.damage, Item.knockBack, Owner.whoAmI);
            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnHeavySmokeParticle(pos, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(.3f, .6f), Main.rand.Next(14, 25), Main.rand.NextFloat(.4f, .5f), Color.Red);
            SoundID.Item43.Play(pos);

            ShootDelay = Owner.HeldItem.useTime;
            Projectile.netUpdate = true;
        }
        if (ShootDelay > 0f && Projectile.FinalExtraUpdate())
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);
        return false;
    }
}
public class RubyBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.RubyBolt;
    public override bool PreAI(Projectile proj)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector2 vel = -proj.velocity * Main.rand.NextFloat(.2f, .4f);
            ParticleRegistry.SpawnHeavySmokeParticle(proj.Center, vel.RotatedByRandom(.15f), Main.rand.Next(20, 30), Main.rand.NextFloat(.1f, .2f), Color.Crimson);
            ParticleRegistry.SpawnGlowParticle(proj.Center, vel, Main.rand.Next(16, 23), Main.rand.NextFloat(20.3f, 36.4f), Color.Red, .7f);
        }
        Lighting.AddLight(proj.Center, Color.Red.ToVector3());

        return false;
    }
    public override void OnHitNPC(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Main.rand.NextBool())
            target.AddBuff(BuffID.OnFire, 120);
    }
    public override void OnKill(Projectile proj)
    {
        for (int i = 0; i < 18; i++)
        {
            ParticleRegistry.SpawnHeavySmokeParticle(proj.Center, proj.velocity.RotatedByRandom(.35f) * Main.rand.NextFloat(.3f, .4f), Main.rand.Next(25, 38), Main.rand.NextFloat(.2f, .4f), Color.Red);
            ParticleRegistry.SpawnGlowParticle(proj.Center, proj.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.3f, .5f), Main.rand.Next(20, 28), Main.rand.NextFloat(20f, 30f), Color.Red, .5f);
        }
    }
}

public class DiamondStaffHoldout : BaseIdleHoldoutProjectile
{
    public override int AssociatedItemID => ItemID.DiamondStaff;
    public override int IntendedProjectileType => ModContent.ProjectileType<DiamondStaffHoldout>();
    public override string Texture => AssociatedItemID.GetTerrariaItem();
    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 40;
        Projectile.DamageType = DamageClass.Magic;
    }
    public ref float ShootDelay => ref Projectile.ai[0];
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Center.SafeDirectionTo(Mouse);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        Vector2 pos = Projectile.RotHitbox().TopRight + PolarVector(Item.height, Projectile.rotation - MathHelper.PiOver4);
        Owner.SetFrontHandBetter(0, Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.GetFrontHandPositionImproved() + Vector2.UnitY * Owner.gfxOffY;
        Lighting.AddLight(pos, Color.White.ToVector3() * .3f);

        if (Modded.SafeMouseLeft.Current && ShootDelay <= 0 && this.RunLocal())
        {
            Vector2 vel = Projectile.velocity * Item.shootSpeed;
            Projectile p = Main.projectile[Projectile.NewProj(pos, vel, Item.shoot, Item.damage, Item.knockBack, Owner.whoAmI)];
            p.ai[1] = Item.shootSpeed;

            for (int i = 0; i < 12; i++)
                ParticleRegistry.SpawnSparkleParticle(pos, vel.RotatedByRandom(.5f) * Main.rand.NextFloat(.3f, .6f), Main.rand.Next(18, 30), Main.rand.NextFloat(.4f, .5f), Color.White, Color.LightCyan, 1.2f);

            SoundID.Item43.Play(pos);

            ShootDelay = Owner.HeldItem.useTime;
            Projectile.netUpdate = true;
        }
        if (ShootDelay > 0f && Projectile.FinalExtraUpdate())
            ShootDelay--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 orig = new(0, tex.Height);
        SpriteEffects fx = SpriteEffects.None;
        float off = 0f;
        if (Projectile.direction == -1)
        {
            orig = new(tex.Width, tex.Height);
            fx = SpriteEffects.FlipHorizontally;
            off = MathHelper.PiOver2;
        }
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation + off, orig, 1, fx);
        return false;
    }
}
public class DiamondBoltOverride : ProjectileOverride
{
    public override int ProjectileOverrideType => ProjectileID.DiamondBolt;
    public override bool PreAI(Projectile proj)
    {
        ref float time = ref proj.ai[0];
        Player owner = Main.player[proj.owner];
        for (int i = 0; i < 2; i++)
        {
            if (i == 0 && Main.rand.NextBool(3))
                ParticleRegistry.SpawnBloomPixelParticle(proj.Center, -proj.velocity.RotatedByRandom(.24f) * Main.rand.NextFloat(.35f, .5f),
                    Main.rand.Next(20, 26), Main.rand.NextFloat(.4f, .5f), Color.WhiteSmoke, Color.LightCyan);

            ParticleRegistry.SpawnGlowParticle(proj.Center, -proj.velocity * Main.rand.NextFloat(.2f, .4f),
                Main.rand.Next(10, 24), Main.rand.NextFloat(18.3f, 24.4f), Color.WhiteSmoke, 1.3f);
        }

        if (time.BetweenNum(15f, 40f))
        {
            if (Main.myPlayer == owner.whoAmI)
            {
                proj.velocity = Vector2.SmoothStep(proj.velocity, proj.SafeDirectionTo(owner.Additions().mouseWorld) * proj.ai[1], .1f);
                if (proj.velocity != proj.oldVelocity)
                    proj.netUpdate = true;
            }
        }
        else if (proj.velocity.Length() < proj.ai[1])
            proj.velocity *= 1.01f;

        time++;
        Lighting.AddLight(proj.Center, Color.WhiteSmoke.ToVector3());
        return false;
    }
    public override void OnKill(Projectile proj)
    {
        for (int i = 0; i < 16; i++)
            ParticleRegistry.SpawnGlowParticle(proj.Center, (MathHelper.TwoPi * i / 16).ToRotationVector2() * Main.rand.NextFloat(1f, 2f), Main.rand.Next(20, 30), Main.rand.NextFloat(22f, 30f), Color.WhiteSmoke);
    }
}