using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Buff;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SolemnLamentProj : BaseHoldoutProjectile
{
    public override string Texture => AssetRegistry.Invis;

    public bool Swap
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public ref float GunType => ref Projectile.ai[1];
    public ref float Wait => ref Projectile.ai[2];
    public bool IsBlack
    {
        get => GunType == 0f;
        set => IsBlack.ToDirectionInt();
    }
    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float Recoil => ref Projectile.AdditionsInfo().ExtraAI[1];

    public int Firerate => Owner.HasBuff(ModContent.BuffType<EternalRest>()) ? 15 : 20;
    public override void Defaults()
    {
        Projectile.width = 44;
        Projectile.height = 42;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
    {
        if (!IsBlack)
        {
            behindNPCs.Add(index);
        }
    }

    public override void SafeAI()
    {
        if (Time == 0 && IsBlack)
            Swap = true;

        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (this.RunLocal())
        {
            Projectile.velocity = center.SafeDirectionTo(Modded.mouseWorld);
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
            Projectile.spriteDirection = Projectile.velocity.X.NonZeroSign();
        }

        float recoil = .4f * new PiecewiseCurve()
            .Add(0f, 1f, .4f, MakePoly(6).OutFunction)
            .Add(1f, 0f, 1f, MakePoly(2).InOutFunction)
            .Evaluate(1f - InverseLerp(4f, Firerate, Recoil)) * -Projectile.spriteDirection;
        float rot = Projectile.velocity.ToRotation() + recoil;

        if (IsBlack)
            Owner.SetBackHandBetter(0, rot);
        else
            Owner.SetFrontHandBetter(0, rot);

        Projectile.Center = center + PolarVector(Projectile.width / 2, rot)
            - (!IsBlack ? new Vector2(6 * Projectile.spriteDirection, 6f) : Vector2.Zero);
        Projectile.rotation = rot;
        Owner.ChangeDir(Projectile.spriteDirection);
        Projectile.timeLeft = Owner.itemTime = Owner.itemAnimation = 2;

        if (Recoil > 0f)
            Recoil--;

        Wait++;
        if (Wait % Firerate == Firerate - 1f)
        {
            if (!Swap)
            {
                Vector2 gunPos = Projectile.RotHitbox().Right;

                if (IsBlack)
                    AdditionsSound.SolemnB.Play(gunPos, .5f, 0f, 0f, 1, Name);
                else
                    AdditionsSound.SolemnW.Play(gunPos, .5f, 0f, 0f, 1, Name);

                int shootType = IsBlack ? ModContent.ProjectileType<SolemButterflyGrief>() : ModContent.ProjectileType<SolemButterflyLament>();
                int damage = Projectile.damage;
                if (IsBlack)
                    damage = (int)(damage * .75f);

                for (int i = 0; i < 10; i++)
                {
                    float shootSpeed = Owner.inventory[Owner.selectedItem].shootSpeed;

                    Vector2 vel = PolarVector(shootSpeed, Projectile.velocity.ToRotation());
                    Vector2 vele = Utils.RotatedByRandom(vel, 0.35) * Utils.NextFloat(Main.rand, .2f, 1f);

                    ParticleRegistry.SpawnSparkParticle(gunPos, vele, Main.rand.Next(50, 70), Main.rand.NextFloat(.6f, 1.1f), Color.Wheat);
                    if (this.RunLocal())
                        Projectile.NewProj(gunPos, vele, shootType, damage, Projectile.knockBack, Owner.whoAmI, 0f, 0f, 0f);
                }
                Recoil = Firerate;
            }

            Swap = !Swap;
            Wait = 0f;
        }

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        SpriteEffects spriteEffects = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Texture2D black = AssetRegistry.GetTexture(AdditionsTexture.SolemnLamentProjBlack);
        Texture2D white = AssetRegistry.GetTexture(AdditionsTexture.SolemnLamentProjWhite);
        Vector2 pos = Projectile.Center;

        float num = Projectile.spriteDirection == -1 ? MathHelper.Pi : 0f;
        if (IsBlack)
        {
            Main.EntitySpriteDraw(black, pos - Main.screenPosition,
                null, Projectile.GetAlpha(Color.White), Projectile.rotation + num, black.Size() / 2, Projectile.scale, spriteEffects, 0f);
        }
        else
        {
            Main.EntitySpriteDraw(white, pos - Main.screenPosition, null,
                Projectile.GetAlpha(lightColor), Projectile.rotation + num, white.Size() / 2, Projectile.scale, spriteEffects, 0f);
        }
        return false;
    }
}