using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using static TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late.Zenith.CoalescenceHoldout;
using static TheExtraordinaryAdditions.Core.Graphics.Animators;


namespace TheExtraordinaryAdditions.Content.Projectiles.Ranged.Late.Zenith;

public class DivinityArrow : ModProjectile
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.DivinityArrow);
    public override void SetDefaults()
    {
        Projectile.height = 124;
        Projectile.width = 30;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 14;
        Projectile.netImportant = true;
    }

    public Vector2 TipOfArrow => Projectile.RotHitbox().Top;

    /// <summary>
    /// The bow
    /// </summary>
    public Projectile ProjOwner => Main.projectile[(int)Projectile.ai[0]];
    public CoalescenceState State
    {
        get => (CoalescenceState)Projectile.ai[1];
        set => Projectile.ai[1] = (int)value;
    }

    public NPC target;

    /// <summary>
    /// The type of arrow this is, 0 is the leading one
    /// </summary>
    public ref float ArrowType => ref Projectile.ai[2];

    /// <summary>
    /// The amount of time spent released
    /// </summary>
    public ref float Time => ref Projectile.Additions().ExtraAI[0];

    /// <summary>
    /// The amount of time spent released
    /// </summary>
    public ref float Charge => ref Projectile.Additions().ExtraAI[1];

    /// <summary>
    /// If the arrow has decided on hitting the ground
    /// </summary>
    public bool HitGround
    {
        get => Projectile.Additions().ExtraAI[2] == 1f;
        set => Projectile.Additions().ExtraAI[2] = value.ToInt();
    }

    /// <summary>
    /// Whether or not the arrow has been released
    /// </summary>
    public bool Release
    {
        get => Projectile.Additions().ExtraAI[3] == 1f;
        set => Projectile.Additions().ExtraAI[3] = value.ToInt();
    }

    /// <summary>
    /// For startup variables of the arrow
    /// </summary>
    public bool Init
    {
        get => Projectile.Additions().ExtraAI[4] == 1f;
        set => Projectile.Additions().ExtraAI[4] = value.ToInt();
    }

    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();
    public float ChargeCompletion => InverseLerp(0f, ReelTime, Charge);
    public override bool ShouldUpdatePosition()
    {
        if (Owner.Additions().MouseLeft.Current && Release == false)
            return false;
        return true;
    }
    public override void ModifyDamageHitbox(ref Rectangle hitbox)
    {
        hitbox.Height = 2;
    }
    public override void AI()
    {
        // Begin
        if (!Init)
        {
            Charge = 0;
            HitGround = false;
            Release = false;
            Init = true;
            this.Sync();
        }

        // Set the arrows rotations
        float totalRot = .45f * (1f - MakePoly(2).OutFunction(ChargeCompletion));
        float rot = ArrowType == -1 ? -totalRot : ArrowType == 1 ? totalRot : 0f;
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        Projectile.Opacity = InverseLerp(0f, 20f, Charge) * InverseLerp(0f, 20f, Projectile.timeLeft);
        Lighting.AddLight(TipOfArrow, Color.Gold.ToVector3() * ChargeCompletion * 1.4f * Projectile.Opacity);

        // Die if not at sufficient charge
        if (this.RunLocal() && !Owner.Additions().MouseLeft.Current && ChargeCompletion < .33f)
        {
            Projectile.Kill();
        }

        // Otherwise rack up the charge and set the positions
        if ((this.RunLocal() && Owner.Additions().MouseLeft.Current) && Release == false)
        {
            Charge++;

            switch (State)
            {
                case CoalescenceState.Richochet:
                    break;
                case CoalescenceState.Pierce:
                    break;
                case CoalescenceState.Blast:
                    // Emphasize beeg power
                    for (int i = -1; i <= 1; i += 2)
                    {
                        float speed = Main.rand.NextFloat(2f, 10f);
                        Vector2 vel = -Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(.45f * i) * speed;
                        float scale = .5f * InverseLerp(10f, 2f, speed, true);
                        int life = 20;
                        ParticleRegistry.SpawnSquishyLightParticle(TipOfArrow, vel, life, scale, Color.Gold);
                    }
                    break;
            }

            // Set the arrows position
            Projectile.Center = ProjOwner.As<CoalescenceHoldout>().arrowPos + PolarVector(Projectile.height / 2f, Projectile.velocity.ToRotation());
            Projectile.timeLeft = 1000;
            Projectile.velocity = ProjOwner.velocity.RotatedBy(rot);
            this.Sync();
        }

        // Behavior for when the arrow has been released
        if (Release == true)
        {
            trailPoints.Update(TipOfArrow);

            Time++;
            switch (State)
            {
                case CoalescenceState.Richochet:
                    Projectile.penetrate = 1;
                    Projectile.tileCollide = true;
                    if (HitGround)
                    {
                        Projectile.extraUpdates = 1;
                        target = NPCTargeting.GetClosestNPC(new(Projectile.Center, 1200, true));
                        if (target.CanHomeInto())
                        {
                            Vector2 vel = Projectile.SafeDirectionTo(target.Center + target.velocity) * 20f;
                            Projectile.velocity = Vector2.Lerp(Projectile.velocity, vel, .11f);
                        }
                        this.Sync();
                    }
                    break;
                case CoalescenceState.Pierce:
                    Projectile.extraUpdates = 1;
                    Projectile.penetrate = -1;
                    break;
                case CoalescenceState.Blast:
                    Projectile.extraUpdates = 2;
                    Projectile.penetrate = 1;

                    if (Collision.SolidCollision(Projectile.Center, Projectile.width, Projectile.height))
                    {
                        Projectile.velocity *= .9f;
                        if (!HitGround)
                        {
                            if (Projectile.timeLeft > 30)
                                Projectile.timeLeft = 30;
                            CreateBlast(TipOfArrow);
                            HitGround = true;
                            this.Sync();
                        }
                    }
                    break;
            }
            Projectile.netUpdate = true;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        switch (State)
        {
            case CoalescenceState.Richochet:
                // Bounce off of tiles
                if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
                    Projectile.velocity.X = -oldVelocity.X;
                if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
                    Projectile.velocity.Y = -oldVelocity.Y;

                if (!HitGround)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        ParticleRegistry.SpawnGlowParticle(TipOfArrow, Vector2.Zero, 20, 3f - i, Color.LightGoldenrodYellow);
                        ParticleRegistry.SpawnPulseRingParticle(TipOfArrow, Vector2.Zero, 15, RandomRotation(), new(.5f, 1f), 0f, Main.rand.NextFloat(260f, 300f), Color.DarkGoldenrod);
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(2f, 2f);
                        float scale = Main.rand.NextFloat(1f, 1.7f);
                        ParticleRegistry.SpawnBloomLineParticle(TipOfArrow, vel * Main.rand.NextFloat(1f, 3f), 35, scale, Color.Gold);
                        ParticleRegistry.SpawnSparkParticle(TipOfArrow, vel, Main.rand.Next(20, 34), scale, Color.Gold);
                    }
                    AdditionsSound.etherealBounce.Play(TipOfArrow, 1f, 0f, .1f);
                    HitGround = true;
                }
                else
                {
                    for (int i = 0; i < 12; i++)
                    {
                        ParticleRegistry.SpawnSquishyPixelParticle(TipOfArrow, oldVelocity.RotatedByRandom(.45f) * Main.rand.NextFloat(1.4f, 2.4f), Main.rand.Next(50, 90), Main.rand.NextFloat(.8f, 1.4f), Color.Gold, Color.Goldenrod, 5, true);
                        ParticleRegistry.SpawnGlowParticle(TipOfArrow + Main.rand.NextVector2Circular(8f, 8f), Main.rand.NextVector2Circular(4f, 4f), Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .6f), Color.Gold);
                    }
                }
                break;
            case CoalescenceState.Pierce:
                break;
            case CoalescenceState.Blast:
                return true;
        }

        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteVector2(Projectile.position);
        writer.Write(Projectile.rotation);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.position = reader.ReadVector2();
        Projectile.rotation = reader.ReadSingle();
    }

    public override void OnKill(int timeLeft)
    {
        for (int i = 0; i < 30; i++)
        {
            Vector2 vel = Projectile.velocity.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 4f) * ChargeCompletion;
            int life = Main.rand.Next(18, 24);
            float scale = Main.rand.NextFloat(.6f, 1.1f);
            ParticleRegistry.SpawnGlowParticle(Projectile.RotHitbox().RandomPoint(), vel, life, scale, Color.Gold);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float _ = 0f;
        Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.Zero);
        Vector2 start = Projectile.Center;
        Vector2 end = start + dir * (Projectile.height * 1f) * Projectile.scale;
        float width = Projectile.width;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, width, ref _);
    }

    public override bool? CanHitNPC(NPC target) => Release ? null : false;

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.RotHitbox().TryGetIntersectionPoints(target.RotHitbox(), out List<Vector2> points))
            HitEffects(points[0]);
        else
            HitEffects(TipOfArrow);
    }

    public void HitEffects(Vector2 pos)
    {
        switch (State)
        {
            case CoalescenceState.Richochet:
                if (HitGround)
                {
                    ParticleRegistry.SpawnDetailedBlastParticle(pos, Vector2.Zero, new Vector2(1f, .5f) * 250f, Vector2.Zero, 22, Color.Gold, Projectile.rotation, Color.DarkGoldenrod, true);
                    for (int i = 0; i < 2; i++)
                    {
                        int amal = ModContent.ProjectileType<CoalescedAmalgam>();
                        Vector2 amalVel = -Projectile.velocity.SafeNormalize(Vector2.Zero)
                            .RotatedBy(Main.rand.NextFloat(.3f, .45f) * (i % 2f == 0f).ToDirectionInt()) * Main.rand.NextFloat(16f, 26f);
                        if (this.RunLocal())
                            Projectile.NewProj(pos, amalVel, amal, Projectile.damage, Projectile.knockBack, Projectile.owner);
                    }
                    AdditionsSound.etherealSmash2.Play(pos, 7f, 0f, .2f);
                    ScreenShakeSystem.New(new(6f, .7f), pos);
                }
                break;
            case CoalescenceState.Pierce:
                AdditionsSound.etherealHit2.Play(pos, 1f, 0f, .1f, 8);
                for (int i = 0; i < 20; i++)
                {
                    Vector2 sparkVel = Projectile.velocity.RotatedByRandom(Main.rand.NextFloat(.33f, .46f)) * Main.rand.NextFloat(.9f, 1.8f);
                    float size = Main.rand.NextFloat(.8f, .9f);
                    int life = Main.rand.Next(22, 30);
                    Color col = Color.DarkGoldenrod;
                    ParticleRegistry.SpawnSparkParticle(pos, sparkVel, life, size, col);
                    ParticleRegistry.SpawnSquishyPixelParticle(pos, sparkVel * .8f, life * 2, size * 1.2f, col, Color.PaleGoldenrod, 4);
                    if (i % 2f == 0f)
                    {
                        ParticleRegistry.SpawnGlowParticle(pos, sparkVel * .3f, 20, size * 16.75f, col);
                    }
                }
                break;
            case CoalescenceState.Blast:
                CreateBlast(pos);
                break;
        }
    }

    public void CreateBlast(Vector2 pos)
    {
        if (this.RunLocal())
        {
            int type = ModContent.ProjectileType<ExtraordinaryHyperBlast>();
            int damage = Projectile.damage * 2;
            Projectile blast = Main.projectile[Projectile.NewProj(pos, Vector2.Zero, type, damage,
                Projectile.knockBack, Projectile.owner, 2f, 0f, 0f, 0f, 0f)];
            blast.scale = ArrowType == -1 ? .5f : ArrowType == 0 ? .75f : 1f;
        }
    }

    public float TrailWidth(float completionRatio)
    {
        float tipInterpolant = MathF.Sqrt(1f - MathF.Pow(Utils.GetLerpValue(0.3f, 0f, completionRatio, true), 2f));
        float width = Utils.GetLerpValue(1f, 0.4f, completionRatio, true) * tipInterpolant * Projectile.scale;
        return width * Projectile.width;
    }

    public TrailPoints trailPoints = new(20);
    public override bool PreDraw(ref Color lightColor)
    {
        if (Release)
        {
            void draw()
            {
                ManagedShader shader = ShaderRegistry.SwingShader;
                shader.SetTexture(AssetRegistry.GetTexture(AdditionsTexture.HarshNoise), 1);
                shader.TrySetParameter("color", Color.Gold);
                shader.TrySetParameter("secondColor", Color.Yellow);
                shader.TrySetParameter("thirdColor", Color.Goldenrod);
                OptimizedPrimitiveTrail trail = new(TrailWidth, (c, pos) => Color.White, null, 20);
                trail.DrawTrail(shader, trailPoints.Points, 200, true);
            }
            PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);
        }

        void arrow()
        {
            Texture2D tex = Projectile.ThisProjectileTexture();
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 orig = tex.Size() * .5f;
            Color col = Projectile.GetAlpha(Color.White) * Projectile.Opacity;
            float scale = Projectile.scale;
            float rot = Projectile.rotation;

            Texture2D star = AssetRegistry.GetTexture(AdditionsTexture.LensStar);
            Vector2 starOrig = star.Size() * .5f;
            Texture2D bloom = AssetRegistry.GetTexture(AdditionsTexture.GlowParticleSmall);
            Vector2 bloomOrig = bloom.Size() * .5f;
            Vector2 bloomDrawPos = TipOfArrow - Main.screenPosition;
            float scale2 = Projectile.scale * ChargeCompletion * .5f;

            Main.spriteBatch.Draw(star, bloomDrawPos, null, Color.Gold * ChargeCompletion, rot, starOrig, new Vector2(scale2 * (Sin01(Main.GlobalTimeWrappedHourly) + .5f), scale2), 0, 0f);
            Main.spriteBatch.Draw(bloom, bloomDrawPos, null, Color.White * .5f * ChargeCompletion, rot, bloomOrig, scale2 * .75f, 0, 0f);

            Color color1 = ArrowType == -1 ? Color.DarkGoldenrod : ArrowType == 1 ? Color.DarkGoldenrod : Color.PaleGoldenrod;
            float speed = 16f;
            float finalSpeed = Main.GameUpdateCount / speed;
            float wavy = MathF.Sin(finalSpeed + (ArrowType == -1 ? 1 : ArrowType == 1 ? 2 : 3));
            float wavyPower = ArrowType == -1 ? .1f : ArrowType == 1 ? .2f : .24f;
            float prog = RangeLerp(ChargeCompletion, 0f, 1f) + wavy * wavyPower;

            float addedPos = ArrowType == -1 ? 65f : ArrowType == 1 ? 75f : 100f;
            Vector2 pos = Projectile.Center + new Vector2(0f, -(-50f + prog * addedPos)).RotatedBy(Projectile.rotation);
            float scale3 = ArrowType == -1 ? .5f : ArrowType == 1 ? .75f : 1f;
            DrawRing(Main.spriteBatch, pos, scale3, scale3, Main.GameUpdateCount / speed, prog, color1);

            Main.spriteBatch.Draw(tex, drawPos, null, col, rot, orig, scale, 0, 0f);
        }
        LayeredDrawSystem.QueueDrawAction(arrow, PixelationLayer.UnderProjectiles, BlendState.Additive);

        return false;
    }

    private void DrawRing(SpriteBatch sb, Vector2 pos, float w, float h, float rotation, float prog, Color color)
    {
        Texture2D outerCircleTexture = AssetRegistry.GetTexture(AdditionsTexture.UnfathomablePortal);

        Color startingColor = Color.Gold;
        Color endingColor = Color.DarkGoldenrod;

        ManagedShader effect = ShaderRegistry.MagicRing;
        if (effect == null)
            return;

        effect.TrySetParameter("time", rotation);
        effect.TrySetParameter("cosine", (float)Math.Cos(rotation));
        effect.TrySetParameter("firstCol", startingColor.ToVector3());
        effect.TrySetParameter("secondCol", endingColor.ToVector3());
        effect.TrySetParameter("opacity", prog);

        sb.EnterShaderRegion(BlendState.Additive, effect.Shader.Value);

        Rectangle target = ToTarget(pos, (int)(20 * (w + prog)), (int)(60 * (h + prog)));
        sb.Draw(outerCircleTexture, target, null, color * prog, Projectile.velocity.ToRotation(), outerCircleTexture.Size() / 2, 0, 0);

        sb.EnterShaderRegion(BlendState.Additive);
    }

    private static float RangeLerp(float input, float start, float end)
    {
        if (input < start)
            return 0;

        return BezierEase(InverseLerp(start, end, input));
    }
}
