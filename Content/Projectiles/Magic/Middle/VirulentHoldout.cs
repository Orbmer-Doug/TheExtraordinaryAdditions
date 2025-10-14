using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class VirulentHoldout : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.VirulentEntrapment);
    public ref float Time => ref Projectile.ai[0];
    public ref float Radius => ref Projectile.ai[1];
    public ref float Rotate => ref Projectile.ai[2];
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 9;
    }

    public override void Defaults()
    {
        Projectile.width = 42;
        Projectile.height = 52;
        Projectile.DamageType = DamageClass.Magic;
    }

    public override void SafeAI()
    {

        Projectile.SetAnimation(9, 6);

        if (Owner.channel)
        {
            Projectile.timeLeft = 2;
            Owner.itemTime = 25;
            Owner.itemAnimation = 25;
            Owner.heldProj = Projectile.whoAmI;
        }

        Owner.ChangeDir((Mouse.X > Owner.Center.X).ToDirectionInt());
        float armPointingDirection = (Mouse - Owner.Center).SafeNormalize(Vector2.UnitX).ToRotation();
        Owner.SetFrontHandBetter(0, armPointingDirection);
        Owner.SetBackHandBetter(0, armPointingDirection);

        if (this.RunLocal())
        {
            float interpolant = Utils.GetLerpValue(5f, 20f, Projectile.Distance(Mouse), true);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(Mouse), interpolant);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true) + Projectile.velocity * Projectile.width * .5f;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Projectile.rotation = Projectile.velocity.ToRotation();

        // Do mana things
        if (Time % 7f == 0f)
        {
            Owner.CheckMana(4, true);
        }
        if (Owner.statMana <= 0)
        {
            Owner.statMana = 0;
            Projectile.Kill();
            return;
        }

        const float RadiusSize = 400f;

        float wait = 40f;
        if (Mouse.Distance(Projectile.Center) < RadiusSize && Time % wait == wait - 1f)
        {
            int type = ModContent.ProjectileType<VirulentPunch>();

            SoundID.Item14.Play(Mouse, 1f, -.2f, .1f);
            if (this.RunLocal())
                Projectile.NewProj(Mouse, Vector2.Zero, type, Projectile.damage, Projectile.knockBack, Owner.whoAmI);
        }

        // Expand radius
        Radius = Animators.MakePoly(3.3f).InOutFunction.Evaluate(0f, RadiusSize, InverseLerp(0f, 40f, Time));
        Projectile.Opacity = InverseLerp(0f, 14f, Time);

        // Create the zany and garnular aura
        Rotate = (Rotate + .01f) % MathHelper.TwoPi;
        float squish = MathF.Sin(Time * .02f) * 20f;
        float dustCount = MathHelper.TwoPi * Radius / 8f;
        for (int j = 0; j < dustCount; j++)
        {
            float interpolant = InverseLerp(0f, dustCount, j);
            float theta = -MathHelper.TwoPi * interpolant + Rotate;

            // Notes: 
            // Flooring the contents within sin creates funny lines
            // Making periods a non-whole number makes a lil curve animation around the aura (unlike in desmos, a krillion helical curves)
            float periods = 14f;
            float offset = 0f + (MathF.Cos(periods * theta + Rotate) + MathF.Sin(periods * theta + Rotate)) * squish;
            Vector2 position = Projectile.Center + PolarVector(Radius + offset, theta);
            Vector2 vel = offset.ToRotationVector2() * 1.2f;
            int life = 20;
            float scale = .8f;
            ParticleRegistry.SpawnDustParticle(position, vel, life, scale, Color.LawnGreen, .1f, false, true, false, false);
        }

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc != null && npc.Distance(Projectile.Center) < Radius && npc.IsAnEnemy())
                npc.AddBuff(BuffID.Poisoned, 120);
        }
        Vector2 pos = Projectile.Center + Main.rand.NextVector2Circular(Radius, Radius);

        if (Time % 3f == 2f)
            ParticleRegistry.SpawnDustParticle(pos, Main.rand.NextVector2Circular(1f, 1f), Main.rand.Next(50, 90), Main.rand.NextFloat(.5f, .9f),
                Color.Olive, Main.rand.NextFloat(-.1f, .1f), false, true, true, false);

        if (Time % 5f == 4f)
            ParticleRegistry.SpawnCloudParticle(pos, Main.rand.NextVector2Circular(3f, 3f), Color.LimeGreen, Color.DarkOliveGreen,
                Main.rand.Next(40, 65), Main.rand.NextFloat(.45f, .75f), Main.rand.NextFloat(.7f, .9f), Main.rand.NextByte(0, 3));

        if (Time % 30f == 29f && this.RunLocal())
            Projectile.NewProj(pos, Vector2.Zero, ModContent.ProjectileType<VirulentFlower>(), Projectile.damage, 0f, Owner.whoAmI);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        Rectangle frame = texture.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);
        Vector2 origin = frame.Size() * 0.5f;
        Main.spriteBatch.DrawBetter(texture, Projectile.Center, frame, Projectile.GetAlpha(Color.White), Projectile.rotation, origin, Projectile.scale, FixedDirection());
        return false;
    }
}