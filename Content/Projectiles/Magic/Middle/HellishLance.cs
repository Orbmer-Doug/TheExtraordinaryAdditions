using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Middle;

public class HellishLance : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.HellishLance);

    public override void SetDefaults()
    {
        Projectile.width = 142;
        Projectile.height = 28;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.localNPCHitCooldown = -1;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }

    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(Offset);
    public override void ReceiveExtraAI(BinaryReader reader) => Offset = reader.ReadVector2();
    public ref float Time => ref Projectile.ai[0];
    public int NPCID
    {
        get => (int)Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }
    public ref float StickTime => ref Projectile.ai[2];
    public ref float AfterOpac => ref Projectile.AdditionsInfo().ExtraAI[0];

    public const int Wait = 20;

    public override bool? CanDamage()
    {
        return Time > Wait;
    }

    public override void AI()
    {
        Projectile.rotation = -MathHelper.PiOver2;

        if (Time == 0f)
        {
            NPCID = -1;
            AfterOpac = 1f;
        }
        else if (Time == Wait)
        {
            for (int i = 0; i < 20; i++)
            {
                Vector2 pos = Projectile.RotHitbox().Left + Vector2.UnitX * Main.rand.NextFloat(-Projectile.height / 2, Projectile.height / 2);
                Vector2 vel = Vector2.UnitY * -Main.rand.NextFloat(4f, 9f);
                int life = Main.rand.Next(30, 40);
                float scal = Main.rand.NextFloat(.4f, .8f);
                Color col = Color.Lerp(Color.Violet, Color.BlueViolet, Main.rand.NextFloat(.4f, .8f));
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, life, scal, col, Color.DarkViolet, null, 1f, 7);
            }
        }
        else if (Time > Wait)
        {
            after ??= new(8, () => Projectile.Center);
            after.UpdateFancyAfterimages(new(Projectile.Center + Projectile.velocity, Vector2.One, AfterOpac, Projectile.rotation, 0, 0, 6));
            Projectile.velocity = new Vector2(0f, -Animators.MakePoly(5f).OutFunction.Evaluate(Time, Wait, Wait + 12f, 0f, 36f));
        }

        if (Projectile.numHits > 0)
        {
            NPC target = Main.npc[NPCID];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                if (StickTime < 30 && StickTime % 10 == 9)
                    Projectile.ResetLocalNPCHitImmunity();

                AfterOpac *= .9f;
                Projectile.velocity *= InverseLerp(10f, 0f, StickTime);
                Projectile.position = target.position + Offset;
                Offset += Projectile.velocity * .5f;
                StickTime++;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
        }

        Projectile.Opacity = InverseLerp(0f, 20f, Projectile.timeLeft);
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Projectile.BaseRotHitbox().Intersects(targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (NPCID == -1)
        {
            if (Projectile.timeLeft > 100)
                Projectile.timeLeft = 100;

            Vector2 start = Projectile.BaseRotHitbox().Right;
            AdditionsSound.SwordSliceShort.Play(start, .5f, -.15f, .09f, 10);
            for (int i = 0; i < 20; i++)
            {
                ParticleRegistry.SpawnSparkParticle(start, Projectile.velocity.RotatedByRandom(.2f) * Main.rand.NextFloat(.3f, .5f), Main.rand.Next(20, 35), Main.rand.NextFloat(.4f, .8f), Color.DarkViolet);
                ParticleRegistry.SpawnBloomLineParticle(start + Main.rand.NextVector2Circular(8, 8), Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(.2f, .3f), Main.rand.Next(15, 25), Main.rand.NextFloat(.5f, .7f), Color.Violet);
            }

            NPCID = target.whoAmI;
            Offset = Projectile.position - target.position;
            Offset -= Projectile.velocity;
            target.velocity += Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.knockBack * target.knockBackResist;

            this.Sync();
        }
        else
            Projectile.damage = (int)(Projectile.damage * .675f);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.Knockback *= 0;
    }

    public FancyAfterimages after;
    public override bool PreDraw(ref Color lightColor)
    {
        void proj()
        {
            Vector2 scale = new(1f, Animators.MakePoly(3f).InOutFunction(InverseLerp(0f, Wait, Time)));

            Texture2D tex = Projectile.ThisProjectileTexture();
            Color col = Lighting.GetColor(Projectile.Center.ToTileCoordinates()) * Projectile.Opacity;
            Color color = Projectile.GetAlpha(Color.White * InverseLerp(Wait, 0f, Time)) with { A = 0 };

            after?.DrawFancyAfterimages(tex, [Color.Black, Color.DarkViolet, Color.Violet], Projectile.Opacity, 1f, 0f);
            Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, col, -MathHelper.PiOver2, tex.Size() / 2, scale);
            for (int i = 0; i < 10; i++)
                Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, color, -MathHelper.PiOver2, tex.Size() / 2, scale);
        }
        LayeredDrawSystem.QueueDrawAction(proj, PixelationLayer.OverProjectiles);

        return false;
    }
}