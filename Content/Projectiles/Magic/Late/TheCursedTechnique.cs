using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Magic.Late;

public class TheCursedTechnique : ModProjectile, ILocalizedModType, IModType
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 1;
        Projectile.localNPCHitCooldown = 20;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = false;
        Projectile.friendly = true;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
    }
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public ref float Time => ref Projectile.ai[0];
    public ref float ThrowTime => ref Projectile.ai[1];
    public static readonly float ChargeTime = SecondsToFrames(9f);
    public float BlueCompletion => InverseLerp(0f, ChargeTime * .3f, Time);
    public float RedCompletion => InverseLerp(ChargeTime * .3f, ChargeTime * .6f, Time);
    public float PurpleCompletion => InverseLerp(ChargeTime * .6f, ChargeTime, Time);
    public float TotalCompletion => InverseLerp(0f, ChargeTime, Time);
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    private const float Dist = 150f;
    public const int MaxSize = 210;
    public override bool? CanDamage() => ThrowTime > 0f ? null : false;
    public override void AI()
    {
        Projectile.scale = TotalCompletion;
        Projectile.ExpandHitboxBy((int)(MaxSize * Projectile.scale), (int)(MaxSize * Projectile.scale));
        Projectile.Opacity = TotalCompletion;

        if (Time == ChargeTime)
        {
            for (int i = 0; i < 100; i++)
            {
                float lightVelocityArc = MathHelper.Pi * InverseLerp(0f, 100f, i);
                Vector2 pos = Projectile.Center + Main.rand.NextVector2Unit() * MaxSize * Main.rand.NextFloat(0.75f, 0.96f);
                Vector2 vel = (pos - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(lightVelocityArc) * Main.rand.NextFloat(2f, 25f);
                ParticleRegistry.SpawnBloomPixelParticle(pos, vel, Main.rand.Next(40, 70), Main.rand.NextFloat(.5f, 1f), Color.Purple, Color.MediumPurple);
            }
        }

        if (PurpleCompletion > 0f)
        {
            if (ThrowTime > 0f || Main.rand.NextBool(3))
            {
                ParticleRegistry.SpawnLightningArcParticle(Projectile.Center, Main.rand.NextVector2CircularLimited(400f, 400f, .3f, 1f) * PurpleCompletion * (ThrowTime > 0f ? 1.4f : 1f), Main.rand.Next(10, 12),
                    Main.rand.NextFloat(.4f, .8f), Purple[Main.rand.Next(Purple.Length - 1)]);
            }
        }

        if (Owner.channel && ThrowTime <= 0f)
        {
            Owner.ChangeDir((ModdedOwner.mouseWorld.X > Owner.Center.X).ToDirectionInt());
            if (this.RunLocal())
            {
                Projectile.velocity = Center.SafeDirectionTo(ModdedOwner.mouseWorld);
                if (Projectile.velocity != Projectile.oldVelocity)
                    this.Sync();
            }
            Projectile.rotation = Projectile.velocity.ToRotation();
            Owner.SetCompositeArmFront(true, 0, Projectile.rotation - MathHelper.PiOver2);

            Projectile.Center = Center + PolarVector(Dist, Projectile.rotation);
            Projectile.timeLeft = SecondsToFrames(4);
            Time++;
        }

        else if (TotalCompletion >= 1f)
        {
            if (ThrowTime == 1f)
            {
                if (this.RunLocal())
                {
                    Projectile.velocity = Projectile.SafeDirectionTo(ModdedOwner.mouseWorld) * 16f;
                }
                Projectile.extraUpdates = 3;
                this.Sync();
            }

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc != null && npc.active && CircularHitboxCollision(Projectile.Center, Projectile.width * 0.37f, npc.Hitbox) && npc.IsAnEnemy())
                {
                    if (npc.lifeMax < 150000)
                    {
                        npc.checkDead();
                        npc.Kill();
                        npc.active = false;
                    }
                }
            }

            ParticleRegistry.SpawnCloudParticle(Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero) * MaxSize / 2 + Main.rand.NextVector2Circular(20f, 20f),
                -Projectile.velocity.RotatedByRandom(.9f) * Main.rand.NextFloat(.2f, 1.2f),
                Color.Purple, Color.DarkViolet, Main.rand.Next(30, 90), Main.rand.NextFloat(40f, 100f), Main.rand.NextFloat(.7f, 1f));

            ThrowTime++;
        }

        else if (!Owner.channel && ThrowTime <= 0f)
        {
            Time = MathHelper.Clamp(Time - 8f, 0f, ChargeTime);
            if (Time <= 0f)
                Projectile.Kill();
        }


        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = 2;
    }

    public static readonly Color[] Purple =
    [
        Color.Purple,
        Color.MediumPurple,
        Color.Violet,
        Color.BlueViolet,
        Color.DarkViolet,
        Color.DeepPink,
        Color.Violet * 1.6f
    ];
    public static readonly Color[] Reversal =
    [
        Color.Red,
        Color.Crimson,
        Color.Red * 1.6f,
        Color.PaleVioletRed,
        Color.DarkRed,
    ];
    public static readonly Color[] Amplification =
    [
        Color.Blue,
        Color.DarkSlateBlue,
        Color.SlateBlue,
        Color.DodgerBlue,
        Color.MediumBlue,
    ];

    public static Vector2 Resolution => new(500);
    private void TechniqueReversal()
    {
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Vector2 pos = Center + PolarVector(Dist, Projectile.rotation - (MathHelper.PiOver2 * (1f - RedCompletion))) - Main.screenPosition;
        Vector2 scale = new Vector2(MaxSize) * InverseLerp(ChargeTime * .1f, ChargeTime * .2f, Time) / pixel.Size();
        Color color = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly * 2f), Reversal) * (.4f - PurpleCompletion);

        ManagedShader sphere = ShaderRegistry.MagicSphere;
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1, SamplerState.LinearWrap);
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 2, SamplerState.LinearWrap);
        sphere.TrySetParameter("resolution", Resolution);
        sphere.TrySetParameter("posterizationPrecision", 14f);
        sphere.TrySetParameter("mainColor", color.ToVector3());

        sphere.Render();

        Main.spriteBatch.Draw(pixel, pos, null, color, 0f, pixel.Size() / 2, scale * 1.2f, 0, 0f);
    }
    private void TechniqueAmplification()
    {
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Vector2 pos = Center + PolarVector(Dist, Projectile.rotation + (MathHelper.PiOver2 * (1f - BlueCompletion))) - Main.screenPosition;
        Vector2 scale = new Vector2(MaxSize) * InverseLerp(0f, ChargeTime * .1f, Time) / pixel.Size();
        Color color = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly * 2f), Amplification) * (.4f - PurpleCompletion);

        ManagedShader sphere = ShaderRegistry.MagicSphere;
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1, SamplerState.LinearWrap);
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 2, SamplerState.LinearWrap);
        sphere.TrySetParameter("resolution", Resolution);
        sphere.TrySetParameter("posterizationPrecision", 14f);
        sphere.TrySetParameter("mainColor", color.ToVector3());
        sphere.Render();

        Main.spriteBatch.Draw(pixel, pos, null, color, 0f, pixel.Size() / 2, scale * 1.2f, 0, 0f);
    }
    private void PurpleBlackhole()
    {
        Texture2D pixel = AssetRegistry.GetTexture(AdditionsTexture.Pixel);

        Vector2 pos = Projectile.Center - Main.screenPosition;
        float fadeOut = InverseLerp(0f, 95f, Projectile.timeLeft);
        Vector2 scale = Projectile.Size / pixel.Size() * PurpleCompletion * fadeOut;
        Color color = MulticolorLerp(Sin01(Main.GlobalTimeWrappedHourly), Purple) * PurpleCompletion * fadeOut;


        ManagedShader sphere = ShaderRegistry.MagicSphere;
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1, SamplerState.LinearWrap);
        sphere.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SuperPerlin), 2, SamplerState.LinearWrap);
        sphere.TrySetParameter("resolution", Resolution);
        sphere.TrySetParameter("posterizationPrecision", 18f);
        sphere.TrySetParameter("mainColor", color.ToVector3());
        sphere.Render();

        Main.spriteBatch.Draw(pixel, pos, null, Projectile.GetAlpha(new Color(0.7f, 1f, 1f)), 0f, pixel.Size() / 2, scale * 1.2f, 0, 0f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.PrepareForShaders();
        if (PurpleCompletion < 1f)
        {
            TechniqueAmplification();
            TechniqueReversal();
        }
        if (PurpleCompletion > 0f)
            PurpleBlackhole();
        Main.spriteBatch.ResetToDefault();
        return false;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
                CircularHitboxCollision(Projectile.Center, Projectile.width * 0.37f, targetHitbox);
}
