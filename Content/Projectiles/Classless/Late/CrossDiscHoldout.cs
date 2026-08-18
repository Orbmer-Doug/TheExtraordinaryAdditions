using System;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.Items.Weapons.Classless;
using TheExtraordinaryAdditions.Content.Projectiles.Base;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Globals.PlayerGlobal;
using TheExtraordinaryAdditions.Core.Graphics.Meshes;
using TheExtraordinaryAdditions.Core.Graphics.Resources;
using TheExtraordinaryAdditions.Core.Graphics.Systems;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;
using TheExtraordinaryAdditions.UI.CrossUI;
using static TheExtraordinaryAdditions.Content.Projectiles.Classless.Late.CrossDiscHoldout;

namespace TheExtraordinaryAdditions.Content.Projectiles.Classless.Late;

public sealed class CrossDiscPlayer : ModPlayer
{
    public Element Element;

    public bool DiscHeld => Player.HeldItem.ModItem is CrossDisc;

    public override void ResetEffects()
    {
        if (!DiscHeld)
            Element = Element.Neutral;
    }

    public override void PostUpdateEquips()
    {
        if (Element == Element.Cold)
            Player.statDefense += 10;
    }

    public override void PostUpdateMiscEffects()
    {
        if (DiscHeld && Player.AdditionsMouse().SafeMouseLeft.Current)
            Player.moveSpeed *= .7f;
    }

    public override void PostUpdateRunSpeeds()
    {
        if (Element == Element.Shock)
            Player.runAcceleration *= 2f;
    }

    public override void NaturalLifeRegen(ref float regen)
    {
        if (Element == Element.Heat)
            Player.lifeRegenTime += 200;
    }

    public override void UpdateLifeRegen()
    {
        if (Element == Element.Wave)
            Player.lifeRegen += 3;
    }
}

public class CrossDiscHoldout : BaseIdleHoldoutProjectile
{
    #region Defaults

    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public override int IntendedProjectileType => ModContent.ProjectileType<CrossDiscHoldout>();
    public override int AssociatedItemID => ModContent.ItemType<CrossDisc>();

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void Defaults()
    {
        Projectile.width = Projectile.height = 1;
    }

    public override void Load()
    {
        Main.QueueMainThreadAction(() => { On_Main.DrawInterface_36_Cursor += ChangeCursor; });
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() => { On_Main.DrawInterface_36_Cursor -= ChangeCursor; });
    }

    private static void ChangeCursor(On_Main.orig_DrawInterface_36_Cursor orig)
    {
        if (FindProjectile(out Projectile p, ModContent.ProjectileType<CrossDiscHoldout>(), Main.myPlayer))
        {
            if (p.owner == Main.myPlayer)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.SamplerStateForCursor,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.UIScaleMatrix);

                Texture2D tex = AssetRegistry.GennedTextures.CursorMelee;
                PlayerMouse player = Main.LocalPlayer.AdditionsMouse();
                if (player.SafeMouseLeft.Current &&
                    Main.LocalPlayer.ownedProjectileCounts[ModContent.ProjectileType<CrossSwing>()] <= 0)
                    tex = AssetRegistry.GennedTextures.CursorRanged;

                Main.spriteBatch.Draw(tex, new Vector2(Main.mouseX + 1, Main.mouseY + 1), null, Color.White, 0f,
                    new Vector2(.5f) * tex.Size(), Main.cursorScale * 1.1f, SpriteEffects.None, 0f);
            }
        }
        else
            orig();
    }

    #endregion Defaults

    #region Definitions

    /// <summary>
    /// Describes cross disc elements
    /// </summary>
    [Flags]
    public enum Element
    {
        Neutral = 0,
        Cold = 1,
        Heat = 2,
        Shock = 3,
        Wave = 4,
    }

    /// <summary>
    /// The current elemental mode the cross disc
    /// </summary>
    public Element State
    {
        get => (Element) Projectile.ai[0];
        set => Projectile.ai[0] = (float) value;
    }

    /// <summary>
    /// A small cooldown between melee swings
    /// </summary>
    public ref float SwingCooldown => ref Projectile.ai[1];

    /// <summary>
    /// The counter for the uncharged bolls
    /// </summary>
    public ref float ReticleCounter => ref Projectile.ai[2];

    /// <summary>
    /// The extra counter for a charged boll
    /// </summary>
    public ref float FullReticleCounter => ref Projectile.AdditionsInfo().ExtraAI[0];

    /// <summary>
    /// How much balance this attack is going to use
    /// </summary>
    public ref float ElementalAmount => ref Projectile.AdditionsInfo().ExtraAI[1];

    /// <summary>
    /// A small cooldown between shooting any boll
    /// </summary>
    public ref float BallCooldown => ref Projectile.AdditionsInfo().ExtraAI[2];

    /// <summary>
    /// A small cooldown before the reticle disappears
    /// </summary>
    public ref float ReticleWait => ref Projectile.AdditionsInfo().ExtraAI[3];

    public ElementalBalance ElementPlayer => Owner.GetModPlayer<ElementalBalance>();
    public const int BigBollCooldown = 20;
    public const int BollCooldown = 15;

    #endregion Definitions

    #region AI

    public override void SafeAI()
    {
        if (Item.ModItem is not CrossDisc || Item.type != ModContent.ItemType<CrossDisc>() || Owner.dead ||
            !Owner.active)
        {
            Projectile.Kill();
            return;
        }

        if (this.RunLocal())
        {
            Projectile.velocity = Projectile.SafeDirectionTo(Modded.MouseWorld).SafeNormalize(Vector2.Zero) * 5f;
            if (Projectile.velocity != Projectile.oldVelocity)
                Projectile.netUpdate = true;
        }

        if (SwingCooldown > 0f)
            SwingCooldown--;

        #region Held

        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.owner = Owner.whoAmI;
        Owner.ChangeDir((Projectile.velocity.X > 0f).ToDirectionInt());
        Projectile.Center = Center;
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, 0f);

        #endregion Held

        ElementalBalanceUI.visible = true;

        // For simplification in the ui
        int mode = (int) Projectile.ai[0];

        #region Idle Effects

        Owner.GetModPlayer<CrossDiscPlayer>().Element = State;

        // this adds in the overload on the circuit if used too much
        if (ElementPlayer.ElementCompletion >= 1f && State != Element.Neutral)
        {
            ElementalBalance.OverloadSound.Play(Owner.Center, 1.5f, -.2f);
            State = Element.Neutral;
            ElementPlayer.CircuitOverload = SecondsToFrames(30);
            this.Sync();
        }

        switch (State)
        {
            case Element.Neutral:
                Projectile.damage = 3600;
                ElementalAmount -= 2;
                if (Owner.AdditionsMisc().GlobalTimer % 3 == 2)
                    ElementPlayer.ElementalResourceCurrent -= 1;
                break;
            case Element.Cold:
                Projectile.damage = 3600;
                ElementalAmount = 7;
                break;
            case Element.Heat:
                Projectile.damage = 4250;
                ElementalAmount = 7;
                break;
            case Element.Shock:
                Projectile.damage = 5500;
                ElementalAmount = 3;
                break;
            case Element.Wave:
                Projectile.damage = 4500;
                Projectile.knockBack = 10f;
                ElementalAmount = 7;
                break;
        }

        #endregion Idle Effects

        #region Shoot Effects

        int swingType = ModContent.ProjectileType<CrossSwing>();
        if (Modded.SafeMouseRight.JustPressed && Owner.ownedProjectileCounts[swingType] <= 0 && this.RunLocal())
        {
            Vector2 velocity = Projectile.SafeDirectionTo(Modded.MouseWorld);

            // Make the swing
            Projectile swing = Main.projectile[Projectile.NewProj(Center, velocity, swingType,
                Projectile.damage, Projectile.knockBack, Projectile.owner)];
            swing.AdditionsInfo().ExtraAI[6] = (float) BaseSwordSwing.SwingDirection.Up;
            swing.AdditionsInfo().ExtraAI[7] = (float) State;
            swing.netUpdate = true;

            SwingCooldown = Item.useTime;
            this.Sync();
        }

        #endregion Shoot Effects

        // Handle Virtual Ricochet Projectile behaviors
        if (Owner.ownedProjectileCounts[swingType] <= 0)
            VRPBehavior();
    }

    public static readonly float MaxCharge = SecondsToFrames(.9f);
    public float ReticleProgress => InverseLerp(0f, MaxCharge, ReticleCounter);
    public float FullReticleProgress => InverseLerp(BigBollCooldown, BigBollCooldown * 2, FullReticleCounter);
    public float Spread => MathHelper.PiOver4 * (1f - ReticleProgress);

    private void VRPBehavior()
    {
        // Apply a decrease in accuracy the faster the cursor is spinning
        ReticleCounter =
            MathHelper.Clamp(
                ReticleCounter - (MathF.Abs(MathHelper.WrapAngle(Projectile.oldRot[0] - Projectile.oldRot[1])) * 5f),
                0f, MaxCharge);

        if (this.RunLocal() && Modded.SafeMouseLeft.Current)
        {
            Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);

            if (ReticleCounter < MaxCharge)
                ReticleCounter++;
            if (ReticleProgress >= 1f)
            {
                FullReticleCounter++;
            }
            else
                FullReticleCounter = 0f;

            ReticleWait = 30f;
            this.Sync();
        }
        else if (this.RunLocal() && !Modded.SafeMouseLeft.Current)
        {
            if (ReticleWait > 0f)
            {
                ReticleCounter += .5f;
                ReticleWait--;
            }

            if (ReticleWait <= 0f)
                ReticleCounter = 0f;
            this.Sync();
        }

        if (BallCooldown > 0f)
            BallCooldown--;

        if (this.RunLocal() && Modded.SafeMouseLeft.JustReleased)
        {
            Vector2 pos = Center;
            Vector2 vel = Center.SafeDirectionTo(Modded.MouseWorld)
                .RotatedByRandom(Spread) * 5f;

            int type = ModContent.ProjectileType<VRP>();
            int damage = Projectile.damage;
            float kB = 0f;
            int own = Projectile.owner;
            float state = Projectile.ai[0];
            float progress = ReticleProgress;

            Projectile vrp = Main.projectile[Projectile.NewProj(pos, vel, type, damage, kB, own, state, progress)];
            vrp.AdditionsInfo().ExtraAI[2] = (FullReticleProgress >= 1f).ToInt();
            vrp.netUpdate = true;

            SoundStyle shoot = new();
            switch (State)
            {
                case Element.Neutral:
                    shoot = AssetRegistry.GennedSounds.NeutralBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AssetRegistry.GennedSounds.NeutralBallThrowCharged;
                    break;
                case Element.Cold:
                    shoot = AssetRegistry.GennedSounds.ColdBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AssetRegistry.GennedSounds.ColdBallThrowCharged;
                    break;
                case Element.Heat:
                    shoot = AssetRegistry.GennedSounds.HeatBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AssetRegistry.GennedSounds.HeatBallThrowCharged;
                    break;
                case Element.Shock:
                    shoot = AssetRegistry.GennedSounds.ShockBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AssetRegistry.GennedSounds.ShockBallThrowCharged;
                    break;
                case Element.Wave:
                    shoot = AssetRegistry.GennedSounds.WaveBallThrow;
                    if (FullReticleProgress >= 1f)
                        shoot = AssetRegistry.GennedSounds.WaveBallThrowCharged;
                    break;
            }

            shoot.Play(Projectile.Center, 1f, 0f, .2f, 20, Name);

            switch (State)
            {
                case Element.Neutral:
                    break;
                case Element.Cold:
                    ElementPlayer.ElementalResourceCurrent += 3;
                    break;
                case Element.Heat:
                    ElementPlayer.ElementalResourceCurrent += 4;
                    break;
                case Element.Shock:
                    ElementPlayer.ElementalResourceCurrent += 2;
                    break;
                case Element.Wave:
                    ElementPlayer.ElementalResourceCurrent += 3;
                    break;
                default:
                    break;
            }

            FullReticleCounter = 0f;

            BallCooldown = BollCooldown;
            this.Sync();
        }
    }

    public override void OnKill(int timeLeft)
    {
        ElementalBalanceUI.visible = false;
    }

    #endregion AI

    #region Drawing

    public static Texture2D normalReticle => AssetRegistry.GennedTextures.Reticle1;
    public static Texture2D chargedReticle => AssetRegistry.GennedTextures.Reticle2;

    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 screenPos = Main.screenPosition;
        if (this.RunLocal() && (Modded.SafeMouseLeft.Current || ReticleWait > 0f) &&
            Owner.ownedProjectileCounts[ModContent.ProjectileType<CrossSwing>()] <= 0)
        {
            float opacity = ReticleWait < 29f ? .3f : 1f;
            int frame = (int) (FullReticleProgress * 3f);
            Rectangle dotFrame = normalReticle.Frame(1, 4, 0, frame);

            /*
            if (FullReticleProgress >= 1f)
            {
                for (int i = 1; i < PredictionLine.PathPoints.Count; i++)
                {
                    Vector2 pos = PredictionLine.PathPoints[i];
                    Main.spriteBatch.Draw(normalReticle, pos - screenPos, dotFrame, Color.White * opacity, 0f, dotFrame.Size() / 2, 1f, 0, 0f);
                }
            }
            else*/
            {
                int maxDist = FullReticleProgress >= 1f ? 1000 : 500;
                for (int i = 0; i < maxDist; i += 100)
                {
                    Vector2 pos = Projectile.Center +
                                  Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(Spread) * i;
                    Main.spriteBatch.Draw(normalReticle, pos - screenPos, dotFrame, Color.White * opacity, 0f,
                        dotFrame.Size() / 2, 1f, 0, 0f);
                    pos = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.Zero).RotatedBy(-Spread) * i;
                    Main.spriteBatch.Draw(normalReticle, pos - screenPos, dotFrame, Color.White * opacity, 0f,
                        dotFrame.Size() / 2, 1f, 0, 0f);
                }
            }

            Rectangle frame2 = chargedReticle.Frame(1, 4, 0, frame);
            Vector2 orig2 = frame2.Size() * .5f;
            Main.EntitySpriteDraw(chargedReticle, Modded.MouseWorld - screenPos, frame2, Color.White * opacity, 0f,
                orig2, 1f, 0);
        }

        return false;
    }

    #endregion Drawing
}

public class VRP : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.CrossCodeBoll.Path;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
        ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 1000;

        Main.projFrames[Projectile.type] = 4;
    }

    public override void SetDefaults()
    {
        Projectile.timeLeft = 1200;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.friendly = Projectile.ignoreWater = Projectile.noEnchantmentVisuals =
            Projectile.usesLocalNPCImmunity = Projectile.tileCollide = true;
        Projectile.hostile = false;
        Projectile.aiStyle = 0;
        Projectile.CritChance = 0;
        Projectile.MaxUpdates = 4;
        Projectile.localNPCHitCooldown = 20;
        Projectile.width = Projectile.height = 1;
    }

    private Element State
    {
        get => (Element) Projectile.ai[0];
        set => Projectile.ai[0] = (float) value;
    }

    public ref float Completion => ref Projectile.ai[1];

    public int Bounces
    {
        get => (int) Projectile.ai[2];
        set => Projectile.ai[2] = value;
    }

    public ref float Time => ref Projectile.AdditionsInfo().ExtraAI[0];

    public bool TileDeath
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[1] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[1] = value.ToInt();
    }

    public bool Charged
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[2] == 1;
        set => Projectile.AdditionsInfo().ExtraAI[2] = value.ToInt();
    }

    public int MaxBounces => Charged ? 4 : 1;

    public Player Owner => Main.player[Projectile.owner];

    public override void AI()
    {
        if (Time == 0)
            this.Sync();
        if (State == Element.Neutral)
        {
            Texture2D bigNeutral = AssetRegistry.GennedTextures.VRPNeutral;
            after ??= new(5, () => Projectile.Center);
            after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation,
                0, 0, 0, 0, bigNeutral.Frame(1, 4, 0, Projectile.frame)));
        }

        Projectile.FacingUp();

        if (Charged && Projectile.FinalExtraUpdate())
        {
            Projectile.SetAnimation(4, 7);

            if (Time % 3 == 2)
            {
                ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, Projectile.rotation,
                    ParticleRegistry.CrosscodeBollType.Trail, State);
            }
        }

        Time++;
    }

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough,
        ref Vector2 hitboxCenterFrac)
    {
        fallThrough = true;
        return true;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (Bounces >= MaxBounces || !Charged)
        {
            ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center,
                ClampToCardinalDirection(oldVelocity).ToRotation() + MathHelper.PiOver2,
                ParticleRegistry.CrosscodeBollType.DieWallBig, State);
            TileDeath = true;
            Projectile.Kill();
            return false;
        }

        Bounces++;
        ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center,
            ClampToCardinalDirection(oldVelocity).ToRotation() + MathHelper.PiOver2,
            ParticleRegistry.CrosscodeBollType.DieWallSmall, State);
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;

        Projectile.damage += (int) (Projectile.damage * .25);
        Projectile.CritChance = (int) (InverseLerp(0f, Bounces, MaxBounces) * 100);

        switch (State)
        {
            case Element.Neutral:
                AssetRegistry.GennedSounds.NeutralBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Cold:
                AssetRegistry.GennedSounds.ColdBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Heat:
                AssetRegistry.GennedSounds.HeatBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Shock:
                AssetRegistry.GennedSounds.ShockBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
            case Element.Wave:
                AssetRegistry.GennedSounds.WaveBounce.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
                break;
        }

        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Charged)
        {
            switch (State)
            {
                case Element.Neutral:
                    AssetRegistry.GennedSounds.NeutralBallHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Cold:
                    AssetRegistry.GennedSounds.ColdHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Heat:
                    AssetRegistry.GennedSounds.HeatHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Shock:
                    AssetRegistry.GennedSounds.ShockHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Wave:
                    AssetRegistry.GennedSounds.WaveHitBig.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
            }

            ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Big, State);
        }

        else if (Completion <= .5f)
        {
            switch (State)
            {
                case Element.Neutral:
                    AssetRegistry.GennedSounds.NeutralBallHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Cold:
                    AssetRegistry.GennedSounds.ColdHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Heat:
                    AssetRegistry.GennedSounds.HeatHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Shock:
                    AssetRegistry.GennedSounds.ShockHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Wave:
                    AssetRegistry.GennedSounds.WaveHitSmall.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
            }

            ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Small, State);
        }

        else if (Completion <= 1f)
        {
            switch (State)
            {
                case Element.Neutral:
                    AssetRegistry.GennedSounds.NeutralBallHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Cold:
                    AssetRegistry.GennedSounds.ColdHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Heat:
                    AssetRegistry.GennedSounds.HeatHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Shock:
                    AssetRegistry.GennedSounds.ShockHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
                case Element.Wave:
                    AssetRegistry.GennedSounds.WaveHitMedium.Play(Projectile.Center, 1f, 0f, 0f, 20, Name);
                    break;
            }

            ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Medium, State);
        }

        switch (State)
        {
            case Element.Neutral:
                break;
            case Element.Cold:
                target.AddBuff(BuffID.Frostburn, SecondsToFrames(3));
                target.AddBuff(BuffID.Frostburn2, SecondsToFrames(3));
                break;
            case Element.Heat:
                target.AddBuff(BuffID.OnFire, SecondsToFrames(3));
                target.AddBuff(BuffID.OnFire3, SecondsToFrames(3));
                break;
            case Element.Shock:
            case Element.Wave:
                break;
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Charged)
            modifiers.FinalDamage *= 1.5f;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.numHits <= 0)
        {
            if (!TileDeath)
                ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, 0f, ParticleRegistry.CrosscodeBollType.Die,
                    State);
            AssetRegistry.GennedSounds.crosscodeBallDie.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
        }
    }

    private static readonly Texture2D Bloom = AssetRegistry.GennedTextures.GlowParticleSmall;
    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D bigNeutral = AssetRegistry.GennedTextures.VRPNeutral;
        Texture2D bigIce = AssetRegistry.GennedTextures.VRPIce;
        Texture2D bigFire = AssetRegistry.GennedTextures.VRPFire;
        Texture2D bigShock = AssetRegistry.GennedTextures.VRPLightning;
        Texture2D bigWave = AssetRegistry.GennedTextures.VRPWave;
        Texture2D smoll = AssetRegistry.GennedTextures.SmolBoll;

        Vector2 drawPos = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        float rot = Projectile.rotation;
        float sca = Projectile.scale;
        Color col = Projectile.GetAlpha(Color.White);

        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Main.EntitySpriteDraw(Bloom, Projectile.Center - Main.screenPosition, null, Color.White * .5f, 0f,
            Bloom.Size() * .5f, .3f, 0);
        Main.spriteBatch.ResetToDefault();

        if (!Charged)
        {
            int smolFrame = 0;
            switch (State)
            {
                case Element.Neutral:
                    smolFrame = 0;
                    break;
                case Element.Cold:
                    smolFrame = 2;
                    break;
                case Element.Heat:
                    smolFrame = 1;
                    break;
                case Element.Shock:
                    smolFrame = 3;
                    break;
                case Element.Wave:
                    smolFrame = 4;
                    break;
            }

            Rectangle framed = smoll.Frame(1, 5, 0, smolFrame);
            Vector2 orig1 = framed.Size() * .5f;
            Main.EntitySpriteDraw(smoll, drawPos, framed, col, rot, orig1, sca, direction);
        }

        if (Charged)
        {
            Texture2D tex = State switch
            {
                Element.Neutral => bigNeutral,
                Element.Cold => bigIce,
                Element.Heat => bigFire,
                Element.Shock => bigShock,
                Element.Wave => bigWave,
                _ => bigNeutral
            };

            Rectangle framed = tex.Frame(1, 4, 0, Projectile.frame);
            Vector2 orig1 = framed.Size() * .5f;
            after?.DrawFancyAfterimages(tex, [col * .2f], Projectile.Opacity);
            Main.EntitySpriteDraw(tex, drawPos, framed, col, rot, orig1, sca, direction);
        }

        return false;
    }
}

public class CrossSwing : BaseSwordSwing
{
    public override string Texture => AssetRegistry.GennedTextures.Invisible.Path;
    public ElementalBalance ElementPlayer => Owner.GetModPlayer<ElementalBalance>();

    public Element State
    {
        get => (Element) Projectile.AdditionsInfo().ExtraAI[7];
        set => Projectile.AdditionsInfo().ExtraAI[7] = (float) value;
    }

    public int SwingCounter
    {
        get => (int) Projectile.AdditionsInfo().ExtraAI[8];
        set => Projectile.AdditionsInfo().ExtraAI[8] = value;
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
        return Spin
            ? Expo(2.5f).OutFunction.Evaluate(Time, 0f, MaxTime, -1f, 5f)
            : Expo().OutFunction.Evaluate(Time, 0f, MaxTime, -1f, 1f);
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
                        Vector2 newVelocity = Center.SafeDirectionTo(Modded.MouseWorld)
                            .RotatedByRandom(MathHelper.ToRadians(12)) * 12f;

                        newVelocity *= 1f - Main.rand.NextFloat(0.3f);

                        Projectile.NewProj(Center, newVelocity, ModContent.ProjectileType<BouncyIcicle>(),
                            Projectile.damage / 3, Projectile.knockBack / 9, Projectile.owner);
                    }

                    break;
                case Element.Heat:
                    Vector2 target = ClosestPointOnLineSegment(Modded.MouseWorld,
                        Center - Vector2.UnitX * Main.LogicCheckScreenWidth / 2,
                        Center + Vector2.UnitX * Main.LogicCheckScreenWidth / 2);
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 pos = target - new Vector2(Main.rand.NextFloat(-40f, 40f), 800f);
                        pos.Y -= 200 * i;
                        Vector2 vel = Vector2.UnitY.RotatedByRandom(.3f) * Main.rand.NextFloat(4f, 10f);
                        Projectile.NewProj(pos, vel, ModContent.ProjectileType<ScarletMeteor>(), Projectile.damage,
                            Projectile.knockBack / 2, Owner.whoAmI);
                    }

                    break;
                case Element.Shock:

                    break;
                case Element.Wave:
                    for (int i = 0; i < 4; i++)
                    {
                        Projectile.NewProj(Center,
                            Center.SafeDirectionTo(Modded.MouseWorld).RotatedByRandom(.6f) *
                            Main.rand.NextFloat(10f, 12f),
                            ModContent.ProjectileType<WaveSiphon>(), Projectile.damage / 5, 0f, Projectile.owner);
                    }

                    break;
            }
        }

        ElementPlayer.ElementalResourceCurrent += 8;
    }

    public override void SafeAI()
    {
        if (trail == null || trail.Disposed)
            trail = new(WidthFunct, ColorFunct, _ => Center.ToNumerics(), 20);

        Owner.ChangeDir(Direction);

        Projectile.rotation = SwingOffset();
        Owner.SetFrontHandBetter(Player.CompositeArmStretchAmount.Full, Projectile.rotation);
        Projectile.Center = Center + PolarVector(90f, Projectile.rotation);
        points.Update(Projectile.Center - Center);

        if (SwingCompletion >= .2f && !PlayedSound)
        {
            SoundStyle sound = new();
            switch (State)
            {
                case Element.Neutral:
                    sound = AssetRegistry.GennedSounds.NeutralSweep;
                    if (Spin)
                        sound = AssetRegistry.GennedSounds.NeutralSweepMassive;
                    break;
                case Element.Cold:
                    sound = AssetRegistry.GennedSounds.ColdSweep;
                    if (Spin)
                        sound = AssetRegistry.GennedSounds.ColdSweepMassive;
                    break;
                case Element.Heat:
                    sound = AssetRegistry.GennedSounds.HeatSweep;
                    if (Spin)
                        sound = AssetRegistry.GennedSounds.HeatSweepMassive;
                    break;
                case Element.Shock:
                    sound = AssetRegistry.GennedSounds.ShockSweep;
                    if (Spin)
                        sound = AssetRegistry.GennedSounds.ShockSweepMassive;
                    break;
                case Element.Wave:
                    sound = AssetRegistry.GennedSounds.WaveSweep;
                    if (Spin)
                        sound = AssetRegistry.GennedSounds.WaveSweepMassive;
                    break;
            }

            sound.Play(Projectile.Center, 1.2f, 0f, .2f, 10, Name);

            PlayedSound = true;
        }

        if (VanishTime <= 0)
        {
            Projectile.scale = MakePoly(3f).OutFunction(InverseLerp(0f, 10f * MaxUpdates, OverallTime));
        }
        else
        {
            Projectile.scale = MakePoly(3f).OutFunction.Evaluate(VanishTime, 0f, 18f * MaxUpdates, 1f, 0f);
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
            }
            else
            {
                VanishTime++;
            }

            this.Sync();
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
                AssetRegistry.GennedSounds.NeutralHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
            case Element.Cold:
                AssetRegistry.GennedSounds.ColdHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
            case Element.Heat:
                AssetRegistry.GennedSounds.HeatHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
                break;
            case Element.Shock:
                AssetRegistry.GennedSounds.ShockHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);

                if (this.RunLocal())
                {
                    ShockLightning shock = Main.projectile[Projectile.NewProj(npc.Center - Vector2.UnitY * 800f,
                            Vector2.Zero,
                            ModContent.ProjectileType<ShockLightning>(), Projectile.damage / 2, 0f, Projectile.owner)]
                        .As<ShockLightning>();
                    shock.End = npc.Center;
                    shock.Sync();
                }

                break;
            case Element.Wave:
                AssetRegistry.GennedSounds.WaveHitMedium.Play(Projectile.Center, 1.1f, 0f, .2f);
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
    public Trail trail;

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (points == null || trail == null || Time < 10f)
                return;

            ManagedShader shader = AssetRegistry.GennedShaders.CrossDiscSwing;
            bool flip = SwingDir != SwingDirection.Up;
            if (Direction == -1)
                flip = SwingDir == SwingDirection.Up;
            shader.TrySetParameter("flip", flip);

            Texture2D noise;

            switch (State)
            {
                case Element.Neutral:
                    noise = AssetRegistry.GennedTextures.TechyNoise;
                    break;
                case Element.Cold:
                    noise = AssetRegistry.GennedTextures.CrackedNoise;
                    break;
                case Element.Heat:
                    noise = AssetRegistry.GennedTextures.HarshNoise;
                    break;
                case Element.Shock:
                    noise = AssetRegistry.GennedTextures.NeuronNoise;
                    break;
                case Element.Wave:
                    noise = AssetRegistry.GennedTextures.WavyBlotchNoise;
                    break;
                default:
                    noise = AssetRegistry.GennedTextures.noise;
                    break;
            }

            shader.SetTexture(noise, 0, SamplerState.LinearWrap);

            trail.DrawTrail(shader, points.Points, 100, true);
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        return false;
    }
}

public class BouncyIcicle : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.DiscIceProjectile.Path;

    public override void SetDefaults()
    {
        Projectile.width = 42;
        Projectile.height = 30;
        Projectile.timeLeft = 1200;
        Projectile.penetrate = 2;
        Projectile.friendly = true;
        Projectile.hostile = false;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 20;
        Projectile.extraUpdates = 0;
        Projectile.active = true;
        Projectile.noEnchantmentVisuals = true;
        Projectile.reflected = true;
        Projectile.scale = 1f;
        Projectile.aiStyle = 0;
    }

    public override void AI()
    {
        after ??= new(5, () => Projectile.Center);
        after?.UpdateFancyAfterimages(new(Projectile.Center, Vector2.One, Projectile.Opacity, Projectile.rotation, 0,
            255));

        Lighting.AddLight(Projectile.Center, Color.DarkBlue.ToVector3() * .6f);

        if (Projectile.velocity.Length() < 30f)
            Projectile.velocity *= 1.015f;

        if ((int) Projectile.ai[0]++ % 2 == 1)
            ParticleRegistry.SpawnMistParticle(Projectile.Center, Projectile.velocity * Main.rand.NextFloat(.2f, .5f),
                Main.rand.NextFloat(.4f, .8f), Color.DarkBlue, Color.DarkSlateBlue, 190);

        Projectile.FacingRight();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AssetRegistry.GennedSounds.ColdHitBig.Play(Projectile.Center, .5f, 0f, .1f, 10);
        Projectile.Kill();
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.penetrate--;
        AssetRegistry.GennedSounds.ColdBounce.Play(Projectile.Center, 1.1f, 0f, .2f, 10);

        for (int i = 0; i < 10; i++)
        {
            Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.IceGolem,
                0f, 0f, 100, default, 2f);
            dust.noGravity = true;
            dust.velocity *= 3f;
        }

        // Bouncy
        if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > float.Epsilon)
            Projectile.velocity.X = -oldVelocity.X;
        if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > float.Epsilon)
            Projectile.velocity.Y = -oldVelocity.Y;
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width,
            Projectile.height);
        AssetRegistry.GennedSounds.ColdBallThrow.Play(Projectile.Center, .4f, 0f, .2f, 10);

        for (int i = 0; i < 20; i++)
            ParticleRegistry.SpawnDustParticle(Projectile.BaseRotHitbox().Right,
                -Projectile.velocity.RotatedByRandom(.4f) * Main.rand.NextFloat(.2f, .3f),
                Main.rand.Next(30, 40), Main.rand.NextFloat(.4f, .8f), Color.DarkSlateBlue, .1f, false, true);
    }

    public FancyAfterimages after;

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Projectile.ThisProjectileTexture();
        after?.DrawFancyAfterimages(Projectile.ThisProjectileTexture(), [Color.LightCyan], Projectile.Opacity);

        Vector2 drawPosition = Projectile.Center - Main.screenPosition;
        SpriteEffects direction = Projectile.spriteDirection == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
        Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation,
            texture.Size() * 0.5f, Projectile.scale, direction);
        return false;
    }
}

public class ScarletMeteor : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.ScarletMeteor.Path;
    public ref float Time => ref Projectile.ai[0];

    public override void SetDefaults()
    {
        Projectile.width = 36;
        Projectile.height = 38;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 450;
        Projectile.MaxUpdates = 2;
        Projectile.DamageType = DamageClass.Generic;
    }

    public SlotId Whoosh;
    public Trail trail;
    public TrailPoints cache;

    public override void AI()
    {
        if (Time > 20f)
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + .1f, -20f, 20f);
        Projectile.Opacity = InverseLerp(0f, 20f, Time);

        if (trail == null || trail.Disposed)
            trail = new(c => Projectile.width,
                (c, _) => Color.OrangeRed * MathHelper.SmoothStep(1f, 0f, c.X) * Projectile.Opacity, null, 10);
        cache ??= new(10);
        cache.Update(Projectile.Center + Projectile.velocity);

        if (SoundEngine.TryGetActiveSound(Whoosh, out ActiveSound t) && t.IsPlaying)
            t.Position = Projectile.Center;
        else
            Whoosh = AssetRegistry.GennedSounds.HeatMeteorFall.Play(Projectile.Center, .5f, 0f, .1f, 20);

        ParticleRegistry.SpawnHeavySmokeParticle(Projectile.RotHitbox().RandomPoint(),
            -Projectile.velocity * Main.rand.NextFloat(.2f, .5f),
            Main.rand.Next(20, 30), Main.rand.NextFloat(.4f, .7f),
            Color.OrangeRed.Lerp(Color.Chocolate, Main.rand.NextFloat(.3f, .6f)) * Projectile.Opacity);
        ParticleRegistry.SpawnSparkleParticle(Projectile.RotHitbox().RandomPoint(),
            -Projectile.velocity * Main.rand.NextFloat(.7f, 1.4f), Main.rand.Next(15, 25),
            Main.rand.NextFloat(.3f, .4f), Color.OrangeRed * Projectile.Opacity, Color.Chocolate * Projectile.Opacity,
            Main.rand.NextFloat(.7f, 1.7f), Main.rand.NextFloat(-.2f, .2f));

        Projectile.VelocityBasedRotation();
        Time++;
    }

    public override void OnKill(int timeLeft)
    {
        ScreenShakeSystem.New(new(.1f, .1f), Projectile.Center);
        AssetRegistry.GennedSounds.HeatMeteorBoom.Play(Projectile.Center, .7f, 0f, .1f, 10);
        if (this.RunLocal())
        {
            float off = RandomRotation();
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * InverseLerp(0f, 4, i) + off).ToRotationVector2();
                Projectile.NewProj(Projectile.Center, vel, ModContent.ProjectileType<ScarletMeteorExplosion>(),
                    Projectile.damage / 4, Projectile.knockBack / 4f, Projectile.owner);
            }
        }
    }

    public override bool PreDraw(ref Color lightColor)
    {
        void draw()
        {
            if (trail == null || cache == null)
                return;

            trail.DrawTrail(AssetRegistry.GennedShaders.StandardPrimitiveShader, cache.Points, 30);
        }

        PixelationSystem.QueuePrimitiveRenderAction(draw, PixelationLayer.UnderProjectiles);

        Projectile.DrawBaseProjectile(Color.White * Projectile.Opacity);
        return false;
    }
}

public class ScarletMeteorExplosion : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.ScarletMeteorExplosion.Path;

    private const int Horiz = 6;

    private const int Vert = 2;

    public int FrameX
    {
        get => (int) Projectile.ai[0];
        set => Projectile.ai[0] = value;
    }

    public int FrameY
    {
        get => (int) Projectile.ai[1];
        set => Projectile.ai[1] = value;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void AI()
    {
        Projectile.frameCounter++;
        if (Projectile.frameCounter % 3 == 2)
        {
            FrameX++;
            if (FrameX >= Horiz)
            {
                FrameY++;
                FrameX = 0;
            }

            if (FrameY >= 2)
            {
                Projectile.Kill();
            }
        }

        Projectile.FacingUp();
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        // Prevent shredding of literally any enemy with more than one segment
        Projectile.damage = (int) (Projectile.damage * .9f);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return CircularHitboxCollision(Projectile.Center, 119f, targetHitbox);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D tex = Projectile.ThisProjectileTexture();
        Rectangle frame = tex.Frame(Horiz, Vert, FrameX, FrameY);
        Vector2 orig = frame.Size() / 2;
        Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, Color.White, Projectile.rotation,
            orig, 1f, 0, 0f);
        return false;
    }
}

public class ShockLightning : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.ShockLightning.Path;

    public const int Lifetime = 20;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 5;
    }

    public override void SetDefaults()
    {
        Projectile.width = 32;
        Projectile.height = 96;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = Lifetime;
        Projectile.penetrate = -1;
    }

    public override bool ShouldUpdatePosition() => false;

    public ref float Time => ref Projectile.ai[0];

    public Vector2 End
    {
        get => new(Projectile.ai[1], Projectile.ai[2]);
        set
        {
            Projectile.ai[1] = value.X;
            Projectile.ai[2] = value.Y;
        }
    }

    public override void AI()
    {
        if (Time == 0f)
        {
            ParticleRegistry.SpawnFlash(Projectile.Center - Vector2.UnitY * Projectile.height / 2, 40, .8f,
                Projectile.height);
            ParticleRegistry.SpawnFlash(End, 30, .5f, Projectile.height);
            Projectile.velocity = Main.rand.NextBool() ? Vector2.UnitX : -Vector2.UnitX;
        }

        Projectile.frame = (int) MathHelper.Lerp(0, 5, InverseLerp(Lifetime, 0f, Projectile.timeLeft));
        Time++;
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return targetHitbox.LineCollision(Projectile.Center, End, Projectile.width);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D shock = Projectile.ThisProjectileTexture();
        Rectangle frame = shock.Frame(1, Main.projFrames[Projectile.type], 0, Projectile.frame);

        Vector2 drawPos = Projectile.Center;
        Vector2 projDirToEnd = End.MoveTowards(drawPos, 24f) - drawPos;
        Vector2 normalized = projDirToEnd.SafeNormalize(Vector2.Zero);
        float segmentLength = frame.Height;
        float rot = normalized.ToRotation() + MathHelper.PiOver2;
        float lengthRemainingToDraw = projDirToEnd.Length() + segmentLength / 2f;

        while (lengthRemainingToDraw > 0f)
        {
            Main.spriteBatch.Draw(shock, drawPos - Main.screenPosition, frame, Color.White, rot, frame.Size() / 2, 1f,
                Projectile.direction.ToSpriteDirection(), 0f);

            drawPos += normalized * segmentLength;
            lengthRemainingToDraw -= segmentLength;
        }

        return false;
    }
}

public class WaveSiphon : ModProjectile
{
    public override string Texture => AssetRegistry.GennedTextures.SmolBoll.Path;

    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 22;
        Projectile.friendly = true;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.DamageType = DamageClass.Generic;
        Projectile.timeLeft = 120;
        Projectile.penetrate = 1;
        Projectile.MaxUpdates = 1;
    }

    public Player Owner => Main.player[Projectile.owner];

    public bool TileDeath
    {
        get => (int) Projectile.ai[0] == 1;
        set => Projectile.ai[0] = value.ToInt();
    }

    public ref float Time => ref Projectile.ai[1];

    public override void AI()
    {
        if (Time == 0f)
        {
            Projectile.timeLeft = Main.rand.Next(90, 120);
        }

        if (Time > 10f && NPCTargeting.TryGetClosestNPC(new(Projectile.Center, 400, true), out NPC target))
        {
            Projectile.velocity =
                Vector2.SmoothStep(Projectile.velocity, Projectile.SafeDirectionTo(target.Center) * 12f, .2f);
        }
        else
            Projectile.velocity *= .996f;

        Projectile.ProjAntiClump(.3f);
        Projectile.FacingUp();
        Time++;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center,
            ClampToCardinalDirection(Projectile.velocity).ToRotation() + MathHelper.PiOver2,
            ParticleRegistry.CrosscodeBollType.DieWallBig, CrossDiscHoldout.Element.Wave);
        TileDeath = true;
        return true;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.numHits <= 0)
        {
            if (!TileDeath)
                ParticleRegistry.SpawnCrossCodeBoll(Projectile.Center, 0f, ParticleRegistry.CrosscodeBollType.Die,
                    CrossDiscHoldout.Element.Wave);
            AssetRegistry.GennedSounds.crosscodeBallDie.Play(Projectile.Center, 1f, 0f, .1f, 20, Name);
        }
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        AssetRegistry.GennedSounds.WaveHitSmall.Play(Projectile.Center, .6f, 0f, 0f, 20, Name);
        ParticleRegistry.SpawnCrossCodeHit(Projectile.Center, ParticleRegistry.CrosscodeHitType.Small,
            CrossDiscHoldout.Element.Wave);
        Owner.Heal(Main.rand.Next(3, 5));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Main.spriteBatch.SetBlendState(BlendState.Additive);
        Texture2D bloom = AssetRegistry.GennedTextures.GlowParticleSmall;
        Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, Color.White * .5f, 0f,
            bloom.Size() * .5f, .3f, 0);
        Main.spriteBatch.ResetToDefault();

        Texture2D tex = Projectile.ThisProjectileTexture();

        Rectangle framed = tex.Frame(1, 5, 0, 4);
        Main.spriteBatch.DrawBetter(tex, Projectile.Center, framed, Color.White, Projectile.rotation, framed.Size() / 2,
            1f);
        return false;
    }
}


/* Still pretty scuffed, thank red for tile code...
public struct BouncePrediction
{
    private const float MaxDistance = 2000f;
    private const float DotInterval = 100f;
    private const int MaxBounces = 4;
    private List<Vector2> pathPoints;

    public IReadOnlyList<Vector2> PathPoints => pathPoints.AsReadOnly();

    public BouncePrediction()
    {
        pathPoints = new List<Vector2>();
    }

    public void Update(Vector2 start, Vector2 direction)
    {
        pathPoints.Clear();

        if (direction == Vector2.Zero)
            return;

        direction = direction.SafeNormalize(Vector2.Zero); // Normalize safely
        Vector2 currentPosition = start;
        float remainingDistance = MaxDistance;
        pathPoints.Add(currentPosition);

        // World boundaries
        float WorldMinX = Main.leftWorld;
        float WorldMaxX = Main.rightWorld;
        float WorldMinY = Main.topWorld;
        float WorldMaxY = Main.bottomWorld;

        int bounceCount = 0;
        while (remainingDistance > 0 && bounceCount < MaxBounces)
        {
            Vector2 rayEnd = currentPosition + direction * remainingDistance;

            // Clamp to world boundaries
            if (rayEnd.X < WorldMinX || rayEnd.X > WorldMaxX || rayEnd.Y < WorldMinY || rayEnd.Y > WorldMaxY)
            {
                float tX = direction.X == 0 ? float.MaxValue : (direction.X > 0 ? (WorldMaxX - currentPosition.X) / direction.X : (WorldMinX - currentPosition.X) / direction.X);
                float tY = direction.Y == 0 ? float.MaxValue : (direction.Y > 0 ? (WorldMaxY - currentPosition.Y) / direction.Y : (WorldMinY - currentPosition.Y) / direction.Y);
                float t = Math.Min(tX, tY);
                if (t <= 0 || float.IsInfinity(t) || float.IsNaN(t))
                {
                    break;
                }
                rayEnd = currentPosition + direction * t;
            }

            Vector2? collision = RaytraceTiles(currentPosition, rayEnd);

            if (!collision.HasValue || (collision.Value.X == 0 && collision.Value.Y == 0))
            {
                // No collision: Add points along the ray up to rayEnd
                AddDotPoints(currentPosition, currentPosition + direction * MaxDistance);
                pathPoints.Add(rayEnd);
                break; // Exit after adding points for the full ray
            }

            Vector2 collisionPoint = collision.Value;
            if (!WorldGen.InWorld((int)(collisionPoint.X / 16f), (int)(collisionPoint.Y / 16f), 0))
            {
                AddDotPoints(currentPosition, collisionPoint);
                pathPoints.Add(collisionPoint);
                break;
            }

            AddDotPoints(currentPosition, collisionPoint);
            pathPoints.Add(collisionPoint);

            Vector2 normal = GetFallbackNormal(collisionPoint, direction);

            Vector2 oldDirection = direction;
            direction = Vector2.Reflect(direction, normal);

            currentPosition = collisionPoint + direction * 0.1f; // Nudge to avoid re-collision
            remainingDistance -= Vector2.Distance(start, collisionPoint);
            bounceCount++;
        }
    }

    private void AddDotPoints(Vector2 from, Vector2 to)
    {
        float distance = Vector2.Distance(from, to);
        if (float.IsNaN(distance))
            return;

        int dotCount = (int)(distance / DotInterval);
        if (dotCount == 0)
            return;

        for (int i = 1; i <= dotCount; i++)
        {
            float t = i * DotInterval / distance;
            Vector2 dotPoint = Vector2.Lerp(from, to, t);
            if (!pathPoints.Contains(dotPoint))
            {
                pathPoints.Add(dotPoint);
            }
        }
    }

    public static Vector2 GetFallbackNormal(Vector2 collisionPoint, Vector2 incomingDirection)
    {
        int x = (int)(collisionPoint.X / 16f);
        int y = (int)(collisionPoint.Y / 16f);
        float tileLeft = x * 16f;
        float tileRight = (x + 1) * 16f;
        float tileTop = y * 16f;
        float tileBottom = (y + 1) * 16f;

        Point collisionTile = collisionPoint.ToTileCoordinates();
        Vector2 center = collisionTile.ToWorldCoordinates();
        Tile tile = Main.tile[collisionTile];
        float sideView = MathHelper.PiOver2;
        if (tile.IsHalfBlock)
        {
            sideView = MathHelper.PiOver4;
            center.Y += 4f;
        }
        //center.SuperQuickDust(Color.Purple, 5);
        //collisionPoint.SuperQuickDust(Color.Yellow, 41);

        if (tile.Slope == SlopeType.Solid)
        {
            if (center.IsInFieldOfView(0f, collisionPoint, sideView, 100f)) // Right
                return Vector2.UnitX;
            else if (center.IsInFieldOfView(MathHelper.PiOver2, collisionPoint, MathHelper.PiOver2, 100f)) // Down
                return Vector2.UnitY;
            else if (center.IsInFieldOfView(MathHelper.Pi, collisionPoint, sideView, 100f)) // Left
                return -Vector2.UnitX;
            else if (center.IsInFieldOfView(-MathHelper.PiOver2, collisionPoint, MathHelper.PiOver2, 100f)) // Up
                return -Vector2.UnitY;
        }
        else
        {
            float diagonal = MathF.Sqrt(2f) / 2f;

            if (tile.Slope == SlopeType.SlopeDownLeft)
                return new Vector2(-diagonal, diagonal);
            if (tile.Slope == SlopeType.SlopeDownRight)
                return new Vector2(diagonal, diagonal);
            if (tile.Slope == SlopeType.SlopeUpLeft)
                return new Vector2(-diagonal, -diagonal);
            if (tile.Slope == SlopeType.SlopeUpRight)
                return new Vector2(diagonal, -diagonal);
        }

        return Vector2.UnitY; // Default to upward if no clear side
    }
}
*/
