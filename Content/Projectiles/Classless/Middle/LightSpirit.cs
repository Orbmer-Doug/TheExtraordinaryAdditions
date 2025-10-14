using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Equipable.Accessories.Middle;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Middle;

public class LightSpirit : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionSacrificable[Projectile.type] = ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 59;
        Projectile.height = 50;
        Projectile.netImportant = Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = Projectile.minion = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.minionSlots = 0f;
        Projectile.timeLeft = 18000;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = Owner.GetBestClass();
    }

    public Player Owner => Main.player[Projectile.owner];

    public ref float OffsetAngle => ref Projectile.ai[0];

    public ref float Time => ref Projectile.ai[1];

    public override void AI()
    {
        Projectile.Opacity = InverseLerp(0f, 30f, Time);
        Lighting.AddLight(Projectile.Center, Color.Goldenrod.ToVector3() * 1.4f * Projectile.Opacity);

        if (!Owner.GetModPlayer<BandOfSunraysPlayer>().Equipped)
        {
            Projectile.Kill();
            return;
        }

        NPC potentialTarget = NPCTargeting.MinionHoming(new(Projectile.Center, 1050), Owner);
        if (Time % 50f == 49f && this.RunLocal() && potentialTarget != null)
        {
            Vector2 vel = Projectile.SafeDirectionTo(potentialTarget.Center) * 10f;
            Projectile p = Main.projectile[Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<LightSpiritStar>(), Projectile.damage, Projectile.knockBack, Projectile.owner, 0f, 0f, 0f)];
            p.originalDamage = Projectile.originalDamage;

            for (int i = 0; i < 12; i++)
            {
                ParticleRegistry.SpawnGlowParticle(Projectile.RandAreaInEntity(), vel.RotatedByRandom(.25f) * Main.rand.NextFloat(.6f, 1f),
                    Main.rand.Next(20, 30), Main.rand.NextFloat(20f, 50f), Color.Gold);
            }
        }

        Projectile.Center = Owner.Center + OffsetAngle.ToRotationVector2() * (150f * (Sin01(Time * .045f) + .35f));
        Projectile.rotation += MathHelper.ToRadians(2f);
        OffsetAngle += MathHelper.ToRadians(3f);

        Time++;
    }

    public override bool? CanDamage() => false;
    public override bool PreDraw(ref Color lightColor)
    {
        SpriteBatch sb = Main.spriteBatch;
        Texture2D tex = AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons);

        ManagedShader shine = AssetRegistry.GetShader("RadialShineShader");
        shine.TrySetParameter("glowPower", .2f);
        shine.TrySetParameter("glowColor", Color.Goldenrod.ToVector4());
        shine.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 1f);

        sb.EnterShaderRegionAlt();
        shine.Render("AutoloadPass", true, false);

        sb.Draw(tex, ToTarget(Projectile.Center, new(150f)), null, Color.Gold * Projectile.Opacity, Projectile.rotation, tex.Size() * 0.5f, 0, 0f);

        sb.ExitShaderRegion();
        return false;
    }
}