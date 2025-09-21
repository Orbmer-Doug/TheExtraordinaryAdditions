using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets;
using TheExtraordinaryAdditions.Common.Particles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Early;

public class ForkStab : ModProjectile
{
    public struct AfterimageData(Vector2 pos, float rot, int time)
    {
        public Vector2 pos = pos;
        public float rot = rot;
        public int time = time;
    }

    public ref float MaxTimeLeft => ref Projectile.ai[0];

    public ref float AmountStabs => ref Projectile.ai[1];

    public bool Fading
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public bool PlayedSound
    {
        get => Projectile.Additions().ExtraAI[0] == 1f;
        set => Projectile.Additions().ExtraAI[0] = value.ToInt();
    }

    public Vector2 offset;

    public Vector2 stabVec;

    public List<NPC> hitNPCs = [];

    public List<AfterimageData> afterImages = [];

    public Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;

    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Fork);

    public Player Owner => Main.player[Projectile.owner];

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(offset);
        writer.WriteVector2(stabVec);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        offset = reader.ReadVector2();
        stabVec = reader.ReadVector2();
    }
    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.width = Projectile.height = 64;
        Projectile.penetrate = -1;
        Projectile.ownerHitCheck = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
    }

    public override void AI()
    {
        if (Projectile.localAI[0] == 0f)
        {
            Projectile.timeLeft = (int)(Owner.HeldItem.useAnimation * (1f / Owner.GetTotalAttackSpeed(Projectile.DamageType)));
            MaxTimeLeft = Projectile.timeLeft;
            Projectile.localAI[0] = 1f;
        }

        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = 2;

        Projectile.velocity = Vector2.Normalize(Projectile.velocity);
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

        int stabTimer = (int)MaxTimeLeft - Projectile.timeLeft;

        if (!Fading)
        {
            float totalTime = MaxTimeLeft * 0.5f;

            if (stabTimer < totalTime)
            {
                if (stabTimer == 1)
                {
                    if (this.RunLocal())
                    {
                        Projectile.velocity = Owner.SafeDirectionTo(Owner.Additions().mouseWorld).RotatedByRandom(0.25f);
                        this.Sync();
                    }
                    stabVec = new Vector2(Main.rand.NextFloat(90, 150), 0);
                    hitNPCs.Clear();
                }

                const int dist = 20;
                if (stabTimer < totalTime * 0.5f)
                {
                    float lerper = stabTimer / (totalTime * 0.5f);
                    offset = Vector2.Lerp(new Vector2(dist, 0), stabVec, Circ.OutFunction(lerper)).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
                }
                else
                {
                    if (!PlayedSound)
                    {
                        afterImages.Add(new AfterimageData(Projectile.Center, Projectile.rotation, 20));
                        SoundEngine.PlaySound(SoundID.DD2_GhastlyGlaivePierce with { Pitch = 1f, PitchVariance = .2f }, Owner.Center, null);
                        PlayedSound = true;
                        this.Sync();
                    }

                    float lerper = (stabTimer - totalTime * 0.5f) / (float)(totalTime * 0.5f);
                    offset = Vector2.Lerp(stabVec, new Vector2(dist, 0), Circ.OutFunction(lerper)).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
                }
            }
            else
            {
                PlayedSound = false;
                AmountStabs++;
                Projectile.timeLeft = (int)MaxTimeLeft;

                stretch = (Player.CompositeArmStretchAmount)Main.rand.Next(4);
                this.Sync();
            }

            if (!Owner.channel && AmountStabs > 5 && Projectile.timeLeft == MaxTimeLeft)
            {
                Projectile.timeLeft = 5;
                Fading = true;
            }
        }

        for (int i = 0; i < afterImages.Count; i++)
        {
            AfterimageData afterImage = afterImages[i];
            afterImages[i] = new AfterimageData(afterImage.pos, afterImage.rot, afterImage.time - 1);

            if (afterImages[i].time <= 0)
                afterImages.RemoveAt(i);
        }

        Vector2 off = new Vector2(0f, -45f).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.MountedCenter + offset + off;

        Owner.SetCompositeArmFront(true, stretch, Projectile.rotation - (MathHelper.PiOver4 + MathHelper.PiOver2));
        Owner.ChangeDir(Projectile.direction);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitNPCs.Add(target);
        ScreenShakeSystem.New(new(.1f, .2f), Projectile.Center);

        Vector2 pos = Owner.Center - (Owner.Center - target.Center);

        if (target.IsFleshy())
        {
            for (int k = 0; k < 7; k++)
            {
                Vector2 vel = Projectile.Center.DirectionTo(Owner.Center).RotatedByRandom(0.35f) * Main.rand.NextFloat(2f, 8f);
                Dust.NewDustPerfect(pos, DustID.Blood, vel, 0, default, Main.rand.NextFloat(1f, 1.4f));
            }
        }
        else
        {
            for (int k = 0; k < 10; k++)
            {
                Vector2 vel = Projectile.Center.DirectionTo(Owner.Center).RotatedByRandom(0.35f) * Main.rand.NextFloat(5f, 12f);
                ParticleRegistry.SpawnSparkParticle(pos, vel, Main.rand.Next(12, 18), Main.rand.NextFloat(.35f, .5f), Color.Chocolate);
            }
        }

        AdditionsSound.SwordSliceShort.Play(Owner.Center, .4f, -.1f, .1f, 10);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        bool original = projHitbox.Intersects(targetHitbox);
        bool line = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Owner.Center, Projectile.Center);
        return original || line;
    }

    public override bool? CanHitNPC(NPC target)
    {
        return Projectile.friendly && !hitNPCs.Contains(target) && !target.friendly;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        SpriteBatch sb = Main.spriteBatch;
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 pos = Projectile.Center - Main.screenPosition;
        float scale = Projectile.scale;
        float rot = Projectile.rotation;

        Vector2 off = new(0, Main.player[Projectile.owner].gfxOffY);

        float fade = 1f;
        if (Projectile.timeLeft <= 5 && Fading)
            fade = Projectile.timeLeft / 5f;

        for (int i = 0; i < afterImages.Count; i++)
        {
            AfterimageData afterImage = afterImages[i];

            float opacity = MathHelper.Lerp(0.5f, 0f, 1f - afterImage.time / 15f);
            Main.spriteBatch.Draw(tex, afterImage.pos - Main.screenPosition + off, null, Color.Red * 0.5f * opacity, afterImage.rot, Vector2.Zero, scale, 0f, 0f);
        }

        sb.Draw(tex, pos + off, null, Color.White * fade, rot, Vector2.Zero, scale, 0, 0);
        return false;
    }
}