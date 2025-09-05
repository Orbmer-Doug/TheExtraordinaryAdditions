using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Vanilla.Early;

public class CrimtaneArrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.CrimtaneArrow);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.penetrate = 2;
        Projectile.timeLeft = Projectile.ArrowLifeTime;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.arrow = true;
        Projectile.alpha = 255;
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

    public bool HitEnemy
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }
    public ref float TrailOpacity => ref Projectile.Additions().ExtraAI[0];

    public override void AI()
    {
        if (Time == 0)
            TrailOpacity = 1f;

        Projectile.FacingDown();

        Projectile.alpha -= 25;
        if (Time >= 5f && !HitEnemy)
            Projectile.velocity.Y += 0.15f;

        if (HitEnemy)
        {
            NPC target = Main.npc[NPCID];
            TrailOpacity *= .9f;

            if (!target.active)
            {
                if (Projectile.timeLeft > 5)
                    Projectile.timeLeft = 5;

                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                Projectile.position = target.position + Offset;
                if (Projectile.position != Projectile.oldPosition)
                    this.Sync();
            }
        }

        if (TrailOpacity > .01f)
        {
            if (trail == null || trail._disposed)
                trail = new(c => Projectile.width, (c, pos) => Color.Crimson * MathHelper.SmoothStep(1f, 0f, c.X) * TrailOpacity, null, 5);
            points ??= new(5);
            points.Update(Projectile.Center + Projectile.velocity - PolarVector(12f, Projectile.velocity.ToRotation()));
        }

        Lighting.AddLight(Projectile.Center, Color.Red.ToVector3() * .35f);

        Time++;
    }

    public bool Intersecting()
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
        {
            if (projectile != null && projectile.type == Type && Projectile.RotHitbox().Intersects(projectile.RotHitbox()) && projectile.identity != Projectile.identity)
            {
                Vector2 pos = Projectile.RotHitbox().Bottom;
                for (int i = 0; i < 10; i++)
                {
                    ParticleRegistry.SpawnBloomPixelParticle(pos, Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(30, 40), Main.rand.NextFloat(.3f, .4f), Color.DarkRed, Color.Crimson, null, 1.4f);
                    ParticleRegistry.SpawnBloomLineParticle(pos, Main.rand.NextVector2Circular(3f, 3f), Main.rand.Next(20, 30), Main.rand.NextFloat(.2f, .4f), Color.Crimson);
                }
                Projectile.Kill();
                projectile.Kill();
                return true;
            }
        }
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 15; i++)
            Dust.NewDustPerfect(Projectile.RotHitbox().RandomPoint(), DustID.Blood, Main.rand.NextVector2Circular(4f, 4f), 90, default, Main.rand.NextFloat(.8f, 1.2f));
    }

    public override bool? CanHitNPC(NPC target) => HitEnemy ? false : null;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.tileCollide = false;

        NPCID = target.whoAmI;
        Offset = Projectile.position - target.position;
        Offset -= Projectile.velocity;

        HitEnemy = true;
        this.Sync();
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Intersecting())
        {
            modifiers.FinalDamage *= 1.4f;
        }
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points;
    public override bool PreDraw(ref Color lightColor)
    {
        void prim()
        {
            if (points == null)
                return;
            if (trail != null && !trail._disposed && TrailOpacity > .01f)
                trail.DrawTrail(ShaderRegistry.StandardPrimitiveShader, points.Points, 50, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(prim, PixelationLayer.UnderProjectiles);

        Projectile.DrawBaseProjectile(lightColor);
        return false;
    }
}