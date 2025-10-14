using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public class WhiteVoid : ModProjectile, IHasScreenShader
{
    public bool IsEntityActive() => Projectile.active;
    public ref float Time => ref Projectile.ai[0];
    public static readonly int Lifetime = SecondsToFrames(1.2f);
    public static readonly int DisappearTime = SecondsToFrames(0.11f);
    public static readonly int FillInTime = SecondsToFrames(0.25f);
    public static readonly int FillInDelay = SecondsToFrames(0.42f);
    public override string Texture => AssetRegistry.Invis;
    public Player Owner => Main.player[Projectile.owner];

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = Projectile.usesLocalNPCImmunity = Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        if (Owner.Available())
            Projectile.Center = Owner.RotatedRelativePoint(Owner.MountedCenter);

        if ((int)Time == (int)FillInDelay)
            AdditionsSound.HeavyWhooshShort.Play(Projectile.Center, 1.1f, .1f, .1f, 20, Name);

        Projectile.ExpandHitboxBy((int)Animators.MakePoly(3f).InFunction.Evaluate(Time, Lifetime, Lifetime - DisappearTime, 0f, 400f));

        Time++;
    }

    public override bool? CanDamage() => Time >= FillInDelay;
    public override bool? CanCutTiles() => false;

    public ManagedScreenShader Shader { get; private set; }
    public bool HasShader { get; private set; } = false;
    public void InitializeShader()
    {
        Shader = ScreenShaderPool.GetShader("WhiteVoid");
        HasShader = true;
        ScreenShaderUpdates.RegisterEntity(this);
    }

    public void UpdateShader()
    {
        float scale = Animators.Circ.OutFunction(InverseLerp(FillInDelay, FillInDelay + FillInTime, Time));
        float fade = Animators.BezierEase.Evaluate(Time, 0f, FillInDelay, 0f, 1.6f);

        Shader.TrySetParameter("screenPos", GetTransformedScreenCoords(Projectile.Center));
        Shader.TrySetParameter("screenSize", Main.ScreenSize.ToVector2());
        Shader.TrySetParameter("edgeColor", Color.AntiqueWhite * fade);
        Shader.TrySetParameter("radius", (float)Projectile.width / Main.screenWidth * Main.GameViewMatrix.Zoom.X);
        Shader.TrySetParameter("scale", scale);
        Shader.TrySetParameter("globalTime", Main.GlobalTimeWrappedHourly);
        Shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.FlameMap2), 1, SamplerState.LinearWrap);

        Shader.Activate();
    }

    public void ReleaseShader()
    {
        if (HasShader)
        {
            Shader.Deactivate();
            ScreenShaderPool.ReturnShader("WhiteVoid", Shader);
            HasShader = false;
            Shader = null;
            ScreenShaderUpdates.UnregisterEntity(this);
        }
    }

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

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, Projectile.width / 3, targetHitbox);
    }
}
