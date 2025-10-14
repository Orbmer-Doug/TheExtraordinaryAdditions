using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class BatSummon : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.BatSummon);
    public override void SetStaticDefaults()
    {
        Main.projFrames[Projectile.type] = 5;
        ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

        Main.projPet[Projectile.type] = false;

        ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
    }

    public sealed override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 20;
        Projectile.tileCollide = false;

        Projectile.friendly = true;
        Projectile.minion = true;
        Projectile.DamageType = DamageClass.MagicSummonHybrid;
        Projectile.minionSlots = .5f;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 60;
        Projectile.netUpdate = true;
    }

    public override bool? CanCutTiles() => false;
    public override bool MinionContactDamage() => true;

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];

        if (!CheckActive(owner))
            return;

        Visuals();
        NPC target = NPCTargeting.MinionHoming(new(Projectile.Center, 1200), Owner);
        if (target != null)
        {
            Utility.ProjAntiClump(Projectile, .3f);
            Charging(target.Center);
        }
        else
        {
            Utility.ProjAntiClump(Projectile, .15f);
            Hover();
        }
    }

    public Player Owner => Main.player[Projectile.owner];

    private void Visuals()
    {
        Projectile.spriteDirection = -Projectile.direction;
        Lighting.AddLight(Projectile.Center, Color.Orange.ToVector3() * .4f);

        Projectile.SetAnimation(Main.projFrames[Type], 7, true);
    }

    private void Charging(Vector2 t)
    {
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(t) * 60f, 0.02f);
    }

    private void Hover()
    {
        Vector2 idlePosition = Owner.Center;
        idlePosition.Y -= 48f;

        float minionPositionOffsetX = (10 + Projectile.minionPos * 30) * -Owner.direction;
        idlePosition.X += minionPositionOffsetX;

        float distance = 2000f;
        if (Projectile.WithinRange(idlePosition, distance) && !Projectile.WithinRange(idlePosition, 500f))
            Projectile.velocity = (idlePosition - Projectile.Center) / 30f;

        else if (!Projectile.WithinRange(idlePosition, 160f))
            Projectile.velocity = (Projectile.velocity * 37f + Projectile.SafeDirectionTo(idlePosition) * 17f) / 40f;

        if (!Projectile.WithinRange(idlePosition, distance))
        {
            Projectile.position = idlePosition;
            Projectile.velocity *= 0.3f;
        }

        Vector2 vectorToIdlePosition = idlePosition - Projectile.Center;
        float distanceToIdlePosition = vectorToIdlePosition.Length();
        if (Main.myPlayer == Owner.whoAmI && distanceToIdlePosition > 2000f)
        {
            Projectile.position = idlePosition;
            Projectile.velocity *= 0.1f;
            Projectile.netUpdate = true;
        }
    }

    private bool CheckActive(Player owner)
    {
        if (owner.dead || !owner.active)
        {
            owner.ClearBuff(ModContent.BuffType<MidnightBats>());
            return false;
        }

        if (owner.HasBuff(ModContent.BuffType<MidnightBats>()))
            Projectile.timeLeft = 2;
        return true;
    }
}