using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Early;

public class HellishNapalm : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 20;
        Projectile.friendly = true;
        Projectile.penetrate = 5;
        Projectile.timeLeft = 300;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 60;
    }

    private bool HitTarget
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    private bool HitGround
    {
        get => Projectile.AdditionsInfo().ExtraAI[0] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[0] = value.ToInt();
    }

    private ref float Timer => ref Projectile.ai[1];
    public ref float EnemyID => ref Projectile.ai[2];

    private Vector2 offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(offset);
    public override void ReceiveExtraAI(BinaryReader reader) => offset = reader.ReadVector2();

    public override void AI()
    {
        bool flag = Math.Abs(Projectile.velocity.X) < 0.01f;
        Vector2 vel = Projectile.velocity.RotatedByRandom(.05f) * .5f;
        if (!HitTarget)
        {
            if (flag)
                vel = Vector2.UnitY.RotatedByRandom(.35f) * -Main.rand.NextFloat(2.5f, 4f);
        }
        else if (HitTarget)
        {
            vel = Vector2.UnitY.RotatedByRandom(.2f) * -Main.rand.NextFloat(1.3f, 3f);
        }

        Vector2 pos = Projectile.RotHitbox().Bottom;
        float scale = Projectile.height * 1.1f * InverseLerp(0f, 60f, Projectile.timeLeft);
        ParticleRegistry.SpawnGlowParticle(pos, vel, 25, scale, Color.OrangeRed * .9f);
        ParticleRegistry.SpawnGlowParticle(pos, vel, 35, scale, Color.Chocolate * .7f);
        if (Main.rand.NextBool(7))
            ParticleRegistry.SpawnSquishyPixelParticle(pos, vel * Main.rand.NextFloat(1.2f, 2.3f), Main.rand.Next(80, 90), scale / Projectile.height, Color.OrangeRed, Color.Chocolate, 6, true, true);
        if (Main.rand.NextBool(7))
            ParticleRegistry.SpawnMistParticle(pos, vel * Main.rand.NextFloat(.7f, 1.2f), scale * .7f / Projectile.height, Color.LightGray, Color.DarkGray, Main.rand.NextFloat(80f, 120f), Main.rand.NextFloat(-.14f, .14f));

        if (HitTarget)
        {
            NPC target = Main.npc[(int)EnemyID];

            if (target == null || target.active == false)
                return;

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.timeLeft = 120;
                Projectile.position = target.position + offset;
            }
        }

        if (Timer > 10f)
        {
            if (Projectile.velocity.Y < 16f && !HitTarget)
                Projectile.velocity.Y += .2f;
        }

        Timer++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!HitTarget)
        {
            Projectile.tileCollide = false;
            EnemyID = target.whoAmI;
            offset = Projectile.position - target.position;
            offset -= Projectile.velocity;

            HitTarget = true;
        }

        target.AddBuff(BuffID.OnFire, SecondsToFrames(2), false);
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.velocity = Vector2.Zero;
        if (!HitGround)
        {
            HitGround = true;
        }
        return false;
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(BuffID.OnFire, SecondsToFrames(2), false);
    }
}
