using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Middle;

public class GreekNapalm : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.penetrate = 5;
        Projectile.timeLeft = 300;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }
    public bool HitNPC
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }
    public bool HitGround
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public ref float Timer => ref Projectile.ai[2];
    public ref float NPCID => ref Projectile.AdditionsInfo().ExtraAI[0];
    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(Offset);
    public override void ReceiveExtraAI(BinaryReader reader) => Offset = reader.ReadVector2();
    public float completion => InverseLerp(0f, 300f, Projectile.timeLeft);
    public override void AI()
    {
        if (Timer == 0)
            NPCID = -1;

        float speed = Main.rand.NextFloat(1f, 12f) * completion;
        Vector2 vel = HitGround ? -Vector2.UnitY.RotatedByRandom(.45f) * speed : -Projectile.velocity.RotatedByRandom(.4f).SafeNormalize(Vector2.Zero) * speed;
        Color col = Color.Lerp(Color.LawnGreen, Color.White, Main.rand.NextFloat(.1f, .2f));
        for (int i = 0; i < 2; i++)
            ParticleRegistry.SpawnHeavySmokeParticle(Projectile.Center, vel * .2f, Main.rand.Next(30, 40), Main.rand.NextFloat(.2f, .4f) * completion, col);
        if (Main.rand.NextBool(5) && completion > .3f)
            ParticleRegistry.SpawnSparkParticle(Projectile.Center, vel * 2, Main.rand.Next(72, 120), Main.rand.NextFloat(.3f, .5f) * completion, Color.Lime.Lerp(Color.White, .4f), true, true);
        if (Main.rand.NextBool(3))
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel.RotatedByRandom(.2f) * 1.2f, (int)(Main.rand.Next(40, 50) * completion), Main.rand.NextFloat(.2f, .6f) * completion, col, 1f, true);

        if (NPCID < 0)
        {
            if (HitGround)
                Projectile.velocity = Vector2.Zero;
            else if (Timer > 20f)
                Projectile.velocity.Y += .2f;
        }
        else
        {
            NPC target = Main.npc[(int)NPCID];

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.timeLeft = 120;
                Projectile.position = target.position + Offset;
            }
        }
        Timer++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!HitGround)
            HitGround = true;
        return false;
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.FinalDamage *= completion;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.tileCollide = false;
        NPCID = target.whoAmI;
        Offset = Projectile.position - target.position;
        Offset -= Projectile.velocity;

        target.AddBuff(BuffID.CursedInferno, 120);
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info)
    {
        target.AddBuff(BuffID.CursedInferno, 120);
    }
}
