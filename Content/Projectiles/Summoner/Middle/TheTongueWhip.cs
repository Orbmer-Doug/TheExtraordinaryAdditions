using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Summoner.Middle;

/// <summary>
/// This was not my idea...
/// Was more cursed in execution than in thought
/// </summary>
public class TheTongueWhip : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.TheTongueWhip);
    public const int SegmentCount = 20;

    public const int FadeoutTime = 20;

    public List<VerletSimulatedSegment> Segments;

    public Player Owner => Main.player[Projectile.owner];

    public ref float Initialized => ref Projectile.ai[0];

    public ref float Timer => ref Projectile.ai[1];

    public override void SetDefaults()
    {
        Projectile.aiStyle = -1;
        Projectile.width = 28;
        Projectile.height = 28;
        Projectile.scale = 1.15f;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.penetrate = -1;
        Projectile.MaxUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        List<Vector2> points = [];
        foreach (VerletSimulatedSegment segment in Segments)
            points.Add(segment.position);
        return targetHitbox.CollisionFromPoints(points, Projectile.height);
    }

    public override bool? CanCutTiles() => false;
    public Vector2 MouseWorld => Owner.Additions().mouseWorld;
    public void Initialize()
    {
        Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        Segments = new List<VerletSimulatedSegment>(SegmentCount);
        for (int i = 0; i < SegmentCount; i++)
        {
            VerletSimulatedSegment segment = new(Projectile.Center + Vector2.UnitY * 20f * i);
            Segments.Add(segment);
        }
        Segments[0].locked = true;
        Initialized = 1f;
        this.Sync();
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (Initialized == 0f)
        {
            Initialize();
        }
        if (Owner.channel)
        {
            Projectile.timeLeft = FadeoutTime;
        }

        // Hold hands out
        float dest = Owner.AngleTo(Segments.Last().position) - MathHelper.PiOver2;
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, dest);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, dest);

        // Face the projectile
        int dir = Owner.direction = (!(Segments.Last().position.X < Owner.Center.X)) ? 1 : (-1);
        Owner.ChangeDir(dir);

        Projectile.Center = Projectile.Center.MoveTowards(Owner.RotatedRelativePoint(Owner.MountedCenter, false, true), 10f);
        SimulateSegments();

        if (Timer % 3f == 0f)
        {
            Vector2 pos = Segments[Main.rand.Next(0, SegmentCount)].position;
            Dust.NewDust(pos, Projectile.width, Projectile.height, DustID.Water);
        }

        Vector2 val = Segments.Last().position - Owner.Center;
        if (val.Length() > 380f * Owner.whipRangeMultiplier)
        {
            Vector2 maxDist = Owner.Center + Utils.SafeNormalize(val, Vector2.One) * 380f * Owner.whipRangeMultiplier;
            Segments.Last().position = maxDist;
        }
        else
        {
            if (this.RunLocal())
            {
                Segments.Last().position = Vector2.Lerp(Segments.Last().position, MouseWorld, .1f);
                this.Sync();
            }
        }

        Vector2 val2 = Segments.Last().position - Segments.Last().oldPosition;
        float centrifugalForce = Math.Clamp(((Vector2)val2).Length() * 2f, 0f, 130f) / 130f;
        Projectile.damage = (int)(Owner.HeldItem.damage * centrifugalForce * 2f);

        if (val.Length() > 3200f)
        {
            Projectile.Kill();
        }
        Timer++;
    }

    public void SimulateSegments()
    {
        if (Segments == null)
        {
            Segments = new List<VerletSimulatedSegment>(SegmentCount);
            for (int i = 0; i < SegmentCount; i++)
            {
                Segments[i] = new VerletSimulatedSegment(Projectile.Center);
            }
        }
        Segments[0].oldPosition = Segments[0].position;
        Segments[0].position = Projectile.Center;
        Segments = VerletSimulatedSegment.SimpleSimulation(Segments, 10f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = ClosestOutOfList(target.Center, out int index, Segments.Select(x => x.position).ToArray());
        Vector2 val = Segments[index].position - Segments[index].oldPosition;

        float centrifugalForce = Math.Clamp(val.Length() * 2f, 0f, 130f) / 130f;
        if (centrifugalForce > 0.2f)
        {
            float power = InverseLerp(0f, SegmentCount, index);
            Vector2 vel = (Segments[index].position - Segments[index].oldPosition).ClampLength(0f, 8f) * power;

            if (index == Segments.Count - 1)
            {
                if (Main.rand.NextBool(2))
                {
                    int p = Projectile.NewProj(pos, vel, ModContent.ProjectileType<IchorGlobule>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
                    Main.projectile[p].DamageType = DamageClass.Summon;
                    Main.projectile[p].tileCollide = false;
                    for (int i = 0; i < 20; i++)
                    {
                        vel *= Main.rand.NextFloat(.6f, 1f);
                        Dust.NewDust(pos, 10, 10, DustID.Ichor, vel.X, vel.Y, 0, default, Main.rand.NextFloat(.8f, 1.2f));
                    }

                    for (int i = 0; i < 30; i++)
                    {
                        vel *= Main.rand.NextFloat(.6f, 1f);
                        Dust.NewDust(pos, 10, 10, DustID.Water, vel.X, vel.Y, 0, default, Main.rand.NextFloat(.8f, 1.2f));
                    }
                }
                Owner.MinionAttackTargetNPC = target.whoAmI;
            }

            for (int i = 0; i < 8; i++)
            {
                ParticleRegistry.SpawnSquishyPixelParticle(pos, vel.RotatedByRandom(.3f) * Main.rand.NextFloat(.7f, 1.4f),
                    Main.rand.Next(70, 90), Main.rand.NextFloat(.8f, 1.5f), Color.Gold, Color.Yellow, 3, true, true);
            }

            SoundID.NPCHit13.Play(pos, 1f * centrifugalForce + .8f, 0f, .2f);
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Vector2 pos = ClosestOutOfList(target.Center, out int index, Segments.Select(x => x.position).ToArray());
        Vector2 val = Segments[index].position - Segments[index].oldPosition;
        float centrifugalForce = Math.Clamp(val.Length() * 2f, 0f, 130f) / 130f;
        float power = InverseLerp(0f, SegmentCount, index);
        modifiers.FinalDamage *= power * (centrifugalForce + .5f);
    }

    public void DrawChain()
    {
        Texture2D innerTongueTex = AssetRegistry.GetTexture(AdditionsTexture.TongueSegment);
        Texture2D tongueTex = Projectile.ThisProjectileTexture();

        // Collect chain draw positions.
        Vector2[] bezierPoints = Segments.Select(x => x.position).ToArray();
        BezierCurves bezierCurve = new(bezierPoints);

        // Calculate squish vectors based on how fast the tongue is moving and how far it is from the player
        Vector2 val = Segments.Last().position - Segments.Last().oldPosition;
        float stretchFactor = MathHelper.Clamp(Segments.Last().position.Distance(Owner.Center) / MouseWorld.Distance(Owner.Center), .5f, 1f);
        float centrifugalForce = Math.Clamp(val.Length() * 2f - 10f, 0f, 130f) / 150f;
        Vector2 centrifugalSquish = new(1f + centrifugalForce * 0.66f, 1f - centrifugalForce);
        centrifugalSquish.ClampLength(.4f, 1f);
        Vector2 scale = new(1f, 1f * stretchFactor);
        scale *= centrifugalSquish;

        int totalChains = (int)(Vector2.Distance(Segments.First().position, Segments.Last().position) / innerTongueTex.Height / scale.Length()) / 2;
        totalChains = (int)MathHelper.Clamp(totalChains, 30f, 1200f);
        for (int i = 0; i < totalChains - 1; i++)
        {
            Vector2 drawPosition = bezierCurve.Evaluate(i / (float)totalChains);
            float completionRatio = i / (float)totalChains + 1f / totalChains;
            float angle = (bezierCurve.Evaluate(completionRatio) - drawPosition).ToRotation();
            Color baseChainColor = Lighting.GetColor((int)drawPosition.X / 16, (int)drawPosition.Y / 16) * 2f;
            if (i == totalChains - 2)
                innerTongueTex = tongueTex;

            Main.EntitySpriteDraw(innerTongueTex, drawPosition - Main.screenPosition, null, baseChainColor.MultiplyRGBA(Color.White) * (Projectile.timeLeft / (float)FadeoutTime), angle, innerTongueTex.Size() * 0.5f, scale, SpriteEffects.None, 0);
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        DrawChain();
        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        if (Segments != null)
            Utils.WriteVector2(writer, Segments.Last().position);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        if (Segments != null)
            Segments.Last().position = Utils.ReadVector2(reader);
    }
}
