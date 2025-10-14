using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late;

public class CosmicBlast : ModProjectile, IHasScreenShader
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(10);
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.timeLeft = Life;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = -1;
        Projectile.usesLocalNPCImmunity = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("ImplosionBlast");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public const float Radius = 200f;
    public void UpdateShader()
    {
        float anim = new Animators.PiecewiseCurve()
            .Add(0f, 2f, .8f, Animators.MakePoly(2f).OutFunction)
            .Add(2f, 0f, 1f, Animators.MakePoly(5f).OutFunction)
            .Evaluate(InverseLerp(0f, Life, Time));
        Shader.TrySetParameter("intensity", anim);
        Shader.TrySetParameter("screenPos", GetTransformedScreenCoords(Projectile.Center));
        Shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        Shader.TrySetParameter("radius", (Radius * Projectile.scale) / Main.screenWidth * Main.GameViewMatrix.Zoom.X);
        Shader.TrySetParameter("falloffSigma", .6f);
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Time = 0f;
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("ImplosionBlast", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }

    public bool IsEntityActive() => Projectile.active;

    public NPC Target;
    public Vector2 Offset;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Offset);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Offset = reader.ReadVector2();
    }

    public ref float Time => ref Projectile.ai[0];
    public static readonly int Life = SecondsToFrames(.9f);
    public override void AI()
    {
        if (!HasShader)
            InitializeShader();
        UpdateShader();

        if (Target == null || !Target.active)
            Projectile.velocity = Vector2.Zero;
        else
            Projectile.position = Target.position + Offset;
        if (Projectile.position != Projectile.oldPosition)
            this.Sync();

        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.CanHomeInto() && !npc.boss && npc.IsAnEnemy() && npc.realLife <= 0 && npc.Center.WithinRange(Projectile.Center, Radius * .8f) && npc.velocity.Length() > 1f && npc.knockBackResist != 0f && !npc.dontTakeDamage)
            {
                npc.velocity *= .4f;
            }
        }

        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        Vector2 pos = Projectile.Center;
        for (int i = 0; i < 90; i++)
        {
            Vector2 vel = Main.rand.NextVector2CircularLimited(60f, 60f, .4f, 1f);
            int life = Main.rand.Next(50, 90);
            float scale = Main.rand.NextFloat(.9f, 1.1f);
            Color col = Main.rand.NextBool() ? Color.Fuchsia : Color.Cyan;
            ParticleRegistry.SpawnBloomPixelParticle(pos, vel * .5f, life / 3, scale, col, Color.Gray, null, 1.3f, 9);
            col = Main.rand.NextBool() ? Color.Fuchsia : Color.Cyan;

            ParticleRegistry.SpawnMistParticle(pos, vel * .1f + Main.rand.NextVector2Circular(9f, 9f), scale, col, Color.DarkViolet, Main.rand.NextFloat(190f, 230f));
            ParticleRegistry.SpawnSparkleParticle(pos, vel * .4f + Main.rand.NextVector2Circular(3f, 3f), life + 10, scale, col, col == Color.Fuchsia ? Color.Cyan : Color.Fuchsia, 1.4f);
        }
        if (this.RunLocal())
            Projectile.NewProj(Projectile.Center, Projectile.velocity, ModContent.ProjectileType<CosmicBlastHidden>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
        ParticleRegistry.SpawnPulseRingParticle(pos, Vector2.Zero, 20, 0f, Vector2.One, 0f, 400f, Color.DarkViolet, true);
        AdditionsSound.CeaselessVoidDeath.Play(Projectile.Center, 1, .4f, 0f, 10, Name);
        ParticleRegistry.SpawnChromaticAberration(pos, 130, 1f, 500f);
        ReleaseShader();
    }
}

public class CosmicBlastHidden : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.Size = new(CosmicBlast.Radius);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.tileCollide = Projectile.hostile = false;
        Projectile.localNPCHitCooldown = 2;
        Projectile.timeLeft = 5;
        Projectile.penetrate = -1;
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.DefenseEffectiveness *= 0f;
        modifiers.ScalingArmorPenetration += 1f;
    }
}