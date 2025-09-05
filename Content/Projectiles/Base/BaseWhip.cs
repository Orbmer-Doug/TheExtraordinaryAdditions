using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.GameContent.Achievements;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Graphics;
using TheExtraordinaryAdditions.Core.Graphics.Primitives;
using TheExtraordinaryAdditions.Core.Graphics.Shaders;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.Projectiles.Base;

/// <summary>
/// Creates a new whip projectile <br></br>
/// The derived class of all whips in the mod <br></br>
/// Any <see cref="Projectile.ai"/> before 2 is taken
/// </summary>
public abstract class BaseWhip : ModProjectile, ILocalizedModType, IModType
{
    #region Variables/Defaults
    public const int Samples = 50;
    public const int MaxUpdates = 3;
    public virtual int TipSize => 16;
    public virtual int HandleSize => 24;
    public virtual int SegmentSkip => 1;
    public int SwingTime => Owner.itemAnimationMax * MaxUpdates;

    /// <summary>
    /// Needs to be set true for every frame to work
    /// </summary>
    internal bool OverrideWhipPoints;

    internal bool PauseTimer;

    public OptimizedPrimitiveTrail Line;
    public ManualTrailPoints WhipPoints = new(Samples);
    public Vector2 Tip;
    public Vector2 OutwardVel;

    public ref float Time => ref Projectile.ai[0];
    public ref float Completion => ref Projectile.ai[1];
    public Player Owner => Main.player[Projectile.owner];
    public GlobalPlayer Modded => Owner.Additions();

    public sealed override void SetStaticDefaults()
    {
        ProjectileID.Sets.IsAWhip[Type] = true;
        ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY[Type] = true;
        ProjectileID.Sets.CanHitPastShimmer[Type] = true;
        ProjectileID.Sets.AllowsContactDamageFromJellyfish[Type] = true;
        ProjectileID.Sets.CanDistortWater[Type] = false; // Manual
    }

    public sealed override void SetDefaults()
    {
        // This will define the ellipse
        Projectile.Size = new(500, 150);

        Projectile.hostile = false;
        Projectile.friendly = true;

        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;

        Projectile.ownerHitCheck = true;
        Projectile.penetrate = -1;
        Projectile.localNPCHitCooldown = -1;
        Projectile.usesLocalNPCImmunity = true;

        Projectile.MaxUpdates = MaxUpdates;

        Projectile.noEnchantmentVisuals = true;

        Projectile.ContinuouslyUpdateDamageStats = true;

        Projectile.netImportant = true;

        Defaults();
    }

    public virtual void Defaults() { }
    #endregion

    public sealed override void AI()
    {
        if (Time == 0f)
        {
            Projectile.velocity = Owner.Center.SafeDirectionTo(Modded.mouseWorld);
            Projectile.netUpdate = true;
        }

        OverrideWhipPoints = false;
        PauseTimer = false;

        SafeAI();
        Completion = MathF.Round(InverseLerp(0f, SwingTime, Time), 2);
        Owner.ChangeDir(Projectile.velocity.X.NonZeroSign());
        Owner.heldProj = Projectile.whoAmI;

        float rot = Projectile.velocity.ToRotation() * Owner.gravDir;
        float armReel = 3 * MathHelper.Pi / 4 * Owner.direction;

        float armRot = new Animators.PiecewiseCurve()
            .Add(rot - armReel, rot, .3f, Animators.MakePoly(4.5f).OutFunction)
            .AddStall(rot, .5f)
            .Add(rot, rot - armReel, 1f, Animators.MakePoly(2.6f).InOutFunction)
            .Evaluate(Completion);
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRot - MathHelper.PiOver2);
        Projectile.Center = Owner.GetFrontHandPositionImproved();
        
        if (!OverrideWhipPoints)
            UpdateControlPoints();
        else
            OverridePoints();

        if (Line == null || Line._disposed)
            Line = new(LineWidth, LineColor);

        if (!Main.dedServ)
            ProduceWaterRipples();

        EmitEnchantmentVisualsAt(Tip, TipSize, TipSize);

        if (Time == (SwingTime / 2))
            CrackEffects();

        if (!PauseTimer)
        {
            Time++;

            if (Time > SwingTime)
            {
                Projectile.Kill();
            }
        }
    }

    public virtual void SafeAI() { }

    public virtual void CrackEffects()
    {
        SoundID.Item153.Play(Tip);
    }

    /// <summary>
    /// Makes some garnular water ripples at the right place
    /// </summary>
    public void ProduceWaterRipples()
    {
        WaterShaderData water = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
        float power = TipSize;
        float waveSine = 1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
        Vector2 size = new(18);
        Vector2 ripplePos = Tip;
        Color waveData = new Color(power, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
        water.QueueRipple(ripplePos, waveData, size, RippleShape.Square, OutwardVel.ToRotation());
    }

    public sealed override void CutTiles()
    {
        DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(Projectile.Center, Tip, LineWidth(0f), DelegateMethods.CutTiles);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.CollisionFromPoints(WhipPoints.Points, c => TipSize);
    }

    public sealed override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 pos = ClosestOutOfList(target.Center, out int index, WhipPoints.Points);
        NPCHitEffects(target, hit, pos, pos.SafeDirectionTo(target.Center), index);
        Owner.MinionAttackTargetNPC = target.whoAmI;
    }

    public sealed override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        Vector2 pos = ClosestOutOfList(target.Center, out int index, WhipPoints.Points);
        ModifyNPCEffects(target, ref modifiers, pos, index);
    }

    public virtual void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index) { }
    public virtual void ModifyNPCEffects(NPC target, ref NPC.HitModifiers modifiers, in Vector2 pos, in int index) { }

    public virtual float GetCompletion() => Animators.MakePoly(3f).InOutFunction(Completion);
    private const float Leniance = .1f;
    public virtual float GetTheta(float t) => new Animators.PiecewiseCurve().Add(MathHelper.Pi - Leniance, 0f, .5f, c => c).Add(0f, -MathHelper.Pi + Leniance, 1f, c => c).Evaluate(t) * -Owner.direction * Owner.gravDir;
    public virtual float GetThin(float t) => new Animators.PiecewiseCurve().Add(0f, 1f, .2f, Animators.MakePoly(2f).OutFunction).AddStall(1f, .8f).Add(1f, 0f, 1f, Animators.MakePoly(2f).InFunction).Evaluate(t);
    public void UpdateControlPoints()
    {
        float width = Projectile.width;
        float height = Projectile.height;

        Vector2 center = Projectile.Center;

        float t = GetCompletion();

        // Grab the end point of the ellipse based on the completion ratio
        float theta = GetTheta(t);

        // Angle the ellipse
        float angle = Projectile.velocity.ToRotation();
        Vector2 offset = PolarVector(width / 2, angle);

        Vector2 expectedEnd = center + PolarVector(width * GetThin(t), angle);
        Vector2 expectedMid = (center + expectedEnd) / 2;

        // Calculate control points on the rotated ellipse
        Tip = center + GetPointOnRotatedEllipse(width, height, angle, theta, offset);

        List<Vector2> points = [];
        for (int i = 0; i < Samples; i++)
        {
            float completion = InverseLerp(0f, Samples, i);
            Vector2 bezier = QuadraticBezier(center, expectedMid, Tip, completion);

            points.Add(bezier);
        }

        OutwardVel = points[^2].SafeDirectionTo(points[^1]);
        WhipPoints.SetPoints(points);
    }

    public virtual void OverridePoints() { }

    public override void Load()
    {
        On_Projectile.Colliding += OverrideWhipCollision;
        On_Projectile.CutTiles += OverrideCuttingTiles;
    }
    public override void Unload()
    {
        On_Projectile.Colliding -= OverrideWhipCollision;
        On_Projectile.CutTiles -= OverrideCuttingTiles;
    }

    private void OverrideCuttingTiles(On_Projectile.orig_CutTiles orig, Projectile self)
    {
        if (ProjectileID.Sets.IsAWhip[self.type] && (self.ModProjectile as BaseWhip) != null)
        {
            AchievementsHelper.CurrentlyMining = true;

            BaseWhip whip = self.ModProjectile as BaseWhip; 
            bool[] tileCutIgnorance = whip.Owner.GetTileCutIgnorance(allowRegrowth: false, self.trap);
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = tileCutIgnorance;
            Utils.PlotTileLine(self.Center, whip.Tip, whip.LineWidth(0f), DelegateMethods.CutTiles);

            AchievementsHelper.CurrentlyMining = false;
            return;
        }

        orig.Invoke(self);
    }

    private bool OverrideWhipCollision(On_Projectile.orig_Colliding orig, Projectile self, Rectangle myRect, Rectangle targetRect)
    {
        if (ProjectileID.Sets.IsAWhip[self.type] && (self.ModProjectile as BaseWhip) != null)
        {
            BaseWhip whip = self.ModProjectile as BaseWhip;
            return targetRect.CollisionFromPoints(whip.WhipPoints.Points, c => whip.TipSize);
        }

        return orig.Invoke(self, myRect, targetRect);
    }

    public virtual float LineWidth(float completion)
    {
        return 2f;
    }

    public virtual Color LineColor(SystemVector2 completion, Vector2 position)
    {
        Point p = position.ToTileCoordinates();
        return Color.White * Lighting.Brightness(p.X, p.Y);
    }

    public virtual void DrawLine()
    {
        if (Line != null && !Line._disposed)
        {
            ManagedShader shader = ShaderRegistry.StandardPrimitiveShader;
            Line.DrawTrail(shader, WhipPoints.Points);
        }
    }

    public abstract void DrawSegments();

    public sealed override bool PreDraw(ref Color lightColor)
    {
        if (Time.BetweenNum(10f, SwingTime - 10f))
            PixelationSystem.QueuePrimitiveRenderAction(DrawLine, PixelationLayer.UnderProjectiles);
        DrawSegments();

        return false;
    }
}

public class ExampleWhipItem : ModItem
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.Atorcoppe);

    public override void SetDefaults()
    {
        // normal item thingymajigs
        Item.damage = 50;
        Item.width = Item.height = 4;
        Item.useTime = Item.useAnimation = 42;
        Item.UseSound = SoundID.Item152;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.rare = ItemRarityID.Blue;
        Item.value = AdditionsGlobalItem.RarityBlueBuyPrice;
        Item.DamageType = DamageClass.SummonMeleeSpeed;
        Item.shoot = ModContent.ProjectileType<ExampleWhip>();
        Item.shootSpeed = 1f;
        Item.knockBack = 5f;
        Item.noMelee = Item.noUseGraphic = true;
    }

    public override bool CanShoot(Player player)
    {
        return player.ownedProjectileCounts[Item.shoot] <= 0;
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        player.NewPlayerProj(position, velocity, type, damage, knockback, player.whoAmI);
        return false;
    }
}

public class ExampleWhip : BaseWhip
{
    public override string Texture => AssetRegistry.GetTexturePath(AdditionsTexture.SpiderWhipProjectile);
    public override int SegmentSkip => 2;
    public override void SafeAI()
    {
        Visuals();
    }

    public override void CrackEffects()
    {
        float rot = RandomRotation();
        for (int i = 0; i < 8; i++)
            ParticleRegistry.SpawnSparkParticle(Tip, (MathHelper.TwoPi * i / 8 + rot).ToRotationVector2() * 7f, 20, .6f, Color.AntiqueWhite);
        ParticleRegistry.SpawnPulseRingParticle(Tip, Vector2.Zero, 20, 0f, Vector2.One, 0f, 50f, Color.White);
        SoundID.Item153.Play(Tip, 1f, .14f);
    }

    public override void NPCHitEffects(NPC target, NPC.HitInfo hit, in Vector2 pos, in Vector2 vel, in int index)
    {
        ParticleRegistry.SpawnSparkleParticle(pos, Vector2.Zero, 20, .4f, Color.White, Color.AntiqueWhite, .8f, .12f);
        for (int i = 0; i < 12; i++)
        {
            ParticleRegistry.SpawnDustParticle(pos, vel.RotatedByRandom(.25f) * Main.rand.NextFloat(2f, 8f), Main.rand.Next(30, 50), Main.rand.NextFloat(.4f, .8f), Color.AntiqueWhite);
        }

        Projectile.damage = (int)(Projectile.damage * .8f);
    }

    public override void ModifyNPCEffects(NPC target, ref NPC.HitModifiers modifiers, in Vector2 pos, in int index)
    {
        if (index == (WhipPoints.Count - 1))
            modifiers.SetCrit();
    }

    public void Visuals()
    {
        Projectile.scale = MathHelper.Lerp(.9f, 1.5f, GetLerpBump(0f, .4f, 1f, .6f, Completion)) * GetThin(GetCompletion());
        if (Completion.BetweenNum(.2f, .8f) && Main.rand.NextBool(2))
        {
            Vector2 vel = OutwardVel.RotatedByRandom(.1f) * 4f;
            int life = Main.rand.Next(30, 50);
            float scale = Main.rand.NextFloat(.5f, .9f);
            Color col = Color.AntiqueWhite;
            ParticleRegistry.SpawnDustParticle(Tip, vel, life, scale, col, .1f);
        }
    }

    public override void DrawSegments()
    {
        Texture2D texture = Projectile.ThisProjectileTexture();

        Rectangle hiltFrame = new(0, 0, 10, 24);
        Rectangle seg1Frame = new(0, 24, 10, 18);
        Rectangle seg2Frame = new(0, 42, 10, 16);
        Rectangle seg3Frame = new(0, 58, 10, 16);
        Rectangle tipFrame = new(0, 74, 10, 18);

        int len = WhipPoints.Points.Length - 1;
        for (int i = 0; i < len; i++)
        {
            Vector2 pos = WhipPoints.Points[i];
            Vector2 next = WhipPoints.Points[i + 1];

            Rectangle frame;
            bool hilt = i == 0;
            bool tip = i == len - 1;
            if (hilt)
                frame = hiltFrame;
            else if (i < (len / 3))
                frame = seg1Frame;
            else if (i < (len / 2))
                frame = seg2Frame;
            else if (i < (len - 1))
                frame = seg3Frame;
            else
                frame = tipFrame;

            Vector3 light = Lighting.GetSubLight(pos);
            Color color = Projectile.GetAlpha(new(light.X, light.Y, light.Z));
            float rotation = ((next - pos).ToRotation()) - MathHelper.PiOver2;
            Vector2 orig = frame.Size() / 2;
            SpriteEffects flip = Owner.direction < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            Main.spriteBatch.DrawBetter(texture, pos, frame, color, rotation, orig, tip ? Projectile.scale : 1f, flip);
        }
    }
}