using System;
using Terraria;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class LightningNode : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LightningNode);

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(16);
        Projectile.tileCollide = Projectile.friendly = false;
        Projectile.ignoreWater = Projectile.hostile = true;
    }

    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }
    public bool Channeling
    {
        get => Projectile.ai[1] == 1;
        set => Projectile.ai[1] = value.ToInt();
    }
    public bool ChosePosition
    {
        get => Projectile.ai[2] == 1;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float OrbitOffsetAngle => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float OrbitSquish => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float OrbitRadius => ref Projectile.AdditionsInfo().ExtraAI[2];

    public override void SafeAI()
    {
        if (Target == null)
            return;

        Projectile.timeLeft = 500;
        if (Channeling)
        {
            Projectile.frame = 1;
            Lighting.AddLight(Projectile.Center, Color.DeepSkyBlue.ToVector3() * 2f);
        }
        else
        {
            Projectile.frame = 0;
        }

        if (!ModOwner.Tesselestic_Shooting)
        {
            if (!ChosePosition)
            {
                Projectile.AI_GetMyGroupIndex(out int index, out int group);
                float indexInterpol = InverseLerp(0f, group, index);
                float radiusInterpolant = indexInterpol * 0.85f + MathF.Sqrt(Main.rand.NextFloat()) * 0.15f;
                OrbitOffsetAngle = RandomRotation();

                float dist = Owner.Distance(Target.Center);
                float rand = Main.rand.NextFloat(50f, 1000f);
                OrbitRadius = MathHelper.Lerp(dist, dist + rand, indexInterpol);
                OrbitSquish = Main.rand.NextFloat(0.75f, 1f);

                ChosePosition = true;
                this.Sync();
            }
        }
        else
        {
            OrbitOffsetAngle += (MathHelper.TwoPi / OrbitRadius * Asterlin.Tesselestic_NodeRotationAmt
                * InverseLerp(Asterlin.Tesselestic_FireTime, 0f, ModOwner.Tesselestic_AttackTime))
                * (Owner.Center.X > Target.Center.X).ToDirectionInt();

            if (ChosePosition)
            {
                ChosePosition = false;
                Projectile.netUpdate = true;
            }
        }

        Vector2 offset = OrbitOffsetAngle.ToRotationVector2() * OrbitRadius * new Vector2(1f, OrbitSquish);
        Vector2 target = ModOwner.Staff.TipOfStaff + offset;
        Projectile.SmoothFlyNear(target, Projectile.Opacity * 0.04f, .12f);

        Projectile p = ProjectileTargeting.GetClosestProjectile(new(Projectile.Center, 2000, false, Type, [Projectile]));
        if (p != null)
            Projectile.rotation = Projectile.rotation.AngleLerp(Projectile.AngleTo(p.Center), .2f);

        Time++;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}
