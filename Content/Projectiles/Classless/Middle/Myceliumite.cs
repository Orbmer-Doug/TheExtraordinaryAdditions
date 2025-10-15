using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class Myceliumite : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public enum MyceliumType
    {
        Homing,
        Bouncy,
        Piercing,
        Exploding
    }

    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.timeLeft = 400;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 15;
        Projectile.DamageType = DamageClass.Generic;
    }

    public ref float Time => ref Projectile.ai[0];
    public MyceliumType Variant
    {
        get => (MyceliumType)Projectile.ai[1];
        set => Projectile.ai[1] = (float)value;
    }
    public ref float NeededTime => ref Projectile.ai[2];

    public Texture2D tex;
    public override void AI()
    {
        if (Projectile.AdditionsInfo().ExtraAI[0] == 0f)
        {
            // Weighted toward the homing version
            Variant = (MyceliumType)Main.rand.NextFromList(0, 0, 1, 2, 3);
            if (Variant == MyceliumType.Piercing)
            {
                NeededTime = Main.rand.Next(25, 45);
                Projectile.penetrate = 4;
            }

            Projectile.AdditionsInfo().ExtraAI[0] = 1f;
            this.Sync();
        }
        Time++;

        after ??= new(5, () => Projectile.Center);
        switch (Variant)
        {
            case MyceliumType.Homing:
                tex = AssetRegistry.GetTexture(AdditionsTexture.HomingMyceliumite);
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 30, 3, 1f, tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame), false, .2f));
                Projectile.height = 18;
                Projectile.width = 14;

                Projectile.SetAnimation(4, 5);

                if (Time > 15f)
                {
                    if (NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 600, true, true), out NPC target))
                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 12f, .18f);
                }

                Projectile.velocity.Y += .1f;
                break;
            case MyceliumType.Bouncy:
                tex = AssetRegistry.GetTexture(AdditionsTexture.BoingMyceliumite);
                Projectile.height = 20;
                Projectile.width = 14;

                Projectile.SetAnimation(4, 5);
                Projectile.velocity.Y += .2f;
                break;
            case MyceliumType.Piercing:
                tex = AssetRegistry.GetTexture(AdditionsTexture.PiercingMyceliumite);
                after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 30, 3, 1f, tex.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame), false, -.4f));
                Projectile.height = 26;
                Projectile.width = 14;
                Projectile.frame = 0;
                Projectile.tileCollide = false;

                if (Time > NeededTime)
                    Projectile.velocity = Projectile.velocity.RotatedBy(.15f * (Projectile.identity % 2f == 0f).ToDirectionInt());

                break;
            case MyceliumType.Exploding:
                tex = AssetRegistry.GetTexture(AdditionsTexture.ExplodingMyceliumite);
                Projectile.height = 26;
                Projectile.width = 18;
                Projectile.frame = 0;

                Projectile.velocity.Y += .4f;
                break;
        }
        Projectile.FacingUp();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        switch (Variant)
        {
            case MyceliumType.Homing:
                return true;
            case MyceliumType.Bouncy:
                if (Projectile.velocity.X != oldVelocity.X)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Projectile.velocity.Y != oldVelocity.Y)
                    Projectile.velocity.Y = -oldVelocity.Y;
                SoundID.Item56.Play(Projectile.Center, .3f, .4f, 0f, null, 40);
                return false;
            case MyceliumType.Piercing:
                return false;
            case MyceliumType.Exploding:
                return true;
        }

        return true;
    }

    public override void OnKill(int timeLeft)
    {
        switch (Variant)
        {
            case MyceliumType.Homing:
                for (int i = 0; i < 25; i++)
                    Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.GlowingMushroom, Projectile.velocity * Main.rand.NextFloat(.5f, .7f), 0, default, Main.rand.NextFloat(.4f, .5f));
                break;
            case MyceliumType.Bouncy:
                for (int i = 0; i < 16; i++)
                    Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.MushroomTorch, Projectile.velocity * Main.rand.NextFloat(.6f, .9f), 0, default, Main.rand.NextFloat(.6f, .8f));
                break;
            case MyceliumType.Piercing:
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    for (int j = 0; j < 3; j++)
                        Dust.NewDustPerfect(Projectile.oldPos[i], DustID.MushroomTorch, Projectile.velocity * Main.rand.NextFloat(.6f, .9f), 0, default, Main.rand.NextFloat(.8f, 1f));
                }
                break;
            case MyceliumType.Exploding:
                if (this.RunLocal())
                {
                    Projectile.penetrate = -1;
                    Projectile.ExpandHitboxBy(60);
                    Projectile.Damage();
                }
                for (int i = 0; i < Main.rand.Next(30, 40); i++)
                {
                    if (i % 2f == 1f)
                        ParticleRegistry.SpawnGlowParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(9f, 9f, .5f, 1f), Main.rand.Next(40, 60), 1f, Color.DodgerBlue);

                    Dust.NewDustPerfect(Projectile.RandAreaInEntity(), DustID.GlowingMushroom, Main.rand.NextVector2CircularLimited(14f, 14f, .5f, 1f), 0, default, Main.rand.NextFloat(.7f, .9f));
                }

                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, Vector2.One * 60f, Vector2.Zero, 40, Color.DodgerBlue, 0f, Color.DarkBlue, true);
                SoundID.Item14.Play(Projectile.Center, .9f, .4f, 0f, null, 20);
                break;
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        switch (Variant)
        {
            case MyceliumType.Homing:
                after?.DrawFancyAfterimages(tex, [new(32, 31, 110), new(34, 87, 142), new(30, 100, 172)], Projectile.Opacity);
                Projectile.DrawBaseProjectile(lightColor, 0, tex);
                break;
            case MyceliumType.Bouncy:
                Projectile.DrawProjectileBackglow(Color.BlueViolet * .8f, 5f, 0, 10, 0, null, tex);
                Projectile.DrawBaseProjectile(lightColor, 0, tex);
                break;
            case MyceliumType.Piercing:
                after?.DrawFancyAfterimages(tex, [new(32, 31, 110), new(34, 87, 142), new(30, 100, 172)], Projectile.Opacity);
                Projectile.DrawBaseProjectile(lightColor, 0, tex);
                break;
            case MyceliumType.Exploding:
                Projectile.DrawProjectileBackglow(new Color(30, 100, 172) * .6f, 4f, 40, 10, 0, null, tex);
                Projectile.DrawBaseProjectile(Color.White, 0, tex);
                break;
        }

        return false;
    }
}
