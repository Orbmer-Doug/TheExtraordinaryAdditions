using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Melee.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class HellsToothpickHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.HellsToothpickHandle.Path;
    public override int AssociatedItemID => ModContent.ItemType<HellsToothpick>();
    public override int IntendedProjectileType => ModContent.ProjectileType<HellsToothpickHeld>();

    public int Time
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int Delay
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.MouseWorld), .5f);
            if (Projectile.oldVelocity != Projectile.velocity)
                this.Sync();
        }

        float rot = Projectile.velocity.ToRotation();
        Owner.ChangeDir(AngleToXDirection(rot));
        Projectile.rotation = rot + MathHelper.PiOver2;
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, rot);
        Projectile.Center = Owner.GetFrontHandPositionImproved();

        Vector2 pos = Projectile.Center + PolarVector(Projectile.ThisProjectileTexture().Height * .4f, rot);
        for (int i = 0; i < 2; i++)
        {
            ParticleRegistry.SpawnHeavySmokeParticle(pos,
                PolarVector(Main.rand.NextFloat(.2f, 1f), RandomRotation()) -
                Vector2.UnitY * Main.rand.NextFloat(0f, 4f),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f),
                Color.OrangeRed.Lerp(Color.Red, Main.rand.NextFloat(0f, .2f)), Main.rand.NextFloat(.8f, 1f));
            ParticleRegistry.SpawnHeavySmokeParticle(pos, PolarVector(Main.rand.NextFloat(.2f, 1f), RandomRotation()),
                Main.rand.Next(20, 30), Main.rand.NextFloat(.5f, 1f),
                Color.OrangeRed * .4f, Main.rand.NextFloat(.1f, .2f));
        }

        if (Delay > 0)
            Delay--;

        int wait = Item.useAnimation;
        if (this.RunLocal() && Modded.SafeMouseLeft.Current && Delay == 0 && TryUseMana())
        {
            Projectile.CreateProj(pos, PolarVector(2f, rot),
                ModContent.ProjectileType<HellPick>(), Projectile.damage / 3, Projectile.knockBack, Projectile.owner);
            AssetRegistry.GennedSounds.IkeSpecial1A.Play(pos, .3f, .5f, .2f, 20);
            Delay = wait;
            this.Sync();
        }

        if (Delay == wait * 2 / 3)
        {
            if (this.RunLocal())
                Projectile.CreateProj(pos, PolarVector(4f, rot),
                    ModContent.ProjectileType<HellPick>(), Projectile.damage / 2, Projectile.knockBack, Projectile.owner);
            AssetRegistry.GennedSounds.IkeSpecial1A.Play(pos, .5f, .3f, .2f, 20);
        }

        if (Delay == wait * 1 / 3)
        {
            if (this.RunLocal())
                Projectile.CreateProj(pos, PolarVector(7f, rot),
                    ModContent.ProjectileType<HellPick>(), Projectile.damage, Projectile.knockBack, Projectile.owner);  
            AssetRegistry.GennedSounds.IkeSpecial1A.Play(pos, .7f, .2f, .2f, 20);
        }

        Lighting.AddLight(pos, Color.DarkOrange.ToVector3() * 1f);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, Projectile.GetAlpha(lightColor), Projectile.rotation,
            new Vector2(tex.Width / 2f, tex.Height), Projectile.scale, FixedDirection(true));
        return false;
    }
}

public class HellPick : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.height = Projectile.width = 24;
        Projectile.friendly = true;
        Projectile.extraUpdates = 50;
        Projectile.timeLeft = 50;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.penetrate = 1;
    }

    public override void AI()
    {
        Projectile.velocity *= .975f;

        float comp = InverseLerp(0f, 50f, Projectile.timeLeft);
        Color col = Color.OrangeRed;
        Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(4f, 4f);
        Vector2 vel = Vector2.UnitY * -Main.rand.NextFloat(0f, 2f);
        float scale = Main.rand.NextFloat(.35f, .4f) * Projectile.scale;
        ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, 50, scale, col, 1f);
        ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * .9f, 40, scale * 2.5f, col * .5f, .1f);
        ParticleRegistry.SpawnHeavySmokeParticle(pos, vel * .9f, 20, scale * 4.5f, col.Lerp(Color.Red, .5f) * .25f,
            .1f);

        Projectile.scale = comp;

        Projectile.ExpandHitboxBy(new Vector2(30f * comp));
    }
}
