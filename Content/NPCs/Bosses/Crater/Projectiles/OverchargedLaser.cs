using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class OverchargedLaser : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
    }
    public override void SetDefaults()
    {
        Projectile.Size = new(20);
        Projectile.hostile = true;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        CooldownSlot = ImmunityCooldownID.Bosses;
    }

    public ref float Time => ref Projectile.ai[0];
    public bool DontHome
    {
        get => Projectile.ai[1] == 1f;
        set => Projectile.ai[1] = value.ToInt();
    }
    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, null, 10);
        points.Update(Projectile.Center);

        if (Time.BetweenNum(50f, 130f))
        {
            if (!DontHome)
            {
                float speed = Animators.MakePoly(3f).OutFunction.Evaluate(Time, 50f, 80f, 20f, 12f);
                float amt = InverseLerp(50f, 130f, Time) * .8f;
                Projectile.velocity = Vector2.SmoothStep(Projectile.velocity, Projectile.Center.SafeDirectionTo(Target.Center) * speed, amt);
            }
        }
        else if (Time > 130f)
        {
            if (Projectile.velocity.Length() < 60f)
                Projectile.velocity *= 1.05f;
        }

        Time++;
    }

    public float WidthFunct(float c)
    {
        float tipInterpolant = MathF.Sqrt(1f - Animators.MakePoly(2f).InFunction(InverseLerp(0.3f, 0f, c)));
        float width = InverseLerp(1f, 0.4f, c) * tipInterpolant * Projectile.scale;
        return width * Projectile.width;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return MulticolorLerp(c.X, Color.LightCyan, Color.Cyan, Color.DarkCyan);
    }

    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(10);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || points == null || trail._disposed)
                return;

            ManagedShader shader = AssetRegistry.GetShader("OverchargedLaserShader");
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap1), 1, SamplerState.AnisotropicWrap);
            shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly * 1.2f);
            trail.DrawTrail(shader, points.Points, 200, true, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        return false;
    }
}
