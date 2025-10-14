using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class SnareHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.NoxiousSnare);

    public ref float Time => ref Projectile.ai[0];
    public ref float Radius => ref Projectile.ai[1];
    public ref float Spin => ref Projectile.ai[2];

    public override void Defaults()
    {
        Projectile.width = 28;
        Projectile.height = 30;
        Projectile.DamageType = DamageClass.Magic;
    }

    public const float MaxRadius = 220f;
    public const int SporeWait = 40;
    public override void SafeAI()
    {
        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Mouse), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Mouse), interpolant);
            if (Projectile.oldVelocity != Projectile.velocity)
                this.Sync();
        }

        Projectile.Center = Center + Projectile.velocity * Projectile.width * .5f;
        Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);

        Projectile.timeLeft = 2;
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = 2;
        Owner.itemAnimation = 2;
        Projectile.rotation = Projectile.velocity.ToRotation();
        Owner.itemRotation = Projectile.rotation;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Owner.SetFrontHandBetter(0, Projectile.rotation);
        Owner.SetBackHandBetter(0, Projectile.rotation);

        Projectile.Opacity = InverseLerp(0f, 20f, Time);

        if (Mouse.Distance(Projectile.Center) < MaxRadius && Time % SporeWait == SporeWait - 1f && Owner.HeldItem.CheckManaBetter(Owner, 3, true))
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 newVelocity = (Center.SafeDirectionTo(Mouse) * 12f).RotatedByRandom(.3f) * Main.rand.NextFloat(.1f, .7f);

                for (int j = 0; j < 14; j++)
                {
                    ParticleRegistry.SpawnDustParticle(Projectile.Center, Main.rand.NextVector2Circular(8f, 8f), Main.rand.Next(20, 30), Main.rand.NextFloat(.8f, 1.2f), Color.LawnGreen, .1f, false, true, true);

                    ParticleRegistry.SpawnMistParticle(Mouse, newVelocity * Main.rand.NextFloat(1.2f, 1.4f), Main.rand.NextFloat(.35f, .67f), Color.LawnGreen, Color.Olive, Main.rand.NextFloat(100f, 140f));
                }
                ParticleRegistry.SpawnPulseRingParticle(Mouse, newVelocity.SafeNormalize(Vector2.Zero), 20, newVelocity.ToRotation(), new(.5f, 1f), 0f, 60f, Color.LawnGreen);

                int type = ModContent.ProjectileType<SnareGas>();

                SoundID.Grass.Play(Mouse, 1f, 0f, .2f);
                if (this.RunLocal())
                    Projectile.NewProj(Mouse, newVelocity, type, Projectile.damage, Projectile.knockBack, Owner.whoAmI);
            }
        }

        // Expand radius
        Spin = (Spin + .005f) % MathHelper.TwoPi;
        Radius = Animators.MakePoly(3f).InOutFunction.Evaluate(0f, MaxRadius, InverseLerp(0f, 30f, Time));
        float dustCount = MathHelper.TwoPi * Radius / 8f;
        for (int j = 0; j < dustCount; j++)
        {
            float angle = MathHelper.TwoPi * j / dustCount + Spin;
            Dust obj = Dust.NewDustPerfect(Projectile.Center, DustID.Grass, null, 0, default, 1f);
            obj.position = Projectile.Center + angle.ToRotationVector2() * Radius;
            obj.scale = 0.7f;
            obj.noGravity = true;
            obj.velocity = Vector2.Zero;
        }

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc != null && npc.Distance(Projectile.Center) < Radius)
                npc.AddBuff(BuffID.Poisoned, 60);
        }
        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        SpriteEffects effects = FixedDirection();
        Vector2 origin = frame.Size() * 0.5f;
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, frame, Projectile.GetAlpha(lightColor), Projectile.rotation, origin, Projectile.scale, effects);
        return false;
    }
}