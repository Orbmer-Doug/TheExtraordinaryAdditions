using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Assets.Audio;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class GammaRay : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        // Since its big we need to make sure it doesn't get cut off
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
    }

    public override void SetDefaults()
    {
        Projectile.Size = new(1);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 8;
        Projectile.DamageType = DamageClass.Default;
        Projectile.penetrate = -1;
    }

    public Projectile ProjOwner => Main.projectile[(int)Projectile.ai[0]];
    public ref float Time => ref Projectile.ai[1];
    public ref float RayLength => ref Projectile.ai[2];
    public ref float Fade => ref Projectile.Additions().ExtraAI[0];

    public const int ExpandTime = 40;
    public Player Owner => Main.player[Projectile.owner];
    public override bool ShouldUpdatePosition() => false;
    public LoopedSoundInstance sound;
    public override void AI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, Amt);
        if (trail2 == null || trail2._disposed)
            trail2 = new(WidthFunct, ColorFunct, null, Amt);

        // Update the ominous hum
        sound ??= LoopedSoundManager.CreateNew(new(AdditionsSound.sunAura, () => Projectile.Opacity * .5f, () => -.5f), () => AdditionsLoopedSound.ProjectileNotActive(Projectile));
        sound?.Update(Projectile.Center);

        // Position and angle relative to genedies
        TheExingendies gen = ProjOwner.As<TheExingendies>();
        if (this.RunLocal())
        {
            Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
            Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, center.SafeDirectionTo(Owner.Additions().mouseWorld),
                Utils.Remap(Owner.Additions().mouseWorld.Distance(center), 0f, 200f, .04f, .16f));
            if (Projectile.velocity != Projectile.oldVelocity)
                this.Sync();
        }

        // Fade away if necessary
        if (gen == null || !Owner.Available() || ProjOwner == null || 
            (ProjOwner.owner == Owner.whoAmI && (ProjOwner.active == false || gen.Phase != TheExingendies.States.ActiveGalacticNucleus || gen.RayWait == true || gen.Completion != 1f)))
        {
            Fade++;
            if (Fade > 20f)
                Projectile.Kill();
        }
        if (ProjOwner != null && ProjOwner.owner == Owner.whoAmI)
            Projectile.Center = ProjOwner.Center;

        // Update graphics
        RayLength = MathF.Max(20f, Animators.Circ.OutFunction.Evaluate(0f, Main.screenHeight * 5, InverseLerp(0f, ExpandTime, Time))
            * Animators.MakePoly(3f).InOutFunction(InverseLerp(20f, 0f, Fade)));
        Projectile.Opacity = Animators.MakePoly(2f).InOutFunction.Evaluate(RayLength, 20f, 50f, 0f, 1f);
        points.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center + Projectile.velocity * RayLength, Amt));
        points2.SetPoints(Projectile.Center.GetLaserControlPoints(Projectile.Center - Projectile.velocity * RayLength, Amt));
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(points.Points, WidthFunct) || targetHitbox.CollisionFromPoints(points2.Points, WidthFunct);
    }

    public const int Amt = 400;
    public ManualTrailPoints points = new(Amt);
    public OptimizedPrimitiveTrail trail;
    public ManualTrailPoints points2 = new(Amt);
    public OptimizedPrimitiveTrail trail2;

    public float WidthFunct(float c) => Animators.MakePoly(1.6f).OutFunction.Evaluate(2f, 1000f, c);
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => MulticolorLerp(c.X, Color.DarkBlue, new Color(50, 200, 220), new Color(120, 140, 220)) * Projectile.Opacity;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null || trail2 == null || points2 == null)
                return;

            ManagedShader ray = AssetRegistry.GetShader("GammaRay");
            ray.TrySetParameter("time", Time * .2f);
            ray.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.DarkTurbulentNoise), 1, SamplerState.AnisotropicWrap);
            trail.DrawTrail(ray, points.Points, Amt, true, false);
            trail2.DrawTrail(ray, points2.Points, Amt, true, false);
        }
        LayeredDrawSystem.QueueDrawAction(draw, PixelationLayer.Dusts);
        return true;
    }
}