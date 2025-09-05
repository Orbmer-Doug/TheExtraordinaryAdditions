using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class TremorSpike : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    private static readonly int Lifetime = 300;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Lifetime;

        Projectile.penetrate = -1;
        Projectile.stopsDealingDamageAfterPenetrateHits = true;
        Projectile.extraUpdates = 1;
        Projectile.DamageType = DamageClass.Generic;
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();

    public struct SegmentData(Vector2 position, float rotation, float opacity)
    {
        public Vector2 Position = position;
        public float Rotation = rotation;
        public float Opacity = opacity;
    }
    public List<SegmentData> Segments = [];

    public ref float Timer => ref Projectile.ai[0];
    public ref float Wait => ref Projectile.ai[1];

    private Vector2 MousePos;
    public override void SendExtraAI(BinaryWriter writer) =>
        writer.WriteVector2(MousePos);

    public override void ReceiveExtraAI(BinaryReader reader) =>
        MousePos = reader.ReadVector2();

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Wait = Main.rand.NextFloat(10f, 16f);
            MousePos = ModdedOwner.mouseWorld;
            Projectile.localAI[0] = 1f;
            Projectile.netUpdate = true;
        }

        if (!Projectile.WithinRange(MousePos, 16f) && Timer > Wait)
        {
            if (this.RunLocal())
            {
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Projectile.SafeDirectionTo(MousePos) * 15f, .2f);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
        }
        else if (Timer > Wait)
        {
            if (Projectile.localAI[1] == 0f)
            {
                ParticleRegistry.SpawnDetailedBlastParticle(Projectile.Center, Vector2.Zero, new Vector2(Main.rand.NextFloat(.67f, 1f), 1f) * 60f, Vector2.Zero, 20, Color.Gray);
                Projectile.CreateFriendlyExplosion(Projectile.Center, Vector2.Zero, Projectile.damage, Projectile.knockBack, 10, 6, Vector2.One * 60f);
                for (int i = 0; i < 20; i++)
                {
                    Vector2 vel = Main.rand.NextVector2CircularLimited(10f, 10f, .5f, 1f);
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.Stone, vel, 0, default, Main.rand.NextFloat(.4f, .6f));
                    ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel * Main.rand.NextFloat(.9f, 1.4f), Main.rand.Next(16, 30), Main.rand.NextFloat(.4f, .8f), Color.Gray);
                }

                AdditionsSound.AsterlinHit.Play(Projectile.Center, Main.rand.NextFloat(.8f, 1.2f), 0f, .14f);
                Projectile.netUpdate = true;
                Projectile.localAI[1] = 1f;
            }

            Projectile.velocity = Vector2.Zero;
        }

        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Timer++;

        if (Projectile.velocity != Vector2.Zero)
            Segments.Add(new SegmentData(Projectile.Center, Projectile.rotation, 1f));

        for (int i = 0; i < Segments.Count; i++)
        {
            SegmentData data = Segments[i];

            float shake = Utils.GetLerpValue(Lifetime * .8f, Lifetime, Timer, true);
            data.Position += Main.rand.NextVector2CircularEdge(shake, shake);

            Segments[i] = new SegmentData(data.Position, data.Rotation, GetLerpBump(0f, 9f, Lifetime, Lifetime - 9f, Timer));
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 vel = -Projectile.velocity.RotatedByRandom(.25f) * Main.rand.NextFloat(.5f, 1.1f);
            Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(16f, 16f), DustID.Stone, vel, 0, default, Main.rand.NextFloat(.4f, .6f));
        }
    }

    public override void OnKill(int timeLeft)
    {
        foreach (SegmentData pos in Segments)
        {
            Vector2 position = pos.Position + Main.rand.NextVector2Circular(16f, 16f);
            Dust.NewDustPerfect(position, DustID.Stone, Main.rand.NextVector2Circular(2f, 2f), Main.rand.Next(0, 100), default, Main.rand.NextFloat(.6f, 1f));
            ParticleRegistry.SpawnSmokeParticle(position, RandomVelocity(2f, 1f, 6f), Main.rand.NextFloat(.3f, .6f), Color.LightGray, Color.DarkGray, Main.rand.NextByte(100, 140));
        }
        Segments.Clear();
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        foreach (SegmentData seg in Segments)
        {
            if (new Rectangle((int)seg.Position.X, (int)seg.Position.Y, 16, 16).Intersects(targetHitbox))
                return true;
        }
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D middle = AssetRegistry.GetTexture(AdditionsTexture.TremorSpikeMiddle);
        Texture2D end = AssetRegistry.GetTexture(AdditionsTexture.TremorSpikeEnd);
        for (int i = 0; i < Segments.Count; i++)
        {
            Texture2D texToUse = middle;
            if (i >= Segments.Count - 1)
                texToUse = end;

            SegmentData data = Segments[i];

            Color color = Color.White * data.Opacity;
            Vector2 orig = texToUse.Size() * .5f;
            Main.spriteBatch.DrawBetter(texToUse, data.Position, null, color, data.Rotation, orig, data.Opacity, 0);
        }

        return false;
    }
}