using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.UI.CrossUI;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode.CrossDiscHoldout;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossCode;

public class CrossSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.Invis;
    public ElementalBalance ElementPlayer => Owner.GetModPlayer<ElementalBalance>();
    public Element State
    {
        get => (Element)Projectile.Additions().ExtraAI[7];
        set => Projectile.Additions().ExtraAI[7] = (float)value;
    }

    public int SwingCounter
    {
        get => (int)Projectile.Additions().ExtraAI[8];
        set => Projectile.Additions().ExtraAI[8] = value;
    }
    public bool Spin => SwingCounter >= 3;

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 50;
        Projectile.localNPCHitCooldown = 9 * MaxUpdates;
    }

    public override float SwordRotation => 0f;
    public override float SwingAngle => MathHelper.PiOver2;
    public override int SwingTime => Spin ? 80 : 50;

    public override float Animation()
    {
        if (Spin)
            return Animators.Exp(2.5f).OutFunction.Evaluate(Time, 0f, MaxTime, -1f, 5f);

        return Animators.Exp(2f).OutFunction.Evaluate(Time, 0f, MaxTime, -1f, 1f);
    }

    public override void SafeInitialize()
    {
        points.Clear();

        if (this.RunLocal())
        {
            switch (State)
            {
                case Element.Neutral:
                    break;
                case Element.Cold:
                    for (int a = 0; a < 5; a++)
                    {
                        Vector2 newVelocity = Center.SafeDirectionTo(Modded.mouseWorld).RotatedByRandom(MathHelper.ToRadians(12)) * 12f;

                        newVelocity *= 1f - Main.rand.NextFloat(0.3f);

                        Projectile.NewProj(Center, newVelocity, ModContent.ProjectileType<BouncyIcicle>(), Projectile.damage / 5, Projectile.knockBack / 9, Projectile.owner);
                    }
                    break;
                case Element.Heat:
                    Vector2 target = ClosestPointOnLineSegment(Modded.mouseWorld, Center - Vector2.UnitX * Main.LogicCheckScreenWidth / 2, Center + Vector2.UnitX * Main.LogicCheckScreenWidth / 2);
                    Vector2 pos;
                    for (int i = 0; i < 2; i++)
                    {
                        pos = target - new Vector2(Main.rand.NextFloat(-300f, 300f), 800f);
                        pos.Y -= 200 * i;

                        Vector2 vel = Vector2.UnitY.RotatedByRandom(.3f) * Main.rand.NextFloat(4f, 10f);
                        Projectile.NewProj(pos, vel, ModContent.ProjectileType<ScarletMeteor>(), Projectile.damage / 3, Projectile.knockBack / 2, Owner.whoAmI);
                    }
                    break;
                case Element.Shock:

                    break;
                case Element.Wave:
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProj(Center, Center.SafeDirectionTo(Modded.mouseWorld).RotatedByRandom(.6f) * Main.rand.NextFloat(10f, 12f),
                            ModContent.ProjectileType<WaveSiphon>(), Projectile.damage / 5, 0f, Projectile.owner);
                    }
                    break;
            }
        }

        ElementPlayer.ElementalResourceCurrent += 8;
    }

    public override void SafeAI()
    {
        if (trail == null || trail._disposed)
            trail = new(WidthFunct, ColorFunct, (c) => Center.ToNumerics(), 20);

        Owner.ChangeDir(Direction);

        Projectile.rotation = SwingOffset();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Projectile.Center = Center + PolarVector(90f, Projectile.rotation);
        points.Update(Projectile.Center - Center);

        if (SwingCompletion >= .2f && !PlayedSound)
        {
            AdditionsSound sound = new();
            switch (State)
            {
                case Element.Neutral:
                    sound = AdditionsSound.NeutralSweep;
                    if (Spin)
                        sound = AdditionsSound.NeutralSweepMassive;
                    break;
                case Element.Cold:
                    sound = AdditionsSound.ColdSweep;
                    if (Spin)
                        sound = AdditionsSound.ColdSweepMassive;
                    break;
                case Element.Heat:
                    sound = AdditionsSound.HeatSweep;
                    if (Spin)
                        sound = AdditionsSound.HeatSweepMassive;
                    break;
                case Element.Shock:
                    sound = AdditionsSound.ShockSweep;
                    if (Spin)
                        sound = AdditionsSound.ShockSweepMassive;
                    break;
                case Element.Wave:
                    sound = AdditionsSound.WaveSweep;
                    if (Spin)
                        sound = AdditionsSound.WaveSweepMassive;
                    break;
            }
            sound.Play(Projectile.Center, 1.2f, 0f, .2f, 10, Name);

            PlayedSound = true;
        }

        if (VanishTime <= 0)
        {
            Projectile.scale = Animators.MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
        }
        else
        {
            Projectile.scale = Animators.MakePoly(3f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, 1f, 0f);
            if (Projectile.scale <= 0f)
                KillEffect();
            VanishTime++;
        }

        // Reset if still holding left, otherwise fade
        if (this.RunLocal() && SwingCompletion >= 1f)
        {
            if (Modded.SafeMouseRight.Current && VanishTime <= 0)
            {
                SwingCounter = (SwingCounter + 1) % 4;
                SwingDir = SwingDir == SwingDirection.Up ? SwingDirection.Down : SwingDirection.Up;
                Initialized = false;
                this.Sync();
            }
            else
            {
                VanishTime++;
                this.Sync();
            }
        }
    }

    public override bool? CanDamage() => InverseLerp(0.018f, 0.05f, AngularVelocity) > .2f ? null : false;

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Center, Center + PolarVector(WidthFunct(1f) + 30f, Projectile.rotation), 20f);
    }

    public override void NPCHitEffects(in Vector2 start, in Vector2 end, NPC npc, NPC.HitInfo hit)
    {
        switch (State)
        {
            case Element.Neutral:
                AdditionsSound.NeutralHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
            case Element.Cold:
                AdditionsSound.ColdHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
            case Element.Heat:
                AdditionsSound.HeatHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
            case Element.Shock:
                AdditionsSound.ShockHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);

                if (this.RunLocal())
                {
                    ShockLightning shock = Main.projectile[Projectile.NewProj(npc.Center - Vector2.UnitY * 800f, Vector2.Zero,
                        ModContent.ProjectileType<ShockLightning>(), Projectile.damage / 2, 0f, Projectile.owner)].As<ShockLightning>();
                    shock.End = npc.Center;
                }
                break;
            case Element.Wave:
                AdditionsSound.WaveHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
        }
        ParticleRegistry.SpawnCrossCodeHit(start, ParticleRegistry.CrosscodeHitType.Medium, State);
    }

    public float WidthFunct(float c) => 120f * Projectile.scale;
    public Color ColorFunct(SystemVector2 c, Vector2 pos)
    {
        float opacity = InverseLerp(0.018f, 0.05f, AngularVelocity);

        Color col = Color.White;
        switch (State)
        {
            case Element.Neutral:
                col = new(169, 195, 205);
                break;
            case Element.Cold:
                col = new Color(99, 157, 255);
                break;
            case Element.Heat:
                col = new(255, 160, 71);
                break;
            case Element.Shock:
                col = new(221, 93, 243);
                break;
            case Element.Wave:
                col = new(57, 255, 101);
                break;
        }

        return col * opacity;
    }

    public TrailPoints points = new(20);
    public OptimizedPrimitiveTrail trail;
    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (points == null || trail == null || Time < 10f)
                return;

            ManagedShader shader = AssetRegistry.GetShader("CrossDiscSwing");
            bool flip = SwingDir != SwingDirection.Up;
            if (Direction == -1)
                flip = SwingDir == SwingDirection.Up;
            shader.TrySetParameter("flip", flip);

            Texture2D noise;

            switch (State)
            {
                case Element.Neutral:
                    noise = AssetRegistry.GetTexture(AdditionsTexture.TechyNoise);
                    break;
                case Element.Cold:
                    noise = AssetRegistry.GetTexture(AdditionsTexture.CrackedNoise);
                    break;
                case Element.Heat:
                    noise = AssetRegistry.GetTexture(AdditionsTexture.HarshNoise);
                    break;
                case Element.Shock:
                    noise = AssetRegistry.GetTexture(AdditionsTexture.NeuronNoise);
                    break;
                case Element.Wave:
                    noise = AssetRegistry.GetTexture(AdditionsTexture.WavyBlotchNoise);
                    break;
                default:
                    noise = AssetRegistry.GetTexture(AdditionsTexture.noise);
                    break;
            }

            shader.SetTexture(noise, 0, SamplerState.LinearWrap);

            trail.DrawTrail(shader, points.Points, 100, true);
        }
        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}