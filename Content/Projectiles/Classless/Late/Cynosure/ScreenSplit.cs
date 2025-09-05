using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.Cynosure;

public class ScreenSplit : ModProjectile, IHasScreenShader
{
    public override string Texture => AssetRegistry.Invis;
    public ref float Time => ref Projectile.ai[0];
    public ref float Width => ref Projectile.ai[1];
    public const int Lifetime = 110;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 60000;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.tileCollide = Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = Lifetime;
        Projectile.localNPCHitCooldown = 1;
        Projectile.penetrate = -1;
    }

    public override void AI()
    {
        Projectile.Opacity = MakePoly(2f).OutFunction.Evaluate(Time, 0f, 20f, 0f, 1f);

        float max = 40f / Main.ScreenSize.X;
        Width = new PiecewiseCurve()
            .Add(0f, max, 30f / Lifetime, MakePoly(4f).InOutFunction)
            .AddStall(max, 90f / Lifetime)
            .Add(max, 0f, 1f, MakePoly(3f).OutFunction)
            .Evaluate(InverseLerp(0f, Lifetime, Time));

        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        return targetHitbox.LineCollision(Projectile.Center - dir * 5000f, Projectile.Center + dir * 5000f, Width * 3f);
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("GenediesScreenSplit");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        Vector2 size = Main.ScreenSize.ToVector2();
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitY);
        Shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        Shader.TrySetParameter("glitchIntensity", .04f * Projectile.Opacity);
        Shader.TrySetParameter("screenSize", size);
        Shader.TrySetParameter("splitWidth", Width);
        Shader.TrySetParameter("splitCenter", GetTransformedScreenCoords(Projectile.Center) / size);
        Shader.TrySetParameter("splitDirection", new Vector2(-dir.Y, dir.X));
        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("GenediesScreenSplit", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }
    public bool IsEntityActive() => Projectile.active;

    public override bool PreDraw(ref Color lightColor)
    {
        if (!HasShader)
            InitializeShader();

        UpdateShader();

        return false;
    }

    public override bool PreKill(int timeLeft)
    {
        ReleaseShader();
        return true;
    }
}
