using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;
using static Microsoft.Xna.Framework.MathHelper;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;

namespace TheExtraordinaryAdditions.Content.Projectiles.Melee.Middle;

public class SangueSpin : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Sangue);

    public const float SwordRot = 1.071632161f / 2f;
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public Vector2 Center => Owner.RotatedRelativePoint(Owner.MountedCenter, false, true);
    public float Speed => Owner.GetAttackSpeed(DamageClass.Melee);
    public int Dir => Projectile.velocity.X.NonZeroSign();
    public int ReelTime => (int)(40 / Speed);
    public int ThrowTime => (int)(60 / Speed);
    public ref float Time => ref Projectile.ai[0];
    public enum SangueState
    {
        Reel,
        Throw
    }

    public SangueState State
    {
        get => (SangueState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }

    public bool Init
    {
        get => Projectile.ai[2] == 1f;
        set => Projectile.ai[2] = value.ToInt();
    }

    public ref float Dist => ref Projectile.AdditionsInfo().ExtraAI[0];
    public ref float OldRot => ref Projectile.AdditionsInfo().ExtraAI[1];

    public override void SetDefaults()
    {
        Projectile.Size = new(72, 132);
        Projectile.friendly = Projectile.ignoreWater = Projectile.usesLocalNPCImmunity = true;
        Projectile.hostile = Projectile.tileCollide = false;
        Projectile.localNPCHitCooldown = 6;
        Projectile.penetrate = -1;
        Projectile.DamageType = DamageClass.MeleeNoSpeed;
    }

    public RotatedRectangle Rect() => new(20, Projectile.Center + PolarVector(57f, Projectile.rotation - SwordRot), Projectile.Center + PolarVector(150f, Projectile.rotation - SwordRot));
    public override void AI()
    {
        if (this.RunLocal() && !Owner.Available())
        {
            Projectile.Kill();
            return;
        }

        if (!Init)
        {
            points.Clear();
            Init = true;
        }

        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Projectile.Center.ToNumerics(), 20);

        Owner.heldProj = Projectile.whoAmI;
        Owner.SetDummyItemTime(2);
        Owner.ChangeDir(Dir);

        switch (State)
        {
            case SangueState.Reel:
                float anim = new PiecewiseCurve()
                .Add(0f, -2.1f, .4f, MakePoly(2.8f).InOutFunction)
                .Add(-2.1f, 0f, 1f, MakePoly(5f).InFunction)
                .Evaluate(InverseLerp(0f, ReelTime, Time)) * Dir;

                if (this.RunLocal())
                {
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, Center.SafeDirectionTo(Modded.mouseWorld), .1f);
                    Dist = MathHelper.Clamp(Center.Distance(Modded.mouseWorld), 150f, 1000f);
                    if (Projectile.velocity != Projectile.oldVelocity)
                        this.Sync();
                }

                Projectile.rotation = Projectile.velocity.ToRotation() + SwordRot + anim;
                OldRot = Projectile.rotation;
                Owner.SetFrontHandBetter(0, Projectile.rotation - SwordRot);
                Projectile.Center = Owner.GetFrontHandPositionImproved();

                if (Time > ReelTime)
                {
                    AdditionsSound.IkeSpecial1B.Play(Projectile.Center, .8f, 0f, .2f, 10, Name);
                    State = SangueState.Throw;
                    Time = 0f;
                    Projectile.netUpdate = true;
                    Projectile.netSpam = 0;
                }
                break;
            case SangueState.Throw:
                points.Update(Rect().Center - Projectile.Center);

                float comp = InverseLerp(0f, ThrowTime, Time);
                float lerp = new PiecewiseCurve()
                    .Add(0f, 1f, .5f, MakePoly(6f).OutFunction)
                    .Add(1f, 0f, 1f, MakePoly(4f).InOutFunction)
                    .Evaluate(comp);

                Owner.SetFrontHandBetter(0, Center.AngleTo(Center + Projectile.velocity * Dist));
                Vector2 hand = Owner.GetFrontHandPositionImproved();
                Projectile.Center = Vector2.Lerp(hand, hand + Projectile.velocity * Dist, lerp);
                float lerper = Convert01To010(comp);

                if (comp > .8f)
                    Projectile.rotation = Projectile.rotation.SmoothAngleLerp(OldRot, .3f, .4f);
                else
                    Projectile.rotation += MathHelper.Lerp(0f, .5f, lerper) * Dir;

                if (!Main.dedServ)
                {
                    Vector2 dir = (Projectile.rotation - SwordRot - MathHelper.PiOver2 * -Dir).ToRotationVector2() * Main.rand.NextFloat(2f, 9f);
                    ParticleRegistry.SpawnBloomPixelParticle(Rect().RandomPoint(), dir, Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .7f), Color.DarkRed, Color.Crimson, null, 2f);
                    for (int i = 0; i < 2; i++)
                        ParticleRegistry.SpawnHeavySmokeParticle(Rect().RandomPoint(), dir * .7f, Main.rand.Next(20, 40), Main.rand.NextFloat(.6f, 1.1f), Color.DarkRed, .8f);
                }

                int wait = (int)(5 / Speed);
                if (lerper.BetweenNum(.8f, 1f) && Time % wait == (wait - 1) && this.RunLocal())
                {
                    Projectile.NewProj(Rect().Top, (Projectile.rotation - SwordRot).ToRotationVector2(), ModContent.ProjectileType<SangueGlare>(),
                        Projectile.damage / 4, Projectile.knockBack, Projectile.owner);
                }

                if (comp >= 1f && this.RunLocal())
                {
                    if (Modded.SafeMouseLeft.Current)
                    {
                        Init = false;
                        State = SangueState.Reel;
                        Time = 0f;
                        Projectile.netUpdate = true;
                        Projectile.netSpam = 0;
                    }
                    else if (!Modded.SafeMouseLeft.Current)
                        Projectile.Kill();
                }
                break;
        }

        Time++;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 pos = target.RandAreaInEntity();
            Vector2 vel = (Projectile.rotation - SwordRot + MathHelper.PiOver2 * Dir).ToRotationVector2() * Main.rand.NextFloat(2f, 9f);
            int life = Main.rand.Next(40, 50);
            float scale = Main.rand.NextFloat(.5f, .8f);
            ParticleRegistry.SpawnBloodParticle(pos, vel * 1.4f, life * 2, scale, Color.DarkRed);
            ParticleRegistry.SpawnHeavySmokeParticle(pos, vel, life, scale, Color.DarkRed, .6f);
        }
        AdditionsSound.MimicryLand.Play(Projectile.Center, 1.4f, .4f, .05f, 10, Name);
    }

    public override bool? CanDamage() => State == SangueState.Throw ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return Rect().Intersects(targetHitbox);
    }

    public float WidthFunct(float c) => 101f;
    public Color ColorFunct(SystemVector2 c, Vector2 pos) => Color.DarkRed * Convert01To010(InverseLerp(0f, ThrowTime, Time));
    public OptimizedPrimitiveTrail trail;
    public TrailPoints points = new(10);
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Vector2 origin;
        bool flip = Dir == 1;

        SpriteEffects fx;
        float offset;
        if (flip)
        {
            origin = new Vector2(0, tex.Height);

            offset = SwordRot;
            fx = SpriteEffects.None;
        }
        else
        {
            origin = new Vector2(tex.Width, tex.Height);

            offset = PiOver2;
            fx = SpriteEffects.FlipHorizontally;
        }

        void draw()
        {
            if (trail == null || points == null)
                return;

            ManagedShader shader = ShaderRegistry.SwordRipShader;
            shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.SwordSlashTexture), 1, SamplerState.LinearWrap);
            shader.TrySetParameter("flip", flip);

            trail.DrawTrail(shader, points.Points);
        }

        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor * Projectile.Opacity,
            Projectile.rotation + offset, origin, Projectile.scale, fx, 0f);
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        return false;
    }
}