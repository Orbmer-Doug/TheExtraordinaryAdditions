using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Magic.Middle;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class IceMistHeld : BaseIdleHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.IceMistStaff);
    public override int AssociatedItemID => ModContent.ItemType<IceMistStaff>();
    public override int IntendedProjectileType => ModContent.ProjectileType<IceMistHeld>();
    public override void Defaults()
    {
        Projectile.width = 136;
        Projectile.height = 30;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override bool? CanDamage() => false;
    public ref float Time => ref Projectile.ai[0];
    public ref float Wait => ref Projectile.ai[1];
    public const int WaitTime = 20;
    public override void SafeAI()
    {
        if (!Owner.Available() && this.RunLocal())
        {
            Projectile.Kill();
            return;
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .6f);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Center = Center + PolarVector(Projectile.width / 2, Projectile.rotation);
        Owner.SetFrontHandBetter(0, Projectile.rotation);

        ParticleRegistry.SpawnCloudParticle(Center + Main.rand.NextVector2Circular(100f, 100f), Main.rand.NextVector2Circular(2, 2), Color.SkyBlue, Color.DarkViolet.Lerp(Color.DarkSlateBlue, .4f),
            Main.rand.Next(30, 50), Main.rand.NextFloat(70f, 120f), Main.rand.NextFloat(.3f, .5f));

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.CanHomeInto() && !npc.boss && npc.Center.WithinRange(Center, 130f))
                npc.AddBuff(BuffID.Frostburn2, Main.rand.Next(120, 140));
        }

        if (Main.rand.NextBool(4))
        {
            ParticleRegistry.SpawnDustParticle(Center + Main.rand.NextVector2Circular(100f, 100f), Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(30, 50), Main.rand.NextFloat(.2f, .4f), Color.White, .1f, false, true, true);
        }

        if (Modded.SafeMouseLeft.Current && Wait <= 0f && this.RunLocal())
        {
            Vector2 off = Main.rand.NextVector2CircularLimited(100f, 100f, .5f, 1f);
            Vector2 pos = Projectile.Center + off;
            FrostyIcicle icy = Main.projectile[Projectile.NewProj(pos, Projectile.velocity,
                ModContent.ProjectileType<FrostyIcicle>(), Projectile.damage, Projectile.knockBack, Main.myPlayer)].As<FrostyIcicle>();
            icy.Offset = off;

            SoundID.Item8.Play(pos, .9f, 0f, .1f, null, 10, Name);
            Wait = 10;
            this.Sync();
        }

        if (Wait > 0f)
            Wait--;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
