using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Buffs.Summon;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Late;

public class SuperIztMinion : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.LokiShrinep.Path;

    public override void SetStaticDefaults()
    {
        // This is necessary for right-click targeting
        ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

        ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
        ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
    }

    public sealed override void SetDefaults()
    {
        Projectile.scale = 1.8f;
        Projectile.width = (int) (78 * Projectile.scale);
        Projectile.height = (int) (22 * Projectile.scale);
        Projectile.tileCollide = false;
        Projectile.friendly = true;
        Projectile.minion = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.minionSlots = 1f;
        Projectile.penetrate = -1;

        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 12;
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
            return;

        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One * Projectile.scale, Projectile.Opacity,
            Projectile.rotation, 0, 40, 2, 2f, null, false, -.1f));

        // So it will lean slightly towards the direction it's moving
        Projectile.rotation = Projectile.velocity.X * 0.05f;

        // Search for targets
        NPC target = NPCTargeting.MinionHoming(new(Projectile.Center, 1200), Owner);
        if (target != null)
        {
            if (HasHitTarget)
                Projectile.velocity =
                    Projectile.velocity.RotatedBy((Projectile.identity % 2f == 0f).ToDirectionInt() * 0.06f,
                        default(Vector2)) * 0.93f;
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
            Owner.ClearBuff(ModContent.BuffType<SuperLoki>());

            return false;
        }

        if (Owner.HasBuff(ModContent.BuffType<SuperLoki>()))
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
            if (this.RunLocal())
                Projectile.NewProj(target.RandAreaInEntity(), Vector2.Zero, ModContent.ProjectileType<LokiBoom>(),
                    Projectile.damage, 0f, Projectile.owner);
            HitTime = 50;
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
            const float flySpeed = 60f;
            Projectile.velocity =
                Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(target) * flySpeed, 0.02f);
            ParticleRegistry.SpawnGlowParticle(Projectile.RandAreaInEntity(), -Projectile.velocity * .5f, 30,
                Main.rand.NextFloat(.05f, .09f), Color.Yellow, 1f);
        }
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(),
            [Color.Gold, Color.Goldenrod, Color.PaleGoldenrod], Projectile.Opacity);
        Projectile.DrawProjectileBackglow(Color.Gold, 2f, 30);
        Projectile.DrawBaseProjectile(Color.White);
        return false;
    }
}

public class LokiBoom : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;

    public override void SetDefaults()
    {
        Projectile.width = 60;
        Projectile.height = 60;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 10;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int) (Projectile.damage * 0.75);
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.OrangeRed.ToVector3() * 2f);
        if (Projectile.localAI[0] == 0f)
        {
            SoundID.DD2_FlameburstTowerShot.Play(Projectile.Center);
            Projectile.localAI[0] += 1f;
        }

        for (int i = 0; i < 10; i++)
        {
            Vector2 dustSpawnOffset = Main.rand.NextVector2Unit() *
                                      (float) Math.Pow(Main.rand.NextFloat(), 2.4) * Projectile.Size * 0.5f;
            Vector2 dustVelocity = dustSpawnOffset.SafeNormalize(Vector2.UnitY)
                .RotatedByRandom(MathHelper.PiOver2 * Main.rand.NextFloatDirection());
            Vector2 val = dustVelocity;
            Vector2 val2 = dustSpawnOffset / Projectile.Size / 0.5f;
            dustVelocity = val *
                           MathHelper.Lerp(1f, 5f, Utils.GetLerpValue(0.05f, 0.85f, ((Vector2) val2).Length(), false));

            Vector2 pos = Projectile.Center + dustSpawnOffset;
            ParticleRegistry.SpawnMistParticle(pos, dustVelocity, Main.rand.NextFloat(.7f, 1.5f), Color.Yellow,
                Color.Orange, 190, Main.rand.NextFloat(-.2f, .2f));
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.Size.Length() * .5f, targetHitbox);
    }
}
