using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class KnifeStab : ModProjectile
{
    private ref float Timer => ref Projectile.ai[0];
    public ref float AmountStabs => ref Projectile.ai[1];
    public ref float MaxTimeLeft => ref Projectile.ai[2];
    public bool Fading
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }
    public bool PlayedSound
    {
        get => Projectile.AdditionsInfo().ExtraAI[1] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.ComicallyLargeKnife);
    private Player Owner => Main.player[Projectile.owner];
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 62;
        Projectile.height = 60;
        Projectile.scale = 2f;

        Projectile.friendly = true;
        Projectile.timeLeft = 10000;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 1;
        Projectile.ownerHitCheck = true;
        Projectile.DamageType = DamageClass.Melee;
    }
    public Vector2 offset;
    public Vector2 stabVec;
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

    public Player.CompositeArmStretchAmount stretch = Player.CompositeArmStretchAmount.Full;
    public List<NPC> hitNPCs = [];
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
        if (this.RunLocal())
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

                    float circ = Circ.OutFunction(lerper);
                    offset = Vector2.Lerp(new Vector2(dist, 0), stabVec, circ).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
                }
                else
                {
                    if (!PlayedSound)
                    {
                        AdditionsSound.SwordSliceShort.Play(Owner.Center, .7f, .3f, .2f);
                        PlayedSound = true;
                    }

                    float lerper = (stabTimer - totalTime * 0.5f) / (float)(totalTime * 0.5f);
                    float circ = Circ.OutFunction(lerper);
                    offset = Vector2.Lerp(stabVec, new Vector2(dist, 0), circ).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
                }
            }
            else
            {
                PlayedSound = false;
                AmountStabs++;
                Projectile.timeLeft = (int)MaxTimeLeft;

                stretch = (Player.CompositeArmStretchAmount)Main.rand.Next(4);
            }

            if (!Owner.channel && AmountStabs > 5 && Projectile.timeLeft == MaxTimeLeft)
            {
                Projectile.timeLeft = 5;
                Fading = true;
            }
        }

        Vector2 off = new Vector2(0f, -45f).RotatedBy(Projectile.rotation - MathHelper.PiOver4);
        Projectile.Center = Owner.MountedCenter + offset;

        Owner.SetCompositeArmFront(true, stretch, Projectile.rotation - (MathHelper.PiOver4 + MathHelper.PiOver2));
        Owner.ChangeDir(Projectile.direction);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f * Projectile.scale, 14f * Projectile.scale);
    }

    public override void CutTiles()
    {
        Vector2 start = Projectile.Center;
        Vector2 end = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * Projectile.width * .5f * Projectile.scale;
        Utils.PlotTileLine(start, end, 14f * Projectile.scale, DelegateMethods.CutTiles);
    }

    public readonly int[] ObliteratableTypes = [NPCID.CaveBat, NPCID.GiantBat, NPCID.IceBat, NPCID.IlluminantBat,
        NPCID.JungleBat, NPCID.SporeBat, NPCID.VampireBat, NPCID.Hellbat, NPCID.Lavabat, NPCID.Medusa, NPCID.FlyingSnake, NPCID.Lihzahrd, NPCID.LihzahrdCrawler];
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // Make knockback go away from player
        modifiers.HitDirectionOverride = target.position.X > Owner.MountedCenter.X ? 1 : -1;

        foreach (int i in ObliteratableTypes)
        {
            if (target.type == i)
            {
                modifiers.Knockback += 20;
                modifiers.FinalDamage *= 100f;
            }
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (NPCID.Sets.ProjectileNPC[target.type])
            return;

        hitNPCs.Add(target);

        CheckLinearCollision(Owner.Center, target.Center, target.Hitbox, out Vector2 start, out Vector2 end);

        for (int i = 0; i < 14; i++)
        {
            Vector2 vel = target.Center.SafeDirectionTo(Owner.Center).RotatedByRandom(.2f) * Main.rand.NextFloat(1f, 12f);
            int life = Main.rand.Next(20, 40);
            float scale = Main.rand.NextFloat(.4f, .8f);

            if (target.IsFleshy())
            {
                if (i == 0)
                    ParticleRegistry.SpawnBloodStreakParticle(start, vel.SafeNormalize(Vector2.Zero), life * 2, scale * .7f, Color.DarkRed);
                ParticleRegistry.SpawnBloodParticle(start, vel, life, scale, Color.Crimson.Lerp(Color.Red, Main.rand.NextFloat(.25f, .78f)));
            }
            else
            {
                ParticleRegistry.SpawnSparkParticle(start, vel * Main.rand.NextFloat(1.2f, 2.1f), life, scale, Color.Chocolate, true, true);
            }
        }

        AdditionsSound.SwordSlice.Play(Owner.Center, .4f, -.1f, .1f, 10);
    }
    public override bool? CanHitNPC(NPC target) => hitNPCs.Contains(target) ? false : null;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 pos = Projectile.Center - Main.screenPosition;
        float scale = Projectile.scale;
        float rot = Projectile.rotation;

        Vector2 off = new(0, Main.player[Projectile.owner].gfxOffY * Projectile.scale);

        float fade = 1f;
        if (Projectile.timeLeft <= 5 && Fading)
            fade = Projectile.timeLeft / 5f;

        SpriteEffects fx = SpriteEffects.None;
        if (int.IsNegative(Projectile.velocity.X.NonZeroSign()))
        {
            fx = SpriteEffects.FlipHorizontally;
            rot += MathHelper.PiOver2;
        }

        Main.spriteBatch.Draw(tex, pos + off, null, Lighting.GetColor(Projectile.Center.ToTileCoordinates()) * fade, rot, tex.Size() / 2, scale, fx, 0);
        return false;
    }
}