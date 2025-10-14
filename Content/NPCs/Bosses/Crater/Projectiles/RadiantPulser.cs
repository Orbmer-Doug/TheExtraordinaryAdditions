using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.DataStructures;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;

public class RadiantPulser : ProjOwnedByNPC<Asterlin>
{
    public override string Texture => AssetRegistry.Invis;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2000;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.hostile = true;
    }

    public const int MaxLength = 2500;
    public const int FadeTime = 110;
    public int Time
    {
        get => (int)Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public ref float Length => ref Projectile.ai[1];

    public ref float Offset => ref Projectile.ai[2];

    public enum State
    {
        Positioning,
        Firing,
        Fade,
    }
    public State CurrentState
    {
        get => (State)Projectile.AdditionsInfo().ExtraAI[0];
        set => Projectile.AdditionsInfo().ExtraAI[0] = (int)value;
    }
    public ref float TargetAngle => ref Projectile.AdditionsInfo().ExtraAI[1];
    public ref float SavedRotation => ref Projectile.AdditionsInfo().ExtraAI[2];
    public int FireCount
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[3];
        set => Projectile.AdditionsInfo().ExtraAI[3] = value;
    }
    public int StateTime
    {
        get => (int)Projectile.AdditionsInfo().ExtraAI[4];
        set => Projectile.AdditionsInfo().ExtraAI[4] = value;
    }
    public ref float PreviousTargetAngle => ref Projectile.AdditionsInfo().ExtraAI[5];
    public bool NotMain
    {
        get => Projectile.AdditionsInfo().ExtraAI[6] == 1f;
        set => Projectile.AdditionsInfo().ExtraAI[6] = value.ToInt();
    }

    public Vector2 TargetPosition
    {
        get => new Vector2(Projectile.AdditionsInfo().ExtraAI[7], Projectile.AdditionsInfo().ExtraAI[8]);
        set
        {
            Projectile.AdditionsInfo().ExtraAI[7] = value.X;
            Projectile.AdditionsInfo().ExtraAI[8] = value.Y;
        }
    }

    public override void SafeAI()
    {
        if (Time == 0 && !NotMain && this.RunServer())
        {
            for (int i = 0; i < 2; i++)
                Main.projectile[SpawnProjectile(Projectile.Center, Vector2.Zero, Type, Projectile.damage, 0f)].AdditionsInfo().ExtraAI[6] = 1;
        }

        if (light == null || light.Disposed)
            light = new(WidthFunct, ColorFunct, null, 40);

        if (CurrentState != State.Fade)
            Length = Animators.BezierEase.Evaluate(Time, 0f, 50f, 0f, MaxLength);
        Projectile.AI_GetMyGroupIndex(out int index, out int total);
        float projOffset = (MathHelper.TwoPi / 3f) * index;

        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Vector2 start = Projectile.Center;
        Vector2 top = start - dir * Length;
        Vector2 bottom = start + dir * Length;
        points.SetPoints(top.GetLaserControlPoints(bottom, 40));

        Vector2 dirToTarget = Projectile.Center.SafeDirectionTo(TargetPosition);

        switch (CurrentState)
        {
            case State.Positioning:
                if (StateTime == 0)
                {
                    TargetAngle = RandomRotation();

                    if (!NotMain)
                    {
                        // Prevent choosing targets too close to where it already went
                        int tries = 0;
                        while (tries < 100)
                        {
                            if (TargetAngle.BetweenNum(PreviousTargetAngle - MathHelper.PiOver4, PreviousTargetAngle + MathHelper.PiOver4))
                                TargetAngle = RandomRotation();
                            else
                                break;
                            tries++;
                        }
                        PreviousTargetAngle = TargetAngle;
                    }
                    else
                    {
                        foreach (Projectile proj in Main.ActiveProjectiles)
                        {
                            if (proj.type == Type)
                                proj.As<RadiantPulser>().TargetAngle = TargetAngle;
                        }
                    }
                    SavedRotation = Projectile.rotation;
                    this.Sync();
                }

                TargetPosition = Target.Center;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, new Vector2(-dirToTarget.Y, dirToTarget.X).RotatedBy(Offset), .2f);

                Projectile.rotation = Utils.AngleLerp(SavedRotation, MathHelper.WrapAngle(TargetAngle + projOffset), Animators.CubicBezier(.74f, .05f, .38f, .81f)(InverseLerp(0f, Asterlin.RotatedDicing_PositioningTime, StateTime)));
                Projectile.Center = Vector2.SmoothStep(Projectile.Center, Target.Center + PolarVector(500f, Projectile.rotation), .3f);

                StateTime++;
                if (StateTime >= Asterlin.RotatedDicing_PositioningTime)
                {
                    StateTime = 0;
                    CurrentState = State.Firing;
                    Projectile.netUpdate = true;
                }
                break;
            case State.Firing:
                if (StateTime % Asterlin.RotatedDicing_Wait == (Asterlin.RotatedDicing_Wait - 1))
                {
                    if (this.RunServer())
                    {
                        for (int i = (int)(-Length / 2f); i < (int)(Length / 2f); i += Asterlin.RotatedDicing_Spacing)
                        {
                            Vector2 pos = Projectile.Center + dir * i;
                            Vector2 vel = new(-dir.Y, dir.X);
                            SpawnProjectile(pos, vel, ModContent.ProjectileType<BurstingLight>(), Asterlin.MediumAttackDamage, 0f);
                        }
                    }

                    AdditionsSound.etherealSharpImpactB.Play(Projectile.Center, .8f, .1f, 0f, 10, Name);
                    ParticleRegistry.SpawnPulseRingParticle(ModOwner.RightHandPosition, Vector2.Zero, 24, RandomRotation(), Vector2.One, 0f, 250f, Color.Gold, true);
                    Offset = Main.rand.NextFloat(-.32f, .32f);
                    FireCount++;
                }

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, new Vector2(-dirToTarget.Y, dirToTarget.X).RotatedBy(Offset), .2f);

                StateTime++;
                if (FireCount >= Asterlin.RotatedDicing_FireCount)
                {
                    StateTime = FireCount = 0;

                    if (!NotMain)
                        ModOwner.RotatedDicing_Cycle++;
                    if (ModOwner.RotatedDicing_Cycle >= Asterlin.RotatedDicing_Cycles)
                        CurrentState = State.Fade;
                    else
                        CurrentState = State.Positioning;
                    Projectile.netUpdate = true;
                }
                break;
            case State.Fade:
                float comp = InverseLerp(0f, FadeTime, StateTime);
                Length = Animators.MakePoly(2f).InFunction.Evaluate(MaxLength, 0f, comp);
                if (comp >= 1f)
                    Kill();
                StateTime++;
                break;
        }

        Time++;
    }

    public override bool ShouldUpdatePosition() => false;

    public float WidthFunct(float c)
    {
        float width = Projectile.width;
        if (CurrentState == State.Fade)
            width = Animators.MakePoly(3f).InFunction.Evaluate(Projectile.width, 0f, InverseLerp(0f, FadeTime, StateTime));
        return width;
    }

    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        return Color.White * GetLerpBump(0f, .2f, 1f, .8f, c.X);
    }

    public OptimizedPrimitiveTrail light;
    public TrailPoints points = new(40);
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (light == null || points == null)
                return;

            ManagedShader shader = AssetRegistry.GetShader("RadiantPulserShader");
            light.DrawTrail(shader, points.Points);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.OverProjectiles);
        return false;
    }
}
