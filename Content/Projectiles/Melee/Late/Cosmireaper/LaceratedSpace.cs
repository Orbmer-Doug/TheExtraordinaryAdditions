using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Common.Particles.Shader;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Late.Cosmireaper;

public class LaceratedSpace : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = 52;
        Projectile.height = 52;
        Projectile.friendly = true;
        Projectile.ignoreWater = false;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = SecondsToFrames(4);
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Projectile.damage = (int)(Projectile.damage * 0.945);
    }

    public bool MakingPoints
    {
        get => Projectile.ai[0] == 1f;
        set => Projectile.ai[0] = value.ToInt();
    }

    public Vector2 Start { get; set; }
    public Vector2 End { get; set; }
    public List<Vector2> Points = [];
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Start);
        writer.WriteVector2(End);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Start = reader.ReadVector2();
        End = reader.ReadVector2();
    }
    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * 2f);
        float interpolant = Utils.GetLerpValue(0f, 30f, Projectile.timeLeft, true) * 3f;

        if (MakingPoints)
        {
            Points = Start.GetLaserControlPoints(End, 50);
        }

        if (Points != null && Points.Count > 0)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                float completion = Convert01To010(InverseLerp(0, Points.Count, i));

                Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedByRandom(.367f * completion) * Main.rand.NextFloat(15.5f, 20f) * completion;
                ShaderParticleRegistry.SpawnCosmicParticle(Points[i], vel, interpolant * 20f * completion);
            }
        }
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(Points, Projectile.width);
    }
}