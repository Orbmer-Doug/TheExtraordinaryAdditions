using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

public class IztMinion : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.LokiShrinep);
    public override void SetStaticDefaults()
    {
        // This is necessary for right-click targeting
        ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

        ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
    }

    public sealed override void SetDefaults()
    {
        Projectile.width = 78;
        Projectile.height = 22;
        Projectile.tileCollide = false;
        Projectile.friendly = true;
        Projectile.minion = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.minionSlots = .5f;
        Projectile.penetrate = -1;
        Projectile.netUpdate = true;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override bool? CanCutTiles() => false;
    public override bool MinionContactDamage() => true;

    public Player Owner => Main.player[Projectile.owner];
    public ref float Timer => ref Projectile.ai[0];
    public ref float HitTime => ref Projectile.ai[1];
    public bool HasHitTarget
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public override void AI()
    {
        if (!CheckActive())
        {
            return;
        }
        
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0, 255));

        // So it will lean slightly towards the direction it's moving
        Projectile.rotation = Projectile.velocity.X * 0.05f;

        // Search for targets
        NPC target = NPCTargeting.MinionHoming(new(Projectile.Center, 900), Owner);
        if (target != null)
        {
            if (HasHitTarget)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy((double)((Projectile.identity % 2f == 0f).ToDirectionInt() * 0.06f), default) * 0.93f;
            }
            else
                TargetPosition(target.Center);
        }
        else
        {
            // Hover near player
            FollowOrigin(Owner.Center);
        }

        if (HitTime > 0)
            HitTime--;
        if (HitTime <= 0)
            HasHitTarget = false;

        Projectile.ProjAntiClump(.1f);
    }

    private bool CheckActive()
    {
        if (Owner.dead || !Owner.active)
        {
            Owner.ClearBuff(ModContent.BuffType<LokiBuff>());

            return false;
        }

        if (Owner.HasBuff(ModContent.BuffType<LokiBuff>()))
        {
            Projectile.timeLeft = 2;
        }

        return true;
    }

    public void FollowOrigin(Vector2 origin)
    {
        if (Projectile.WithinRange(origin, 1200f) && !Projectile.WithinRange(origin, 300f))
        {
            Projectile.velocity = (origin - Projectile.Center) / 30f;
        }
        else if (!Projectile.WithinRange(origin, 160f))
        {
            Projectile.velocity = (Projectile.velocity * 37f + Projectile.SafeDirectionTo(origin) * 17f) / 40f;
        }
        if (!Projectile.WithinRange(origin, 1200f))
        {
            Projectile.position = origin;
            Projectile.velocity *= 0.3f;
        }
    }
    
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HasHitTarget)
        {
            HitTime = 60;
            Projectile.velocity *= 2f;
            HasHitTarget = true;
            Projectile.netUpdate = true;
        }

    }

    public void TargetPosition(Vector2 target)
    {
        // Attack in some wierd collective way
        // Its funny when there is a bunch of enemies because they start to run down the chain individually
        if (HitTime <= 0)
        {
            float flySpeed = 30f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target) * flySpeed, 0.02f);
            ParticleRegistry.SpawnGlowParticle(Projectile.RandAreaInEntity(), -Projectile.velocity * Main.rand.NextFloat(.4f, .6f), 16, Main.rand.NextFloat(.09f, .2f), Color.Gold);
        }
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.Yellow], Projectile.Opacity);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }
}
