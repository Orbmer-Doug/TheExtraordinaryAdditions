using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class VolatilePlasmaGlobule : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.penetrate = 5;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 22;

        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = true;
        Projectile.extraUpdates = 1;
        Projectile.timeLeft = 400;
    }

    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer) => writer.WriteVector2(Offset);
    public override void ReceiveExtraAI(BinaryReader reader) => Offset = reader.ReadVector2();
    
    public enum CurrentState
    {
        Free,
        HitEnemy,
        HitGround
    }

    public CurrentState State
    {
        get => (CurrentState)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = (float)value;
    }

    public override void AI()
    {
        Color randomColor = Main.rand.Next(4) switch
        {
            0 => Color.Yellow * 1.6f,
            1 => Color.YellowGreen,
            2 => Color.LimeGreen,
            _ => Color.Yellow * 1.8f,
        };

        ref float offset = ref Projectile.ai[2];
        offset += .09f * (Projectile.identity % 2f == 1f).ToDirectionInt() % MathHelper.TwoPi;
        int arms = 3;
        for (int i = 0; i < arms; i++)
        {
            Vector2 vel = (MathHelper.TwoPi * i / arms + offset).ToRotationVector2().RotatedBy(4) * Main.rand.NextFloat(2.1f, 4.1f);
            ParticleRegistry.SpawnGlowParticle(Projectile.Center, vel, 20, 40f, randomColor, 1f, true);
        }
        ParticleRegistry.SpawnBloomPixelParticle(Projectile.Center, Main.rand.NextVector2CircularEdge(4f, 4f) * Main.rand.NextFloat(.6f, 1.8f), Main.rand.Next(24, 34), Main.rand.NextFloat(.3f, .9f), randomColor, randomColor * 2);

        switch (State)
        {
            case CurrentState.Free:
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .3f, -20f, 30f);
                break;
            case CurrentState.HitEnemy:
                NPC target = Main.npc[(int)NPCID];

                if (!target.active)
                {
                    if (Projectile.timeLeft > 5)
                        Projectile.timeLeft = 5;

                    Projectile.velocity = Vector2.Zero;
                }
                else
                    Projectile.position = target.position + Offset;

                break;
            case CurrentState.HitGround:
                Projectile.velocity = Vector2.Zero;
                break;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        State = CurrentState.HitGround;
        return false;
    }

    public ref float NPCID => ref Projectile.AdditionsInfo().ExtraAI[2];
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.tileCollide = false;

        NPCID = target.whoAmI;
        Offset = Projectile.position - target.position;
        Offset -= Projectile.velocity;

        State = CurrentState.HitEnemy;
        Projectile.netUpdate = true;
    }

    public override void OnKill(int timeLeft)
    {
        ParticleRegistry.SpawnSparkleParticle(Projectile.Center, Vector2.Zero, 18, Main.rand.NextFloat(1f, 1.5f), Color.Yellow * 1.8f, Color.AntiqueWhite, 2f, .1f);
    }
}
