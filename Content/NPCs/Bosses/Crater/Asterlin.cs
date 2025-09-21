using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;
using TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater.Projectiles;
using TheExtraordinaryAdditions.Core.Globals;
using TheExtraordinaryAdditions.Core.Systems;
using TheExtraordinaryAdditions.Core.Utilities;

namespace TheExtraordinaryAdditions.Content.NPCs.Bosses.Crater;

// Lucille... i remember your PARADIGMS
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
        Cleave,
        Disintegration,
        Lightripper,

        // Phase 3
        Judgement,
        Hyperbeam,
        TechnicBombBarrage,

        // Desperation
        UnveilingZenith,
        UnrelentingRush,
        QuickHyperbeam,

        // Phase Transitions
        EnterPhase2,
        EnterPhase3,
        DesperationDrama,
        GabrielLeave,

        // Intermediate states
        GetScrewed,
        ResetCycle,
    }

    #endregion

    #region Balancing
    /// <summary>
    /// Represents the 1 - 0 ratio of life for asterlin
    /// </summary>
    public float LifeRatio => InverseLerp(0f, NPC.lifeMax, NPC.life);
    public const float Phase2LifeRatio = 0.65f;
    public const float Phase3LifeRatio = 0.3f;

    public static int LightAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    public static int MediumAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    public static int HeavyAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    public static int SuperHeavyAttackDamage => DifficultyBasedValue(210, 230, 260, 290, 310, 350);
    #endregion

    private static NPC myself;
    public static NPC Myself
    {
        get
        {
            if (myself is not null && !myself.active)
                return null;

            return myself;
        }
        internal set => myself = value;
    }

    private CyberneticSword Sword;
    private TheTechnicBlitzripper Gun;
    private TheTesselesticMeltdown Staff;

    /// <summary>
    /// The aimed target for Asterlin, whether it be a player or NPC
    /// </summary>
    public NPCAimedTarget Target;
    public Player PlayerTarget;

    /// <summary>
    /// The state that Asterlin is currently in
    /// </summary>
    public AsterlinAIType CurrentState
    {
        get
        {
            // Add the relevant phase cycle if it has been exhausted
            if (StateMachine.StateStack is not null && (StateMachine?.StateStack?.Count ?? 1) <= 0)
                StateMachine?.StateStack.Push(StateMachine.StateRegistry[AsterlinAIType.ResetCycle]);

            return StateMachine?.CurrentState?.Identifier ?? AsterlinAIType.AbsorbingEnergy;
        }
    }

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
        get => NPC.AdditionsInfo().ExtraAI[11] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[11] = value.ToInt();
    }
    public bool DonePhase3Transition
    {
        get => NPC.AdditionsInfo().ExtraAI[12] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[12] = value.ToInt();
    }
    public bool DoneDesperationTransition
    {
        get => NPC.AdditionsInfo().ExtraAI[13] == 1f;
        set => NPC.AdditionsInfo().ExtraAI[13] = value.ToInt();
    }
    public ref float HeatDistortionArea => ref NPC.AdditionsInfo().ExtraAI[14];
    public ref float HeatDistortionStrength => ref NPC.AdditionsInfo().ExtraAI[15];

    #region AI
    public override void AI()
    {
        // Pick a target if the current one is invalid
        bool invalidTargetIndex = Target.Invalid;
        if (invalidTargetIndex || !NPC.WithinRange(Target.Center, 4600f))
            PlayerTargeting.SearchForTarget(NPC, Target);
        Target = NPC.GetTargetData();
        if (NPC.HasValidTarget && Target.Type == NPCTargetType.Player)
            PlayerTarget = Main.player[NPC.target];

        // Set the global NPC instance
        Myself = NPC;
        if (Myself == null)
            return;

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
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || player.ghost)
                    continue;

                player.GrantInfiniteFlight();
                player.GrantBossEffectsBuff();

                player.moonLeech = true;
            }
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

        ResetGraphics();
        SearchForArsenalWeapons();
        StateMachine?.PerformBehaviors();
        StateMachine?.PerformStateTransitionCheck();
        AITimer++;
        UpdateGraphics();
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