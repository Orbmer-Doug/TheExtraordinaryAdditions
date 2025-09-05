using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.CrossUI;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode.CrossDiscHoldout;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class DiscSlash : ModProjectile
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 40;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = MaxSwingTime;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.noEnchantmentVisuals = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.netImportant = true;
    }
    public override bool? CanCutTiles()
    {
        return base.CanCutTiles();
    }
    public override bool? CanHitNPC(NPC target)
    {
        return base.CanHitNPC(target);
    }
    public enum SwingState
    {
        DownSwing,
        UpSwing,
    }

    /// <summary>
    /// The current elemental mode the cross disc
    /// </summary>
    public Element State
    {
        get => (Element)Projectile.ai[0];
        set => Projectile.ai[0] = (float)value;
    }
    public const int MaxSwingTime = 40;
    public ref float Timer => ref Projectile.ai[1];
    public float Completion => InverseLerp(0f, MaxSwingTime, Timer);
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer ModdedOwner => Owner.Additions();
    public ElementalBalance ElementPlayer => Owner.GetModPlayer<ElementalBalance>();
    public float InitRot => Projectile.velocity.ToRotation();
    public int Direction => Projectile.velocity.X.NonZeroSign();
    private SwingState Swing
    {
        get => (SwingState)Projectile.ai[2];
        set => Projectile.ai[2] = (float)value;
    }
    public float RotShift()
    {
        float animation = Animators.MakePoly(3).OutFunction.Evaluate(Timer, 0f, 40f, -1f, 1f);
        return InitRot + (MathHelper.PiOver2 * -(Swing == SwingState.UpSwing).ToDirectionInt() * animation * Direction);
    }
    public override void AI()
    {
        Vector2 center = Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
        if (Timer == 0f)
        {
            Projectile.velocity = center.SafeDirectionTo(ModdedOwner.mouseWorld);
            Projectile.netUpdate = true;
        }

        if (Timer > MaxSwingTime)
        {
            Projectile.Kill();
            return;
        }

        Projectile.Center = center + PolarVector(Projectile.width * 2.5f, RotShift());

        cache ??= new(30);
        cache.Update(Projectile.Center - Main.screenPosition);

        Timer++;
    }

    private float WidthFunction(float c) => Convert01To010(Completion) * (Projectile.height / 2) * MathHelper.SmoothStep(1f, 0f, c);
    private Color ColorFunction(SystemVector2 c, Vector2 position)
    {
        return State switch
        {
            Element.Neutral => Color.Gray,
            Element.Cold => Color.Cyan,
            Element.Heat => Color.OrangeRed,
            Element.Shock => Color.Purple,
            Element.Wave => Color.Lime,
            _ => Color.White,
        };
    }

    public TrailPoints cache;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            ManagedShader prim = ShaderRegistry.SpecialLightningTrail;
            prim.SetTexture(State switch
            {
                Element.Neutral => AssetRegistry.GetTexture(AdditionsTexture.TechyNoise),
                Element.Cold => AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise),
                Element.Heat => AssetRegistry.GetTexture(AdditionsTexture.FireNoise),
                Element.Shock => AssetRegistry.GetTexture(AdditionsTexture.WavyNeurons),
                Element.Wave => AssetRegistry.GetTexture(AdditionsTexture.SuperWavyPerlin),
                _ => AssetRegistry.GetTexture(AdditionsTexture.TechyNoise)
            }, 1);
            
            ITrailTip tip = new RoundedTip(35);
            OptimizedPrimitiveTrail trail = new(tip, WidthFunction, ColorFunction, null, 12);
            trail.DrawTippedTrail(prim, cache.Points, tip, true, 200, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverPlayers);

        return false;
    }
}