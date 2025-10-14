using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Middle;

public class KrakenTentacle : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.KrakenTentacle);
    public ref float Time => ref Projectile.ai[0];
    public bool Init
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public Projectile Kraken => Main.projectile[(int)Projectile.ai[2]];
    public bool Attack
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public ref float AttackWait => ref Projectile.AdditionsInfo().ExtraAI[1];
    public float WaitTime => 40f;
    public ref float Spin => ref Projectile.AdditionsInfo().ExtraAI[2];

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public const int MaxSegments = 10;
    public List<VerletSimulatedSegment> segments;

    public override void SetDefaults()
    {
        Projectile.aiStyle = -1;
        Projectile.width = Projectile.height = 28;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
        Projectile.penetrate = -1;
        Projectile.usesLocalNPCImmunity = Projectile.friendly = Projectile.ignoreWater = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        if (!Owner.Available() || Kraken == null || !Kraken.active)
        {
            Projectile.Kill();
            return;
        }
        else
            Projectile.timeLeft = 120;

        if (!Init)
        {
            segments = new List<VerletSimulatedSegment>(MaxSegments);
            for (int i = 0; i < MaxSegments; i++)
            {
                VerletSimulatedSegment segment = new(Projectile.Center);
                segments.Add(segment);
            }
            segments[0].locked = true;

            Init = true;
        }

        Projectile.Center = Kraken.Center;

        if (segments == null)
        {
            segments = new List<VerletSimulatedSegment>(MaxSegments);
            for (int i = 0; i < MaxSegments; i++)
                segments[i] = new VerletSimulatedSegment(Projectile.Center);
        }
        segments[0].oldPosition = segments[0].position;
        segments[0].position = Projectile.Center;
        segments = VerletSimulatedSegment.SimpleSimulation(segments, 12f);

        if (AttackWait > 0f)
            AttackWait--;
        if (AttackWait <= 0)
            Attack = true;

        if (Attack && NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 200, true, true), out NPC target))
        {
            segments[^1].position += segments[^1].position.SafeDirectionTo(target.Center) * 10f;
        }
        else
        {
            Projectile.AI_GetMyGroupIndex(out int index, out int group);
            Vector2 pos = Kraken.Center + PolarVector(100f, MathHelper.TwoPi * InverseLerp(0f, group, index) + Spin);
            segments[^1].position = Vector2.Lerp(segments[^1].position, pos, .1f);
            Spin = (Spin + .01f) % MathHelper.TwoPi;
        }
    }

    public override bool? CanCutTiles() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Utils.CenteredRectangle(segments[^1].position, new(28f)).Intersects(targetHitbox);
    }

    public override bool? CanDamage() => Attack ? null : false;

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.HitDirectionOverride = (target.Center.X > segments[^1].position.X).ToDirectionInt();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Attack)
        {
            for (int i = 0; i < 25; i++)
                Dust.NewDustPerfect(segments[^1].position + Main.rand.NextVector2Circular(8, 8),
                    DustID.Water, target.Center.SafeDirectionTo(segments[^1].position).RotatedByRandom(.4f) * Main.rand.NextFloat(4f, 8f),
                    0, default, Main.rand.NextFloat(.5f, 1.2f));
            AttackWait = WaitTime;
            Attack = false;
            Projectile.netUpdate = true;
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (segments == null)
            return false;

        Texture2D tip = Projectile.ThisProjectileTexture();
        Texture2D inner = AssetRegistry.GetTexture(AdditionsTexture.KrakenTentacleSegment);

        Vector2[] bezierPoints = segments.Select(x => x.position).ToArray();
        BezierCurves bezierCurve = new(bezierPoints);
        Vector2 val = segments[^1].position - segments[^1].oldPosition;
        Vector2 scale = Vector2.One;

        int totalChains = (int)(Vector2.Distance(segments[0].position, segments[^1].position) / inner.Height / scale.Length()) / 2;
        totalChains = (int)MathHelper.Clamp(totalChains, 10f, 100f);
        for (int i = 0; i < totalChains - 1; i++)
        {
            Vector2 drawPosition = bezierCurve.Evaluate(i / (float)totalChains);
            float completionRatio = i / (float)totalChains + 1f / totalChains;
            float angle = (bezierCurve.Evaluate(completionRatio) - drawPosition).ToRotation();
            if (i == totalChains - 2)
                inner = tip;

            Main.spriteBatch.DrawBetter(inner, drawPosition,
                null, Color.White, angle, inner.Size() * 0.5f, scale, SpriteEffects.None);
        }

        return false;
    }
}