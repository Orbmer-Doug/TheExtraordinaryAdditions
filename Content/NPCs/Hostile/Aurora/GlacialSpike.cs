using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Hostile.Aurora;

public class GlacialSpike : ProjOwnedByNPC<AuroraGuard>
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.GlacialSpike);

    public const float Scale = 1.5f;
    public static readonly int SnowflakeCount = DifficultyBasedValue(7, 10, 12, 14, 16, 22);
    public override void SetDefaults()
    {
        Projectile.width = (int)(70 * Scale);
        Projectile.height = (int)(156 * Scale);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
    }

    public ref float Time => ref Projectile.ai[0];
    public float Interpolant => InverseLerp(0f, AuroraGuard.TimeToRise, Time);
    public Vector2 SavedPos;
    public Vector2 GroundPos;
    public override void SendAI(BinaryWriter writer)
    {
        writer.WriteVector2(SavedPos);
        writer.WriteVector2(GroundPos);
    }
    public override void ReceiveAI(BinaryReader reader)
    {
        SavedPos = reader.ReadVector2();
        GroundPos = reader.ReadVector2();
    }

    public override void SafeAI()
    {
        if (Time == 0f)
        {
            GroundPos = FindNearestSurface(Projectile.Center, true, 2000f, 50, true).Value;
            SavedPos = GroundPos - Vector2.UnitY * Projectile.height / 2;
            Projectile.Center = SavedPos + Vector2.UnitY * (Projectile.height - 10);

            Projectile.netUpdate = true;
        }
        else if (Time == AuroraGuard.TimeToRise)
        {
            AdditionsSound.ColdHitMassive.Play(Projectile.Center, .9f, .1f, .1f);
            for (int i = 0; i < 40; i++)
            {
                Vector2 pos = GroundPos + Vector2.UnitX * Main.rand.NextFloat(-Projectile.width / 2, Projectile.width / 2) - Vector2.UnitY * 8f;
                Vector2 vel = -pos.SafeDirectionTo(GroundPos) * Main.rand.NextFloat(6f, 11f);
                ParticleRegistry.SpawnDustParticle(pos, vel, Main.rand.Next(30, 60), Main.rand.NextFloat(.5f, 1.2f), Color.Cyan, .1f, true, true);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(30, 60), Main.rand.NextFloat(.5f, .9f), Color.DeepSkyBlue, Color.LightSkyBlue, null, 1.5f, 5, true);
            }

            if (this.RunServer())
            {
                for (int i = 0; i < SnowflakeCount; i++)
                {
                    Vector2 pos = GroundPos + Vector2.UnitX * Main.rand.NextFloat(-Projectile.width / 2, Projectile.width / 2) - Vector2.UnitY * 8f;
                    Vector2 vel = -Vector2.UnitY.RotatedBy(Convert01To101(InverseLerp(GroundPos.X - Projectile.width / 2, GroundPos.X + Projectile.width / 2, pos.X))
                        * Utils.Remap(pos.X, GroundPos.X - Projectile.width / 2, GroundPos.X + Projectile.width / 2, -.3f, .3f)) * Main.rand.NextFloat(16f, 21f);
                    SpawnProjectile(pos, vel, ModContent.ProjectileType<Razorflake>(), AuroraGuard.IcicleDamage, 0f);
                }
            }
        }
        else if (Time > AuroraGuard.TimeToRise)
        {
            float interpol = Animators.MakePoly(8f).OutFunction(InverseLerp(AuroraGuard.TimeToRise, AuroraGuard.TimeToRise + 20f, Time));
            Projectile.Center = Vector2.Lerp(Projectile.Center, SavedPos, interpol);
        }

        Projectile.Opacity = Animators.MakePoly(2.5f).InFunction(InverseLerp(AuroraGuard.TimeToRise + 80, AuroraGuard.TimeToRise + 65f, Time));
        if (Projectile.Opacity <= 0f)
            Projectile.Kill();

        Time++;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPlayer(Player target)
    {
        return Time.BetweenNum(AuroraGuard.TimeToRise, AuroraGuard.TimeToRise + 20);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            Texture2D tex = Projectile.ThisProjectileTexture();
            Color col = Color.White * Projectile.Opacity;
            Vector2 scale = new Vector2(Animators.MakePoly(3f).OutFunction(InverseLerp(0f, 20f, Time)), 1f) * Scale;
            Color color = Projectile.GetAlpha(Color.White * InverseLerp(35f, 0f, Time)) with { A = 0 };

            Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, col, 0f, tex.Size() / 2, scale);
            for (int i = 0; i < 10; i++)
                Main.spriteBatch.DrawBetter(tex, Projectile.Center, null, color, 0f, tex.Size() / 2, scale);
        }
        LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.OverPlayers, BlendState.AlphaBlend);

        return false;
    }
}
