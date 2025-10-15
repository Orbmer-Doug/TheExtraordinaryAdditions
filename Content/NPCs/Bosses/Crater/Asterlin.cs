using CalamityMod;
using CalamityMod.NPCs.Yharon;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

[AutoloadBossHead]
public partial class Asterlin : ModNPC, IBossDowned
{
    #region Enums
    public enum AsterlinAIType : int
    {
        // pondering the orb
        AbsorbingEnergy,

        // Phase 1
        Swings,
        RotatedDicing,
        Barrage,

        // Phase 2
        Tesselestic,
        Disintegration,
        Lightripper,

        // Phase 3
        Cleave,
        Hyperbeam,
        TechnicBombBarrage,

        // Desperation
        UnrelentingRush,
        UnveilingZenith,

        // Phase Transitions
        EnterPhase2,
        EnterPhase3,
        DesperationDrama,
        GabrielLeave,

        // Intermediate states
        GetScrewed,
    }

    #endregion

    #region Balancing
    /// <summary>
    /// Represents the 1 - 0 ratio of life for asterlin
    /// </summary>
    public float LifeRatio => InverseLerp(0f, NPC.lifeMax, NPC.life);
    public const float Phase2LifeRatio = 0.65f;
    public const float Phase3LifeRatio = 0.3f;

    public static int LightAttackDamage => DifficultyBasedValue(220, 384, 424, 464, 560, 620);
    public static int MediumAttackDamage => DifficultyBasedValue(250, 440, 464, 488, 550, 700);
    public static int HeavyAttackDamage => DifficultyBasedValue(280, 480, 515, 588, 680, 730);
    public static int SuperHeavyAttackDamage => DifficultyBasedValue(340, 390, 605, 645, 780, 820);
    #endregion

    /// <summary>
    /// The aimed target for Asterlin, whether it be a player or NPC
    /// </summary>
    public NPCAimedTarget Target;
    public Player PlayerTarget;
    public ref float[] ExtraAI => ref NPC.AdditionsInfo().ExtraAI;

    public bool FightStarted
    {
        get => NPC.ai[0] == 1f;
        set => NPC.ai[0] = value.ToInt();
    }

    /*
     * Note: ExtraAI 0 - 10 are reserved for attack states and will be reset per transition
     */

    public bool DonePhase2Transition
    {
        get => ExtraAI[11] == 1f;
        set => ExtraAI[11] = value.ToInt();
    }
    public bool DonePhase3Transition
    {
        get => ExtraAI[12] == 1f;
        set => ExtraAI[12] = value.ToInt();
    }
    public bool DoneDesperationTransition
    {
        get => ExtraAI[13] == 1f;
        set => ExtraAI[13] = value.ToInt();
    }
    public ref float HeatDistortionArea => ref ExtraAI[14];
    public ref float HeatDistortionStrength => ref ExtraAI[15];

    public int SwordIndex
    {
        get => (int)ExtraAI[16];
        set => ExtraAI[16] = value;
    }
    public CyberneticSword Sword;

    public int GunIndex
    {
        get => (int)ExtraAI[17];
        set => ExtraAI[17] = value;
    }
    public TheTechnicBlitzripper Gun;

    public int StaffIndex
    {
        get => (int)ExtraAI[18];
        set => ExtraAI[18] = value;
    }
    public TheTesselesticMeltdown Staff;

    public int HammerIndex
    {
        get => (int)ExtraAI[19];
        set => ExtraAI[19] = value;
    }
    public JudgementHammer Hammer;

    #region AI
    public override void AI()
    {
        if (StateMachine is null)
            LoadStates();

        // Pick a target if the current one is invalid
        bool invalidTargetIndex = Target.Invalid;
        if (invalidTargetIndex || !NPC.WithinRange(Target.Center, 4600f))
            PlayerTargeting.SearchForTarget(NPC, Target);
        Target = NPC.GetTargetData();
        if (NPC.HasValidTarget && Target.Type == NPCTargetType.Player)
            PlayerTarget = Main.player[NPC.target];

        // Reset variables every frame
        NPC.damage = 0;
        NPC.defense = NPC.defDefense;
        NPC.dontTakeDamage = false;
        NPC.immortal = false;
        NPC.ShowNameOnHover = true;

        if (FightStarted)
        {
            NPC.noTileCollide = true;
            NPC.noGravity = true;

            // Disable lifesteal for all players and give them infinite flight
            foreach (Player player in Main.ActivePlayers)
            {
                if (player.DeadOrGhost)
                    continue;

                player.GrantInfiniteFlight();
                player.GrantBossEffectsBuff();
                player.moonLeech = true;
            }

            if (Main.netMode != NetmodeID.SinglePlayer)
                AbsorbingEnergy_RemoveAnyMasses();

            PlayerCount(out _, out int alive);
            if (StateMachine != null && alive == 0)
                CurrentState = AsterlinAIType.GetScrewed;
        }

        NPC.scale = ZPosition;
        if (NPC.scale < .6f)
            NPC.ShowNameOnHover = false;
        int oldWidth = NPC.width;
        int idealWidth = (int)(NPC.scale * 128f);
        int idealHeight = (int)(NPC.scale * 278f);
        if (idealWidth != oldWidth)
        {
            NPC.position.X += NPC.width / 2;
            NPC.position.Y += NPC.height / 2;
            NPC.width = idealWidth;
            NPC.height = idealHeight;
            NPC.position.X -= NPC.width / 2;
            NPC.position.Y -= NPC.height / 2;
        }

        // Disable damage when invisible
        if (NPC.Opacity <= 0.35f)
        {
            NPC.ShowNameOnHover = false;
            NPC.dontTakeDamage = true;
        }

        // you dare despawn
        NPC.timeLeft = 7200;
        if (NumUpdates == -1)
        {
            ResetGraphics();
            SearchForArsenalWeapons();
            StateMachine?.PerformBehaviors();
            StateMachine?.PerformStateTransitionCheck();
            AITimer++;
            UpdateGraphics();
        }
    }

    public override bool ModifyCollisionData(Rectangle victimHitbox, ref int immunityCooldownSlot, ref MultipliableFloat damageMultiplier, ref Rectangle npcHitbox)
    {
        if (RotatedHitbox.Intersects(victimHitbox))
            return true;
        return false;
    }

    public override bool CheckDead()
    {
        if (CurrentState == AsterlinAIType.GabrielLeave)
            return true;

        // Keep life at 1
        NPC.life = 1;
        NPC.netUpdate = true;
        return false;
    }
    #endregion AI
}